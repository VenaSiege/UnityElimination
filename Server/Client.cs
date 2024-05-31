using Elimination;
using Elimination.Command;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using static EliminationServer.UserManager;

namespace EliminationServer {

    /// <summary>
    /// 封装每一个客户端的业务逻辑
    /// </summary>
    public class Client : IDisposable {

        /// <summary>
        /// 这个静态容器，存放所有已知的客户端Socket，及与之相关联的 Client 对象。
        /// （这个变量用 s_ 开关，表示它是一个 static 成员）
        /// </summary>
        private static readonly Dictionary<Socket, Client> s_allClients = new();

        /// <summary>
        /// 静态函数：得到所有的 Socket（用于主模块的 Select() 检查所有 Socket 是否“可读”）
        /// </summary>
        public static void CopyAllSocketsToList(List<Socket> target) {
            target.AddRange(s_allClients.Keys);
        }

        /// <summary>
        /// 静态函数：根据给定的 Socket，来查找与之相关联的 Client 对象。
        /// </summary>
        /// <param name="sock">给定的 Socket</param>
        /// <returns>与给定 Socket 相关联的 Client 对象，或 null（若未找到）</returns>
        public static Client? FindBySocket(Socket sock) {
            if (s_allClients.TryGetValue(sock, out var found)) {
                return found;
            } else {
                return null;
            }
        }

        /// <summary>
        /// 仅用于 ToString() 时的显示
        /// </summary>
        private readonly string _id;

        /// <summary>
        /// 持有的 Socket 对象
        /// （注：我们项目的统一约定，以下划线开头的成员变量，是私有变量）
        /// </summary>
        private Socket? _socket;

        /// <summary>
        /// 用于对收到的字节流进行解析的 PacketParser 对象
        /// </summary>
        private readonly PacketParser _packetParser = new();

        /// <summary>
        /// 玩家名字，当且仅当玩家已经成功登录后，才不为空。
        /// </summary>
        public string? UserName { get; private set; }

        /// <summary>
        /// 玩家是否已经成功登录了？
        /// </summary>
        public bool IsLoginSuccess => (UserName != null);

        /// <summary>
        /// 构造函数。将一个给定的 Socket 与构造出来的 Client 相关联。
        /// </summary>
        public Client(Socket socket) {
            _socket = socket;
            _id = MakeId(socket);
            s_allClients.Add(socket, this); // 将 Socket 与 this 的映射，存入到静态成员中（以便 FindBySocket 进行查找）
        }

        private static string MakeId(Socket socket) {
            var s = socket.RemoteEndPoint?.ToString();
            if (s == null) {
                s = socket.ToString();
            }
            return $"[Client {s}]";
        }

        /// <summary>
        /// 取回客户端发来的一个完整的 <see cref="Cmd"/>。
        /// </summary>
        /// <returns>
        /// 一个 Cmd 对象，表示客户端发来的“Command/Request”包。或者返回 null，
        /// 表示客户端发来的数据还不够完整，暂时无法得到一个完整的 Cmd。        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">本 Client 已销毁。</exception>
        /// <exception cref="SystemException">客户端发来的数据包是非法的，或者断线了等……</exception>
        public Cmd? ReceiveCmd() {
            if (_socket == null) {
                throw new ObjectDisposedException("Socket already closed.");
            }
            return _packetParser.ReceiveCmd(_socket); // 将解析工作委派给 PacketParser 对象来执行。
        }

        /// <summary>
        /// 向客户端发送一个 <see cref="Cmd"/>
        /// </summary>
        /// <param name="cmd">要发送的 Cmd</param>
        /// <exception cref="ObjectDisposedException">本 Client 已销毁</exception>
        /// <exception cref="IOException">通讯错误（比如对方断线了，或主动关闭了……）</exception>
        public void Send(Cmd cmd) {
            // 先将要发送的 Cmd 对象，序列化为 JSON 字串.
            // 并转换为字节数组，因为 Socket 发送和接收的都是字节流。
            string json = Helper.Serialize(cmd, out int cmdType);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // 因为 TCP 传输的是字节流，没有“包”的边界。为了确保对端能够
            // 知道如何多少字节是一个“包”，所以需要告诉对端“本次我发的包，有
            // 多少字节”。
            // 为此，我们在每次发送完整 JSON 之前，都额外发送两个 uint（共 8 个字节）。
            // 一个用来表示本包的 Cmd 是哪个 Cmd，另一个用来表示 JSON 的长度（字节数）。
            byte[] prefix = new byte[sizeof(uint) * 2];
            Utils.WriteUnsignedInt32ToBytes((uint)cmdType, prefix, 0, true);
            Utils.WriteUnsignedInt32ToBytes((uint)jsonBytes.Length, prefix, sizeof(uint), true);

            Send(prefix);       // 先把前缀的 8 个字节发出去。
            Send(jsonBytes);    // 再把 JSON 字节流发出去。
        }

        private void Send(byte[] data) {
            Send(data, 0, data.Length);
        }

