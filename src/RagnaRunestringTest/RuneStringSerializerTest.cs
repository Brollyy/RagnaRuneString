namespace RagnaRuneString.Tests
{
    [TestClass]
    public class RuneStringSerializerTest
    {
        [TestMethod]
        public void Serialize() => Assert.AreEqual("AAEAAAAA", RuneStringSerializer.Serialize(new Version1.RuneStringData([], [])));

        [TestMethod]
        public void Deserialize() => Assert.AreEqual(new Version1.RuneStringData([], []), RuneStringSerializer.Deserialize("AAEAAAAA"));

        [TestMethod]
        public void DeserializeV1() => Assert.AreEqual(new Version1.RuneStringData([], []), RuneStringSerializer.Deserialize("AAEAAAAA"));
    }
}