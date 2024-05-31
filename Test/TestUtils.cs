using Elimination;

namespace TestServer {

    public class TestUtils {
        [Test]
        public void TestParseUnsignedInt32() {
            byte[] data = { 0x01, 0x02, 0x03, 0x04, };
            uint value = Utils.ParseUnsignedInt32FromBytes(data, true);
            Assert.That(value, Is.EqualTo(0x01020304));
            value = Utils.ParseUnsignedInt32FromBytes(data, false);
            Assert.That(value, Is.EqualTo(0x04030201));
        }

        [Test]
        public void TestWriteUnsignedInt32() {
            uint value = 0x12345678;
            byte[] buffer = new byte[4];

            Utils.WriteUnsignedInt32ToBytes(value, buffer, true);
            Assert.That(buffer, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78 }));

            Utils.WriteUnsignedInt32ToBytes(value, buffer, false);
            Assert.That(buffer, Is.EqualTo(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
        }

        [Test]
        public void ConvertTwoHexCharToNumber_ValidInput_ReturnsCorrectResult() {
            int result = Utils.ConvertTwoHexCharToNumber('1', 'A');
            Assert.That(result, Is.EqualTo(26));
        }

        [Test]
        public void ConvertTwoHexCharToNumber_InvalidInput_ThrowsFormatException() {
            Assert.Throws<FormatException>(() => Utils.ConvertTwoHexCharToNumber('G', 'A'));
            Assert.Throws<FormatException>(() => Utils.ConvertTwoHexCharToNumber('A', 'G'));
        }
    }
}