        private void Send(byte[] data, int offset, int len) {
            if (_socket == null) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            try {
                // 注：一次 Socket.Send() 不保证能把所有想发送的数据都成功发出去。
                // 所以这里用了一个循环。比如想发10字节，第一次只发了3次，那就再来一次，
                // 第二次尝试发 7 字节。
                while (len > 0) {
                    int sent = _socket.Send(data, offset, len, SocketFlags.None);
                    if (sent <= 0) {
                        throw new IOException($"send bytes return {sent}");
                    }
                    len -= sent;
                    offset += sent;
                }
            } catch (SocketException e) {
                throw new IOException($"{this} send error.", e);
            }
        }

        public override string ToString() {
            return _id;
        }

        /// <summary>
        /// “关闭/销毁”一个 Client。
        /// </summary>
        /// <remarks>
        /// 虽然 C# 有自动回收垃圾的机制，但是在我们的业务逻辑里，当一个 Client 不再有效（比
        /// 如 Socket 已断开）时，必须显式地调用 Close 方法（等价于 Dispose）。在 Close 方法
        /// 里，可以关闭 Socket，更重要的时，及时地把本 Client 从 s_allClients 中移除。
        /// </remarks>
        public void Close() {
            Dispose();
        }

        public void Dispose() {
            if (_socket != null) {
                Console.WriteLine($"{this} has been closed.");
                s_allClients.Remove(_socket);
                _socket.Dispose();
                _socket = null;
            }
        }

        /// <summary>
        /// Called when the attached socket can be read (some data is already in the protocol stack buffer).
        /// </summary>
        /// <exception cref="SystemException">通讯错误（比如 Socket 断线等），或客户端发来的数据非法。</exception>
        public void OnSocketCanRead() {
            // 先尝试读取一个完整的 Cmd。若取得 null，说明对端传来的数据还不完整，下次再接着取。
            Cmd? cmd = ReceiveCmd();
            if (cmd == null) {
                return;
            }

            // 尚未登录的，那第一个 Cmd 必须是 LoginRequest。否则就要抛异常。
            if (!this.IsLoginSuccess) {
                // This client is not logged in yet; the command must be LoginRequest.
                if (cmd is LoginRequest loginRequest) {
                    ProcessClientLoginRequest(loginRequest);
                    return;
                }
                throw new InvalidOperationException("Not logged in.");
            }

            // 已经登录的，就判断 Cmd 是什么类型，分别调用 GameManager 的相关函数来执行。
            switch (cmd) {
            case GamePrepare gamePrepare:
                GameManager.Instance.OnGamePrepare(this, gamePrepare);
                break;
            case GamePieceClick pieceClick:
                GameManager.Instance.OnGamePieceClick(this, pieceClick);
                break;
            }
        }

        /// <summary>
        /// 处理 LoginRequest 命令/请求
        /// </summary>
        /// <param name="cmd"></param>
        private void ProcessClientLoginRequest(LoginRequest cmd) {
            /*
             * 这段代码需要了解 Await/Async 机制：
             *      首先，调用  UserManager.LoginAsync() 方法，得到一个异步对象 Task。
             *      然后，用 ContinueWith() 来告诉这个 Task：“当 LoginAsync 这个异步
             *          操作成功完成之后，继续执行 OnLoginAsyncComplete 函数”。
             */
            UserManager.Instance.LoginAsync(this, cmd).ContinueWith(OnLoginAsyncComplete);
        }

        /// <summary>
        /// 当 UserManager.LoginAsync() 这个异步操作成功完成后，调用此函数
        /// </summary>
        /// <param name="task"><see cref="UserManager.LoginAsync(Client, LoginRequest)"/> 返回的 Task</param>
        private void OnLoginAsyncComplete(Task<LoginResult> task) {
            LoginResult loginResult = task.Result;
            // 向队列里放一个 Action。这个 Action 等会儿在主模式的 MainLoop 里，会被取出来执行。
            WorkQueue.Instance.Post(() => {
                LoginResponse response = new() {
                    UserName = loginResult.UserInfo?.UserName,
                    Code = loginResult.StatusCode,
                };
                this.Send(response);
                var userInfo = loginResult.UserInfo;
                if (userInfo == null) {
                    Console.WriteLine($"{this} login failed.");
                    this.Dispose();
                } else {
                    this.UserName = userInfo.UserName;
                    Console.WriteLine($"{this} logged in, user name is '{this.UserName}'.");
                    // 告诉 GameManager，有新的 Client 进来了。
                    GameManager.Instance.NewPlayerIncoming(this);
                }
            });
        }

        /// <summary>
        /// 数据包解析器。
        /// </summary>
        private class PacketParser {
            /* 
            An entire packet:
            +---------------------------+-------------------------------------+------//----------------+
            |   Command Type (4 Bytes)  |   Following JSON Length (4 Bytes)   |   Variable size JSON   |
            +---------------------------+-------------------------------------+------//----------------+     
            */

