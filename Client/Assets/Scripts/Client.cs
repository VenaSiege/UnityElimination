using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elimination;
using Elimination.Command;

/// <summary>
/// Represents the client.
///     - Utilizes TcpClient for communication with the server.
///     - Processes commands received from the server.
///     - Performs various actions (such as login...).
/// </summary>
public class Client : IDisposable {
    
    public readonly struct LoginData {
        public readonly string UserName;
        public readonly string Password;

        public LoginData(string userName, string password) {
            this.UserName = userName;
            this.Password = password;
        }
    }

    private TcpClient _tcpClient = new();
    private readonly byte[] _buffer = new byte[8 * 1024];
    private readonly CancellationTokenSource _cancellationTokenSource = new();


    public static Client Instance { get; private set; }

    public static Client CreateInstance() {
        Client prev = Instance;
        prev?.Dispose();
        Instance = new Client();
        return Instance;
    }

    private Client() { }

    public void Dispose() {
        _cancellationTokenSource.Cancel();
        if (_tcpClient != null) {
            _tcpClient.Dispose();
            _tcpClient = null;
        }
    }

    /// <summary>
    /// Asynchronously logs in to the server.
    /// </summary>
    public async UniTask LoginAsync(IPEndPoint serverEndPoint, LoginData loginData) {
        await _tcpClient.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
        await SendAsync(new LoginRequest() {
            UserName = loginData.UserName,
            Password = EncodePassword(loginData.Password),
        });
    }

    public async UniTask<Cmd> ReceiveAndProcessSingleCommandAsync() {
        return await ReceiveAsync(_cancellationTokenSource.Token);
    }

    private static string EncodePassword(string password) {
        byte[] md5 = Utils.CalculateMD5(Encoding.UTF8.GetBytes(password));
        return Utils.ToHexString(md5, false);
    }

    private async UniTask SendAsync(int cmdType, string json) {
        byte[] data = Encoding.UTF8.GetBytes(json);
        byte[] prefix = new byte[sizeof(uint) * 2];
        Utils.WriteUnsignedInt32ToBytes((uint)cmdType, prefix, 0, true);
        Utils.WriteUnsignedInt32ToBytes((uint)data.Length, prefix, sizeof(uint), true);
        NetworkStream stream = _tcpClient.GetStream();
        await stream.WriteAsync(prefix);
        await stream.WriteAsync(data);
    }

    public async UniTask SendAsync(Cmd cmd) {
        string json = Helper.Serialize(cmd, out int cmdType);
        await SendAsync(cmdType, json);
    }

    private async UniTask<Cmd> ReceiveAsync(CancellationToken cancel) {
        // The first, read the command and json length field.
        NetworkStream stream = _tcpClient.GetStream();
        await ReadExactlyAsync(stream, sizeof(uint) * 2, cancel);
        
        // Parse the command. That are 4 bytes in the _buffer[0].
        int cmdType = (int)Utils.ParseUnsignedInt32FromBytes(_buffer, 0, true);
        if (!Helper.IsCommandTypeValid(cmdType)) {
            throw new InvalidDataException($"Unknown command type '{cmdType}'");
        }
        
        // Parse the length of json.
        int jsonLen = (int)Utils.ParseUnsignedInt32FromBytes(_buffer, sizeof(uint), true);
        if (jsonLen <= 0 || jsonLen > _buffer.Length) {
            throw new InvalidDataException($"Invalid json length ({jsonLen}).");
        }
        
        // Read the json.
        await ReadExactlyAsync(stream, jsonLen, cancel);
        return Helper.Deserialize(cmdType, _buffer, 0, jsonLen);
    }

    /// <summary>
    /// Read exactly count bytes into the _buffer.
    /// </summary>
    /// <param name="stream">The stream</param>
    /// <param name="count">How many bytes should be read.</param>
    /// <param name="cancel">The cancellation token</param>
    /// <exception cref="IOException" />
    private async UniTask ReadExactlyAsync(Stream stream, int count, CancellationToken cancel) {
        for (int pos = 0; count > 0;) {
            int read = await stream.ReadAsync(_buffer, pos, count, cancel);
            if (read > 0) {
                pos += read;
                count -= read;
            } else {
                throw new IOException("Peer closed.");
            }
        }
    }
}