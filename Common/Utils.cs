using System;
using System.Security.Cryptography;
using System.Text;

namespace Elimination {
    public static class Utils {

        /// <summary>
        /// 从字节流中，按指定的字节序解析一个 32 位无符号整数
        /// </summary>
        /// <param name="bytes">字节流</param>
        /// <param name="startIndex">起始偏移。从此偏移位置起，字节流至少有 4 字节长</param>
        /// <param name="bigEndian">为 True 表示大端序（高位在前），否则是小端序（低位在前）</param>
        public static uint ParseUnsignedInt32FromBytes(byte[] bytes, int startIndex, bool bigEndian) {
            if (bigEndian) {
                return ((uint)bytes[startIndex] << 24) | ((uint)bytes[startIndex + 1] << 16)
                    | ((uint)bytes[startIndex + 2] << 8) | (uint)bytes[startIndex + 3];
            } else {
                return ((uint)bytes[startIndex + 3] << 24) | ((uint)bytes[startIndex + 2] << 16)
                    | ((uint)bytes[startIndex + 1] << 8) | (uint)bytes[startIndex];
            }
        }

        public static uint ParseUnsignedInt32FromBytes(byte[] bytes, bool bigEndian) {
            return ParseUnsignedInt32FromBytes(bytes, 0, bigEndian);
        }

        /// <summary>
        /// 以指定的字节序，将 32 位无符号整数序列化到字节流中。
        /// </summary>
        public static void WriteUnsignedInt32ToBytes(uint value, byte[] buffer, int startIndex, bool bigEndian) {
            if (bigEndian) {
                buffer[startIndex++] = (byte)((value >> 24) & 0xFF);
                buffer[startIndex++] = (byte)((value >> 16) & 0xFF);
                buffer[startIndex++] = (byte)((value >> 8) & 0xFF);
                buffer[startIndex] = (byte)(value & 0xFF);
            } else {
                buffer[startIndex++] = (byte)(value & 0xFF);
                buffer[startIndex++] = (byte)((value >> 8) & 0xFF);
                buffer[startIndex++] = (byte)((value >> 16) & 0xFF);
                buffer[startIndex] = (byte)((value >> 24) & 0xFF);
            }
        }

        public static void WriteUnsignedInt32ToBytes(uint value, byte[] buffer, bool bigEndian) {
            WriteUnsignedInt32ToBytes(value, buffer, 0, bigEndian);
        }

        public static byte[] CalculateMD5(byte[] input) {
            using (MD5 md5 = MD5.Create()) {
                return md5.ComputeHash(input);
            }
        }

        public static string ToHexString(byte[] input, bool upperCase) { 
            StringBuilder sb = new StringBuilder(input.Length * 2);
            string fmt = upperCase ? "X2" : "x2";
            foreach (byte b in input) {
                sb.Append(b.ToString(fmt));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts two hexadecimal characters to an integer.
        /// </summary>
        /// <param name="chars">An array containing the two hexadecimal characters.</param>
        /// <param name="offset">The offset in the array where conversion begins.</param>
        /// <returns>The integer representation of the two hexadecimal characters.</returns>
        /// <exception cref="FormatException">Thrown if the input characters are not valid hexadecimal characters.</exception>
        public static int ConvertTwoHexCharToNumber(char highChar, char lowChar) {
            int high = ConvertHexCharToNumber(highChar);
            int low = ConvertHexCharToNumber(lowChar);
            return (high << 4) | low;
        }

        /// <summary>
        /// Converts a hexadecimal character to its corresponding integer value.
        /// </summary>
        /// <param name="ch">The hexadecimal character to convert.</param>
        /// <returns>The integer representation of the hexadecimal character.</returns>
        /// <exception cref="FormatException">Thrown if the input character is not a valid hexadecimal character.</exception>
        public static int ConvertHexCharToNumber(char ch) {
            if (ch >= '0' && ch <= '9') {
                return ch - '0';
            }
            if (ch >= 'a' && ch <= 'f') {
                return ch - ('a' - 10);
            }
            if (ch >= 'A' && ch <= 'F') {
                return ch - ('A' - 10);
            }
            throw new FormatException($"Incorrect hex char '{ch}'");
        }
    }
}