            /// <summary>
            /// An entire packet where the first 4 bytes (sizeof uint) represent the command type. 
            /// (In network byte order).
            /// </summary>
            private const int SIZE_OF_CMD_TYPE_FIELD = sizeof(uint);

            /// <summary>
            /// The next 4 bytes (sizeof uint) in the packet represent the length of the following JSON data. 
            /// (In network byte order).
            /// </summary>
            private const int SIZE_OF_JSON_LEN_FIELD = sizeof(uint);

            /// <summary>
            /// Therefore, the head size of the packet is 4 bytes for type and 4 bytes for length.
            /// </summary>
            private const int HEAD_SIZE = SIZE_OF_CMD_TYPE_FIELD + SIZE_OF_JSON_LEN_FIELD;

            /// <summary>
            /// The buffer used for receive socket data.
            /// </summary>
            private readonly byte[] _buffer = new byte[8 * 1024];

            /// <summary>
            /// The received data is stored in which position of the _buffer.
            /// Data received from the previous reception, which has not been processed yet, is stored before this position.
            /// </summary>
            private int _bufferPos;

            /// <summary>
            /// The cmdType field parsed from _buffer[0..3], valid only when _bufferPos is greater than or equal to HEAD_SIZE.
            /// </summary>
            private int _cmdType;

            /// <summary>
            /// The json length field parsed from _buffer[4..7], valid only when _bufferPos is greater than or equal to HEAD_SIZE.
            /// </summary>
            private int _jsonLen;

            /// <summary>
            /// Parse cmdType and jsonLen fields from _buffer.
            /// It is required that the available bytes in the _buffer must be at least HEAD_SIZE.
            /// </summary>
            /// <exception cref="InvalidDataException">When cmdType or jsonLen is not valid</exception>
            private void ParseCmdTypeAndJsonLen() {
                _cmdType = (int)Utils.ParseUnsignedInt32FromBytes(_buffer, true);
                if (!Helper.IsCommandTypeValid(_cmdType)) {
                    throw new InvalidDataException($"Invalid command type '{_cmdType}'");
                }
                _jsonLen = (int)Utils.ParseUnsignedInt32FromBytes(_buffer, SIZE_OF_CMD_TYPE_FIELD, true);
                if (_jsonLen <= 0 || _jsonLen >= _buffer.Length - HEAD_SIZE) {
                    throw new InvalidDataException($"Invalid json length '{_jsonLen}'");
                }
            }

            /// <summary>
            /// Try to read a specified number of bytes from the socket.
            /// And automatically increase _bufferPos.
            /// </summary>
            /// <param name="socket">The socket object.</param>
            /// <param name="bytes">The number of bytes expected to be read, must not be greater than '_buffer.Length - _bufferPos'.</param>
            /// <returns>The number of bytes read, or 0 if no data is available.</returns>
            /// <exception cref="IOException"></exception>
            private int ReadFromSocket(Socket socket, int bytes) {
                Debug.Assert(bytes <= _buffer.Length - _bufferPos);
                try {
                    int receivedBytes = socket.Receive(_buffer, _bufferPos, bytes, SocketFlags.None);
                    if (receivedBytes <= 0) {
                        throw new IOException("Peer disconnected.");
                    }
                    _bufferPos += receivedBytes;
                    return receivedBytes;
                } catch (SocketException) {
                    // There is no data available in the protocol stack buffer.
                    return 0;
                }
            }

            /// <summary>
            /// Receive a command from the socket.
            /// </summary>
            /// <param name="socket">The socket.</param>
            /// <returns>Received command, or null when we have not read all packet yet.</returns>
            /// <exception cref="SystemException"></exception>
            public Cmd? ReceiveCmd(Socket socket) {
                if (_bufferPos < HEAD_SIZE) {
                    ReadFromSocket(socket, HEAD_SIZE - _bufferPos);
                    if (_bufferPos < HEAD_SIZE) {
                        // Need at least 8 bytes in the buffer, cmdType and jsonLen.
                        // So we return null, meaning the entire command has not been received.
                        return null;
                    }
                    // We have HEAD_SIZE bytes in the _buffer, parse cmdType and jsonLen now.
                    ParseCmdTypeAndJsonLen();
                }

                int jsonByLastRead = _bufferPos - HEAD_SIZE;
                int jsonNeedRead = _jsonLen - jsonByLastRead;
                Debug.Assert(jsonNeedRead > 0);

                // Read only the required number of bytes, leaving the data for the subsequent
                // bytes in the protocol stack buffer.
                // This keeps the Socket in a 'data available to read' state.
                ReadFromSocket(socket, jsonNeedRead);

                if (_bufferPos - HEAD_SIZE < _jsonLen) {
                    // We have not read all the JSON yet, return null and wait for the next read.
                    return null;
                }

                Cmd cmd = Helper.Deserialize(_cmdType, _buffer, HEAD_SIZE, _jsonLen);
                _bufferPos = 0;
                return cmd;
            }
        }
    }
}
