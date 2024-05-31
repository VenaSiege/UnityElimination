using LitJson;
using System;
using System.IO;
using System.Text;

namespace Elimination.Command {

    public interface Cmd { }

    public struct NullCmd : Cmd {
        public static readonly NullCmd Instance = new NullCmd();
    }

    public struct LoginRequest : Cmd {
        public string UserName;
        public string Password;
    }

    public struct LoginResponse : Cmd {
        public string UserName;
        public int Code;
    }

    public struct GamePrepare : Cmd {
        public bool Ready;
        public bool BattleAI;
    }

    public struct GameStart : Cmd {
        public int BoardWidth;
        public int BoardHeight;
        public string Pieces;
        public string PlayerA;
        public string PlayerB;
    }

    /// <summary>
    /// Indicates that a piece has been clicked.
    /// </summary>
    public struct GamePieceClick : Cmd {
        /// <summary>
        /// The player name
        /// </summary>
        public string Player;

        /// <summary>
        /// Clicked coordinate of X
        /// </summary>
        public int X;

        /// <summary>
        /// Clicked coordinate of Y
        /// </summary>
        public int Y;

        /// <summary>
        /// Score of this click
        /// </summary>
        public int ThisScore;

        /// <summary>
        /// Total score (include this score)
        /// </summary>
        public int TotalScore;
    }

    public struct GameOver : Cmd {
        public string Winner;
        public string Loser;
    }

    public static class Helper {

        private static readonly Type[] CMD_TYPE_LIST = {
            typeof(LoginRequest),
            typeof(LoginResponse),
            typeof(GamePrepare),
            typeof(GameStart),
            typeof(GamePieceClick),
            typeof(GameOver),
        };

        public static bool IsCommandTypeValid(int cmdType) {
            return cmdType >= 0 && cmdType < CMD_TYPE_LIST.Length;
        }

        /// <summary>
        /// Serialize a command object to json string
        /// </summary>
        /// <param name="cmd">The command object.</param>
        /// <param name="cmdType">Return the command type</param>
        /// <returns>Serialized JSON string</returns>
        /// <exception cref="IOException">If the command objecg</exception>
        public static string Serialize(Cmd cmd, out int cmdType) {
            Type type = cmd.GetType();
            cmdType = Array.IndexOf(CMD_TYPE_LIST, type);
            if (cmdType < 0) {
                throw new IOException($"'{type.Name}' is not a command.");
            }
            return JsonMapper.ToJson(cmd);
        }

        /// <summary>
        /// Deserialize a command object from json.
        /// </summary>
        /// <param name="cmdType">The command type.</param>
        /// <param name="json">Json array</param>
        /// <param name="startIndex">Offset of the json start in the bytes array.</param>
        /// <param name="len">How many bytes of the json.</param>
        /// <returns>The command object</returns>
        /// <exception cref="IOException">If command type unknown, or json is not correct.</exception>
        public static Cmd Deserialize(int cmdType, byte[] json, int startIndex, int len) {
            return Deserialize(cmdType, Encoding.UTF8.GetString(json, startIndex, len));
        }

        /// <summary>
        /// Deserialize a command object from json.
        /// </summary>
        /// <param name="cmdType">The command type.</param>
        /// <param name="json">Json array</param>
        /// <returns>The command object</returns>
        /// <exception cref="IOException">If command type unknown, or json is not correct.</exception>
        public static Cmd Deserialize(int cmdType, byte[] json) {
            return Deserialize(cmdType, json, 0, json.Length);
        }

        /// <summary>
        /// Deserialize a command object from json.
        /// </summary>
        /// <param name="cmdType">The command type.</param>
        /// <param name="json">Json string</param>
        /// <returns>The command object</returns>
        /// <exception cref="IOException">If command type unknown, or json is not correct.</exception>
        public static Cmd Deserialize(int cmdType, string json) {
            if (cmdType < 0 || cmdType >= CMD_TYPE_LIST.Length) {
                throw new IOException($"Unknown command type '{cmdType}'.");
            }
            try {
                return JsonMapper.ToObject(json, CMD_TYPE_LIST[cmdType]) as Cmd;
            } catch (JsonException e) {
                throw new IOException($"Deserialize command failed", e);
            }
        }
    }
}
