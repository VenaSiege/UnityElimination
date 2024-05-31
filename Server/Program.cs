using System.Net;
using System.Net.Sockets;

namespace EliminationServer {
    internal class Program {

        /// <summary>
        /// 程序入口。
        /// <list type="bullet">
        /// <item>创建侦听 Socket</item>
        /// <item>执行 MainLoop()</item>
        /// </list>
        /// </summary>
        /// <param name="args">命令行参数</param>
        static void Main(string[] args) {
            Socket listenSocket = CreateListenSocket(args);
            MainLoop(listenSocket);
        }

        /// <summary>
        /// 创建一个 TCP 的 Socket，并根据命令行上指定的侦听地址和端口，开启一个 TCP 侦听。
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns></returns>
        private static Socket CreateListenSocket(string[] args) {
            IPEndPoint listenEndPoint = GetListenEndPointFromCmdLine(args);
            Socket socket = new Socket(
                listenEndPoint.AddressFamily,       // 协议/地址族，在本例中，是 IPv4
                SocketType.Stream,                  // TCP 协议是“流式”的，用 SocketType.Stream
                ProtocolType.Tcp);                  // 协议类型是 TCP
            socket.Blocking = false;                // 我们使用“非阻塞式”的模式
                                                    // （非阻塞式，意即：当试图从 Socket 中读取数据，
                                                    // 但无数据可读（对方并没有发送任何数据）时，并不
                                                    // 会阻塞调用者的线程，而是立刻从读取函数中返回失败。
            
            socket.Bind(listenEndPoint);            // 将此 Socket 绑定到给定的 End Point。
            socket.Listen();

            Console.WriteLine($"Server started on '{listenEndPoint}' ...");
            return socket;
        }

        /// <summary>
        /// 解析命令行参数，根据命令行指定的侦听地址和端口，生成一个 IPEndPoint 并返回。
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>侦听地址和端口组成的 IPEndPoint 对象</returns>
        private static IPEndPoint GetListenEndPointFromCmdLine(string[] args) {
            CmdLine cmdLine = new();
            cmdLine.Parse(args);
            IPAddress listenAddress = IPAddress.Parse(cmdLine.ListenAddress);
            return new IPEndPoint(listenAddress, cmdLine.ListenPort);
        }

        /// <summary>
        /// 程序的主循环。<br />
        /// 我们的 Server，一直循环运行而不退出（直到用户按下 CTRL+C 键）。
        /// </summary>
        /// <param name="listenSocket"></param>
        private static void MainLoop(Socket listenSocket) {

            /*
            主循环主要的工作流程和原理是，一个大循环，每一次循环：
                - 检查“消息队列”里有没有待处理的 Action，若有则处理。
                - 检查包括 Listener Socket 在内的所有 Socket，有没有事件发生。
                    - 若是 Listener Socket 有事件发生，表示有客户端连入，就 Accept 它得到一个新的 Client Socket，
                      用这个 Client Socket 与相应的客户端进行通讯。
                      这个新的 Client Socket 也要加入到 Socket List 中，在每一轮循环中进行检查。
                    - 若是 Client Socket 有事件发生，表示有客户端发来的数据要处理，就读这个 Client Socket，然后
                      处理该数据。
            注：我们使用比较传统的 Select 模型来处理 Socket 的 IO，这样可以在单线程里处理业务逻辑，不用考虑并发，从而简化代码。
            */

            // 创建一个 CancellationTokenSource，用于“取消”操作并退出大循环
            // 参考 .NET 的相关文档。
            CancellationTokenSource cts = new();

            // 给系统控制台的“取消键”（也就是CTRL+C）按下事件，加一个委托回调。
            // （即：当用户在控制台窗口中按下 CTRL+C 时，将会通知到我们的程序，调用我们加上去的匿名函数）
            Console.WriteLine("Press CTRL+C to exit.");
            Console.CancelKeyPress += (_, e) => {
                Console.WriteLine("Try to exit, please wait...");
                e.Cancel = true;    // 告诉系统：不要强行中断我的程序，我自己会处理。（参见微软文档）
                cts.Cancel();       // 让我们自己的 Cancellation Token 触发（以告诉我们的大循环，要退出了）
            };

            // 创建一个 List，用来存放所有 Socket，这些 Socket 在下面的循环里，要被 Socket.Select() 函数
            // 进行检查，检查这些 Socket 里有没有哪些有数据进来了。
            List<Socket> checkReadList = new();

            // 大循环，循环退出条件是：cancellationToken.IsCancellationRequested 为真。
            // 这里的 cancellationToken 来自上面创建的 CancellationTokenSource (cts)
            for (CancellationToken cancellationToken = cts.Token; !cancellationToken.IsCancellationRequested; ) {

                // 先处理队列里的 Action
                ProcessQueuedAction(cancellationToken);

                // 从 ProcessQueuedAction() 返回，可能是 cancelationToken 触发了。所以这里要检查一下。 
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                // 把所有 Socket 加到 checkReadList 里。
                checkReadList.Clear();
                checkReadList.Add(listenSocket);
                Client.CopyAllSocketsToList(checkReadList);

                // 用 Socket 类的静态函数 Select，来检查 List 中的所有 Socket。
                // 检查只关心这些 Socket “是否能读”，也就是说，将 List 中任意的一个 Socket “可读” 时，
                // Select 函数就会马上返回。
                // 如果所有的 Socket 都不是“可读”状态，那么 Select 函数将在 10ms 后也返回（我们传入的第 4 个参数是 Timeout 值）
                // （参阅微软的相关文档）
                Socket.Select(checkReadList, null, null, 10 * 1000);
                
                // Select 函数返回了，可能是有某个或某几个 Socket “可读” 了，也可能是超时时间到了。
                // 无论如何，我们都再检查一下 cancellationToken 是否触发了，以便及时地退出程序。
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                // 现在 checkReadList 里剩下的 Socket，都是“可读”状态。
                // 我们依次地处理它。
                foreach (Socket sock in checkReadList) {
                    if (sock == listenSocket) {
                        // 如果 “可读” 的 Socket 是我们的 Listen Socket，这表示有新的客户端
                        // 正在尝试连入，所以调用 Accept 来处理它
                        AcceptIncompingClient(sock);
                    } else {
                        // “可读”的 Socket 是那些 Client Socket，那我们就针对它调用
                        // recv() 来读取客户端发来的数据
                        ReadFromClient(sock);
                    }
                }
            }

            // 大循环退出了，程序也该退出了。
            GameManager.Instance.TearDown();
            Console.WriteLine("Application terminated.");
        }

