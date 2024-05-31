using Elimination.Command;
using System.Text.Json;

namespace Test {
    public class TestRequest {

        [Test]
        public void TestSerialize() {
            LoginRequest login = new() {
                UserName = "Hello",
                Password = "World",
            };
            string json = Helper.Serialize(login, out int cmdType);
            Assert.That(json, Is.Not.Null);
            Assert.That(cmdType, Is.AtLeast(0));

            object? cmd = Helper.Deserialize(cmdType, json);
            Assert.That(cmd, Is.Not.Null);
            Assert.IsTrue(cmd is LoginRequest);

            LoginRequest request = (LoginRequest)cmd;
            Assert.That(login.UserName, Is.EqualTo(request.UserName));
            Assert.That(login.Password, Is.EqualTo(request.Password));

            Assert.That(() => Helper.Serialize(NullCmd.Instance, out int cmdType),
                Throws.InstanceOf<SystemException>());
        }

        [Test]
        public void TestDeserialize() {
            string json = "{\"Type\":\"Login\"}";
            Assert.That(
                () => Helper.Deserialize(10000, json),
                Throws.InstanceOf<SystemException>());
        }
    }
}
