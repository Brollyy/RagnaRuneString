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
                return Enumerable.Range(0, 1000).Select(_ => { var seed = random.Next(); return new object[] { seed, RandomRuneStringData(seed) }; });
            }
        }

        private static RuneStringData RandomRuneStringData(int seed)
        {
            var random = new Random(seed);
            // Totally random runes have very small chance to generate double or triple runes, so we force a percentage of all the runes to be of certain type.
            var totalRuneCount = random.Next(1000);
            var doubleRuneCount = totalRuneCount / (2 * 20);   // 5% of runes are double runes
            var tripleRuneCount = totalRuneCount / (3 * 200); // 0.5% of runes are triple runes
            var singleRuneCount = totalRuneCount - 2 * doubleRuneCount - 3 * tripleRuneCount;
            return new RuneStringData(
                runes: [
                    ..Enumerable.Range(0, singleRuneCount).Select(_ => RandomRune(random)),
                    ..Enumerable.Range(0, doubleRuneCount).SelectMany(_ => RandomNRune(random, 2)),
                    ..Enumerable.Range(0, tripleRuneCount).SelectMany(_ => RandomNRune(random, 3))
                ],
                bpmChanges: Enumerable.Range(0, random.Next(100)).Select(_ => new BPMChange(random.NextDouble() * 300, random.NextDouble() * 1000))
            );
        }

        private static Rune RandomRune(Random random) => new(random.NextDouble() * 1000, random.Next(4));

        private static List<Rune> RandomNRune(Random random, int n)
        {
            List<Rune> result = [RandomRune(random)];
            while (result.Count < n)
            {
                var rune = RandomRune(random);
                rune.time = result[0].time;
                if (rune != result[0]) result.Add(rune);
            }
            return result;
        }

        [TestMethod]
        [DynamicData("TestData")]
        public void IdempotencyTest(int seed, RuneStringData inputData)
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

        // Utility test for reproducing and debugging specific failing seeds.
        //[TestMethod]
        //[DataRow(1000103267)]
        public void SeededIdempotencyTest(int seed) => IdempotencyTest(seed, RandomRuneStringData(seed));
    }
}