        /// <summary>
        /// 从队列里取出一个个的 Action，并执行它们
        /// </summary>
        /// <remarks>
        /// 本函数的返回条件是：所有队列里的 Action 都已经执行完毕，或者 CancellationToken 触发。
        /// </remarks>
        /// <see cref="WorkQueue" />
        private static void ProcessQueuedAction(CancellationToken cancellationToken) { 
            while (!cancellationToken.IsCancellationRequested) {
                Action? action = WorkQueue.Instance.TryDequeue();
                if (action == null) {
                    // 取得的 Action 为空，说明队列已空，没有其它 Action 了。
                    break;
                }
                try {
                    action.Invoke();
                } catch (Exception e) {
                    Console.WriteLine($"Execute queued worker error: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 当 Listen Socket “可读” 时，表示有一个来自客户端的 TCP 连接请求。
        /// 接受这个请求，得到一个新的 Client Socket，用于与客户端的通讯。
        /// </summary>
        /// <param name="listenSocket">侦听 Socket</param>
        /// <seealso cref="Socket.Accept"/>
        /// <see cref="Client"/>
        private static void AcceptIncompingClient(Socket listenSocket) {
            Socket incomingSocket = listenSocket.Accept();      // 因为 listenSocket 当前是“可读”（即：有
                                                                // 连入请求）状态，所以 Accept() 不会阻塞。
            incomingSocket.Blocking = false;                    // 当连入的 Client Scket 设为“非阻塞”模式。
            Client client = new Client(incomingSocket);         // 创建一个 Client 对象，这个 Client 对象
                                                                // 与 incomingSocket 相关联。
                                                                // 在 Client 类的构造函数里，会将新创建出来
                                                                // 的 Client 对象都记录在一个全局变量里，所
                                                                // 以新创建的对象不会“丢失”。
            Console.WriteLine($"{client} incoming, wait for login.");
        }

        /// <summary>
        /// 从 Client Socket 里读取字节流。
        /// </summary>
        /// <param name="clientSocket">与客户端进行 TCP 通讯的 Socket</param>
        private static void ReadFromClient(Socket clientSocket) {
            // 根据 Socket，找到与之相关联的 Client 对象。
            Client? client = Client.FindBySocket(clientSocket);
            if (client == null) {
                // 居然没找到？
                // 如果程序设计没有问题，那这种情况是不可能发生的（因为一个 Socket 肯定对应
                // 一个 Client 对象）。
                // 但是我们还是写一些防御性的代码，如果在实际运行的时候，真的遇到这种情况，那
                // 我们就打印一些错误信息，记录一下。
                Console.WriteLine("Inner error: cannnot find client by socket !!!");
                clientSocket.Dispose(); // Socket 是一个 IDispoable 对象，为了释放资源，建议要对其 Dispose()。
                return;
            }

            // 找到对应的 Client 对象了，调用该对象的相应方法。
            try {
                client.OnSocketCanRead();
            } catch (Exception e) {
                // 捕捉到了异常，有可能是断线了，或者客户端主动断连了，这时候，我们应该
                // 销毁这个 Client。因为我们的业务逻辑是 GameManager 在管理，所以我们
                // 把销毁 Client 的工作，统一放在 GameManager 里执行。
                Console.WriteLine($"{client} {e.Message}");
                GameManager.Instance.CloseClient(client);
            }
        }
    }
}