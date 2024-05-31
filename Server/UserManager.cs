using System.Collections.Concurrent;

namespace EliminationServer {
    public class UserManager {

        public readonly struct LoginResult {
            public readonly int StatusCode;
            public readonly UserInfo? UserInfo;
            public LoginResult(int statusCode, UserInfo? userInfo) {
                StatusCode = statusCode;
                UserInfo = userInfo;
            }
        }

        private static readonly UserManager _instance = new();
        public static UserManager Instance => _instance;

        private readonly ConcurrentDictionary<string, UserInfo> _users = new();

        private UserManager() { }

        public async Task<LoginResult> LoginAsync(Client client, Elimination.Command.LoginRequest request) {
            return await Task.Run(() => {
                string? userName = request.UserName;
                string? password = request.Password;
                if (userName == null || password == null) {
                    // 400, Bad Request.
                    return new LoginResult(400, null);
                }
                if (_users.TryGetValue(userName, out var userInfo)) {
                    if (request.Password == userInfo.Password) {
                        // 200, OK
                        return new LoginResult(200, userInfo);
                    }
                    // 401, Unauthorized
                    return new LoginResult(401, null);
                }

                // Create a new account
                userInfo = new UserInfo(userName, password);
                if (_users.TryAdd(userName, userInfo)) {
                    // TODO: Performing data persistence processing may take some time.
                    Thread.Sleep(10);
                    return new LoginResult(201, userInfo);
                } else {
                    return new LoginResult(400, null);
                }
            });
        }

        public class UserInfo {
            public readonly string UserName;
            public readonly string Password;

            public UserInfo(string userName, string password) {
                UserName = userName;
                Password = password;
            }
        }

        private class AsyncResult : IAsyncResult {
            public object? AsyncState { get; private set; }

            public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

            public bool CompletedSynchronously => false;

            public bool IsCompleted => true;

            public AsyncResult(object? state) {
                AsyncState = state;
            }
        }
    }
}
