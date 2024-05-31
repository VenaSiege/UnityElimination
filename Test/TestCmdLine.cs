namespace TestServer {
    public class TestCmdLine {

        [Test]
        public void TestUsage() {
            var usage = CmdLine.GetUsage();
            Assert.IsFalse(string.IsNullOrEmpty(usage));
        }

        [Test]
        public void TestParse() {
            CmdLine cmdLine = new();
            cmdLine.Parse(new string[0]);
            Assert.That(cmdLine.ListenAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(cmdLine.ListenPort, Is.EqualTo(CmdLine.DEFAULT_LISTEN_PORT));

            string[] addressOptions = { "-a", "/a", "--address" };
            string[] portOptions = { "-p", "/p", "--port" };
            string[] args = {
                "-a", "127.0.0.2",
                "-p", "12345",
            };
            for (int i = 0; i < addressOptions.Length; i++) {
                for (int j = 0; j < portOptions.Length; j++) {
                    args[0] = addressOptions[i];
                    args[2] = portOptions[j];
                    cmdLine.Parse(args);
                    Assert.That(cmdLine.ListenAddress, Is.EqualTo("127.0.0.2"));
                    Assert.That(cmdLine.ListenPort, Is.EqualTo(12345));
                }
            }
        }

        [Test]
        public void TestInvalidCmdLine() {
            CmdLine cmdLine = new();
            Assert.That(() => cmdLine.Parse(new string[] { "-a", }), Throws.InstanceOf<FormatException>());
            Assert.That(() => cmdLine.Parse(new string[] { "-p", }), Throws.InstanceOf<FormatException>());
            Assert.That(() => cmdLine.Parse(new string[] { "-p", "-12" }), Throws.InstanceOf<FormatException>());
            Assert.That(() => cmdLine.Parse(new string[] { "-z", }), Throws.InstanceOf<FormatException>());
            Assert.That(() => cmdLine.Parse(new string[] { "abc", }), Throws.InstanceOf<FormatException>());
        }
    }
}