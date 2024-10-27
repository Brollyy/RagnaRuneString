using RagnaRuneString;
using RagnaRuneString.Version1;

namespace RagnaRuneStringTest.Version1
{
    /// <summary>
    /// Run a simple serialize-deserialize test on a bunch of random input data to check if we get back exactly the same data as an output.
    /// </summary>
    [TestClass]
    public class IdempotencyRandomTest
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used in DynamicData attribute")]
        private static IEnumerable<object[]> TestData
        {
            get
            {
                var random = new Random();
                return Enumerable.Range(0, 1000).Select(_ => new object[] { RandomRuneStringData(random) });
            }
        }

        private static RuneStringData RandomRuneStringData(Random random)
        {
            return new RuneStringData(
                runes: Enumerable.Range(0, random.Next(1000)).Select(_ => new Rune(random.NextDouble() * 1000, random.Next(4))),
                bpmChanges: Enumerable.Range(0, random.Next(100)).Select(_ => new BPMChange(random.NextDouble() * 300, random.NextDouble() * 1000))
            );
        }

        [TestMethod]
        [DynamicData("TestData")]
        public void IdempotencyTest(RuneStringData inputData)
        {
            var outputData = RuneStringSerializer.DeserializeV1(RuneStringSerializer.Serialize(inputData, RagnaRuneString.Version.VERSION_1));

            //Assert.AreEqual(inputData, outputData) would be enough, but getting more detailed info about what exactly was different is preferable.
            Assert.AreEqual(inputData.runes.Count, outputData.runes.Count, "Runes count is different");
            var inputRuneEnumerator = inputData.runes.GetEnumerator();
            var outputRuneEnumerator = outputData.runes.GetEnumerator();
            int i = 0;
            while (inputRuneEnumerator.MoveNext() && outputRuneEnumerator.MoveNext())
            {
                Assert.AreEqual(inputRuneEnumerator.Current, outputRuneEnumerator.Current, $"Rune at index {i} is different");
                i++;
            }

            Assert.AreEqual(inputData.runes.Count, outputData.runes.Count, "BPM change count is different");
            var inputBPMEnumerator = inputData.runes.GetEnumerator();
            var outputBPMEnumerator = outputData.runes.GetEnumerator();
            i = 0;
            while (inputBPMEnumerator.MoveNext() && outputBPMEnumerator.MoveNext())
            {
                Assert.AreEqual(inputBPMEnumerator.Current, outputBPMEnumerator.Current, $"BPM change at index {i} is different");
                i++;
            }
        }
    }
}
