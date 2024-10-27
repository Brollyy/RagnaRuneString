using RagnaRuneString;

namespace RagnaRuneStringTest.Version1
{
    [TestClass]
    public class SerializeVersion1UnitTest
    {
        [TestMethod]
        public void SerializeEmpty()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new([], []);

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,     // header
                0x00,           // single rune count 
                0x00,           // double rune count
                0x00,           // n-rune count
                0x00            // BPM changes count
            ]);
            Assert.AreEqual(expectedRuneString, RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1));
        }

        [TestMethod]
        public void SerializeOnlySingleRunes()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [
                    new(0.25, 0),
                    new(0.5, 1),
                    new(0.75, 2),
                    new(1, 3)
                ],
                []
             );

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,     // header
                0x04,           // single rune count 
                0xC4, 0x13,     // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0x88, 0x27,     // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0xCC, 0x3A,     // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x90, 0x4E,     // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x1B,           // single rune column byte (00 01 10 11)
                0x00,           // double rune count
                0x00,           // n-rune count
                0x00            // BPM changes count
            ]);

            var runeString = RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1);
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String(runeString)));
            Assert.AreEqual(expectedRuneString, runeString);
        }

        [TestMethod]
        public void SerializeOnlyDoubleRunes()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [
                    new(0.25, 0), new(0.25, 3),
                    new(0.5, 1), new (0.5, 2),
                    new(0.75, 0), new(0.75, 2),
                    new(1, 1), new(1, 3)
                ],
                []
            );

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,         // header
                0x00,               // single rune count
                0x04,               // double rune count
                0xC4, 0x13,         // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0x88, 0x27,         // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0xCC, 0x3A,         // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x90, 0x4E,         // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x4C, 0xC0, 0x00,   // double rune column combo bytes (010 011 001 100), with additional 12 bits of padding.
                0x00,               // n-rune count
                0x00                // BPM changes count
            ]);

            var runeString = RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1);
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String(runeString)));
            Assert.AreEqual(expectedRuneString, runeString);
        }

        [TestMethod]
        public void SerializeOnlyNRunes()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [
                    new(1, 0), new(1, 1), new(1, 2),
                    new(2, 0), new(2, 1), new(2, 2), new(2, 3)
                ],
                []
             );

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,                 // header
                0x00,                       // single rune count
                0x00,                       // double rune count
                0x02,                       // n-rune count
                0x90, 0x4E,                 // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x03,                       // n for first n-rune
                0x00, 0x01, 0x02,           // column bytes (0, 1, 2)
                0xA0, 0x9C, 0x01,           // varint 20000 (2 * 20000)    -> 00000000 00000000 01001110 00100000 -> 10100000 10011100 00000001
                0x04,                       // n for second n-rune
                0x00, 0x01, 0x02, 0x03,     // column bytes (0, 1, 2, 3)
                0x00                        // BPM changes count
            ]);

            var runeString = RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1);
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String(runeString)));
            Assert.AreEqual(expectedRuneString, runeString);
        }

        [TestMethod]
        public void SerializeRunesWithRoundingAndPadding()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [
                    new(0.25, 0),
                    new(1.0 / 3, 3),
                    new(0.5, 1),
                    new(0.75, 2),
                    new(1, 3)
                ],
                []
            );

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,     // header
                0x05,           // single rune count
                0xC4, 0x13,     // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0x85, 0x1A,     // varint 3333 (0.(3) * 10000) -> 00000000 00000000 00001100 10000101 -> 10000101 00011010
                0x88, 0x27,     // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0xCC, 0x3A,     // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x90, 0x4E,     // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x36, 0xC0,     // single rune column bytes (00 11 01 10 11), with additional 6 bits of zero padding
                0x00,           // double rune count
                0x00,           // n-rune count
                0x00            // BPM changes count
            ]);

            var runeString = RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1);
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String(runeString)));
            Assert.AreEqual(expectedRuneString, runeString);
        }

        [TestMethod]
        public void SerializeOnlyBPMChanges()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [],
                [
                    new(120),
                    new(140, 40),
                    new(120, 74.28571428571429)
                ]
             );

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,                 // header
                0x00,                       // single rune count
                0x00,                       // double rune count
                0x00,                       // n-rune count
                0x03,                       // BPM changes count
                0x00,                       // time - varint 0 (0 * 10000)                    -> 00000000 00000000 00000000 00000000 -> 00000000
                0x80, 0x9F, 0x49,           // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
                0x80, 0xB5, 0x18,           // time - varint 400000 (40 * 10000)              -> 00000000 00000110 00011010 10000000 -> 10000000 10110101 00011000
                0xC0, 0xB9, 0x55,           // bpm  - varint 1400000 (140 * 10000)            -> 00000000 00010101 01011100 11000000 -> 11000000 10111001 01010101
                0xC9, 0xAB, 0x2D,           // time - varint 742857 (74.(2857174) * 10000)    -> 00000000 00001011 01010101 11001001 -> 11001001 10101011 00101101
                0x80, 0x9F, 0x49            // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
            ]);

            var runeString = RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1);
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String(runeString)));
            Assert.AreEqual(expectedRuneString, runeString);
        }

        [TestMethod]
        public void SerializeMixed()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [
                    new(0.25, 0),
                    new(0.5, 1), new (0.5, 2),
                    new(0.75, 2),
                    new(1, 0), new(1, 1), new(1, 2)
                ],
                [
                    new(120),
                    new(140, 40),
                    new(120, 74.28571428571429)
                ]
             );

            string expectedRuneString = Convert.ToBase64String([
                0x00, 0x01,                 // header
                0x02,                       // single rune count
                0xC4, 0x13,                 // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0xCC, 0x3A,                 // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x20,                       // single rune column byte (00 10), with additional 4 bits of padding
                0x01,                       // double rune count
                0x88, 0x27,                 // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0x60, 0x00, 0x00,           // double rune column combo bytes (011), with additional 21 bits of padding.
                0x01,                       // n-rune count
                0x90, 0x4E,                 // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x03,                       // n for n-rune
                0x00, 0x01, 0x02,           // column bytes (0, 1, 2)
                0x03,                       // BPM changes count
                0x00,                       // time - varint 0 (0 * 10000)                    -> 00000000 00000000 00000000 00000000 -> 00000000
                0x80, 0x9F, 0x49,           // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
                0x80, 0xB5, 0x18,           // time - varint 400000 (40 * 10000)              -> 00000000 00000110 00011010 10000000 -> 10000000 10110101 00011000
                0xC0, 0xB9, 0x55,           // bpm  - varint 1400000 (140 * 10000)            -> 00000000 00010101 01011100 11000000 -> 11000000 10111001 01010101
                0xC9, 0xAB, 0x2D,           // time - varint 742857 (74.(2857174) * 10000)    -> 00000000 00001011 01010101 11001001 -> 11001001 10101011 00101101
                0x80, 0x9F, 0x49            // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
            ]);

            var runeString = RuneStringSerializer.Serialize(runeStringData);
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String(runeString)));
            Assert.AreEqual(expectedRuneString, runeString);
        }

        [TestMethod]
        public void SerializeInvalidPayload() => Assert.ThrowsException<InvalidPayloadException>(() => RuneStringSerializer.Serialize("test", RagnaRuneString.Version.VERSION_1));

        [TestMethod]
        public void SerializeWrongVersion() => Assert.ThrowsException<ArgumentException>(() => RuneStringSerializer.Serialize(new RagnaRuneString.Version1.RuneStringData([], []), RagnaRuneString.Version.VERSION_0));

        [TestMethod]
        public void SerializeInvalidRuneColumns()
        {
            RagnaRuneString.Version1.RuneStringData runeStringData = new(
                [
                    new(0.25, 0),
                    new(0.5, 1), new (0.5, 4),
                    new(0.75, 2),
                    new(1, 0), new(1, 1), new(1, 2)
                ],
                [
                    new(120),
                    new(140, 40),
                    new(120, 74.28571428571429)
                ]
             );
            Assert.ThrowsException<InvalidPayloadException>(() => RuneStringSerializer.Serialize(runeStringData, RagnaRuneString.Version.VERSION_1));
        }
    }
}