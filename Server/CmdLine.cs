using System.Diagnostics;

namespace EliminationServer {
    public class CmdLine {

        private readonly static string USAGE =
@"Usage: {0} <options>
  Options:
    -a, --address: Listening address, default is ""0.0.0.0"".
    -p, --port:    Listening port, default is {1}.
";

        public readonly static ushort DEFAULT_LISTEN_PORT = 20678;

        public string ListenAddress { get; private set; }
        public ushort ListenPort { get; private set; }

        public CmdLine() {
            ListenAddress = "0.0.0.0";
            ListenPort = DEFAULT_LISTEN_PORT;
        }

        /// <summary>
        /// Parse the command line.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <exception cref="FormatException">If command line invalidate.</exception>
        public void Parse(string[] args) {
            for (int i = 0; i < args.Length; ++i) {
                string arg = args[i];
                if (arg == "-a" || arg == "/a" || arg == "--address") {
                    this.ListenAddress = GetNextToken("listening address", args, ref i);
                } else if (arg == "-p" || arg == "/p" || arg == "--port") {
                    string strPort = GetNextToken("listening port", args, ref i);
                    if (!ushort.TryParse(strPort, out ushort port)) {
                        throw new InvalidCommandLineException($"Invalid port value '{strPort}'");
                    }
                    this.ListenPort = port;
                } else {
                    throw new InvalidCommandLineException($"Unknown option '{arg}'.");
                }
            }
        }

        private static string GetNextToken(string expected, string[] args, ref int index) {
            if (index + 1 >= args.Length) {
                throw new InvalidCommandLineException($"Expect {expected} after the '{args[index]}' option.");
            }
            ++index;
            return args[index];
        }

        public static string GetUsage() {
            return string.Format(
                USAGE,
                Environment.ProcessPath,
                DEFAULT_LISTEN_PORT);
        }

        private class InvalidCommandLineException : FormatException {
            public InvalidCommandLineException(string msg)
                : base($"{msg}\n{CmdLine.GetUsage()}") { }
        }
    }
}
