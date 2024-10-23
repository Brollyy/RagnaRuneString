using RagnaRuneString;

namespace RagnaRuneStringTest
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
                0x80, 0x02,     // varint 256 (0.25 * 1024)  -> 00000000 00000000 00000001 00000000 -> 10000000 00000010
                0x80, 0x04,     // varint 512 (0.5 * 1024)   -> 00000000 00000000 00000010 00000000 -> 10000000 00000100
                0x80, 0x06,     // varint 768 (0.75 * 1024)  -> 00000000 00000000 00000011 00000000 -> 10000000 00000110
                0x80, 0x08,     // varint 1024 (1 * 1024)    -> 00000000 00000000 00000100 00000000 -> 10000000 00001000
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
                0x80, 0x02,         // varint 256 (0.25 * 1024)  -> 00000000 00000000 00000001 00000000 -> 10000000 00000010
                0x80, 0x04,         // varint 512 (0.5 * 1024)   -> 00000000 00000000 00000010 00000000 -> 10000000 00000100
                0x80, 0x06,         // varint 768 (0.75 * 1024)  -> 00000000 00000000 00000011 00000000 -> 10000000 00000110
                0x80, 0x08,         // varint 1024 (1 * 1024)    -> 00000000 00000000 00000100 00000000 -> 10000000 00001000
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
                0x80, 0x08,                 // varint 1024 (1 * 1024)    -> 00000000 00000000 00000100 00000000 -> 10000000 00001000
                0x03,                       // n for first n-rune
                0x00, 0x01, 0x02,           // column bytes (0, 1, 2)
                0x80, 0x10,                 // varint 2048 (2 * 1024)    -> 00000000 00000000 00001000 00000000 -> 10000000 00010000
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
                0x80, 0x02,     // varint 256 (0.25 * 1024)  -> 00000000 00000000 00000001 00000000 -> 10000000 00000010
                0xD5, 0x02,     // varint 341 (0.(3) * 1024) -> 00000000 00000000 00000001 01010101 -> 11010101 00000010
                0x80, 0x04,     // varint 512 (0.5 * 1024)   -> 00000000 00000000 00000010 00000000 -> 10000000 00000100
                0x80, 0x06,     // varint 768 (0.75 * 1024)  -> 00000000 00000000 00000011 00000000 -> 10000000 00000110
                0x80, 0x08,     // varint 1024 (1 * 1024)    -> 00000000 00000000 00000100 00000000 -> 10000000 00001000
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
                0x00,                       // time - varint 0 (0 * 1024)                   -> 00000000 00000000 00000000 00000000 -> 00000000
                0x80, 0xc0, 0x07,           // bpm  - varint 122880 (120 * 1024)            -> 00000000 00000001 11100000 00000000 -> 10000000 11000000 00000111
                0x80, 0xc0, 0x02,           // time - varint 40960 (40 * 1024)              -> 00000000 00000000 10100000 00000000 -> 10000000 11000000 00000010
                0x80, 0xe0, 0x08,           // bpm  - varint 143360 (140 * 1024)            -> 00000000 00000010 00110000 00000000 -> 10000000 11100000 00001000
                0xa4, 0xd2, 0x04,           // time - varint 76068 (74.(2857174) * 1024)    -> 00000000 00000001 00101001 00100100 -> 10100100 11010010 00000100
                0x80, 0xc0, 0x07            // bpm  - varint 122880 (120 * 1024)            -> 00000000 00000001 11100000 00000000 -> 10000000 11000000 00000111
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
                0x80, 0x02,                 // varint 256 (0.25 * 1024)  -> 00000000 00000000 00000001 00000000 -> 10000000 00000010
                0x80, 0x06,                 // varint 768 (0.75 * 1024)  -> 00000000 00000000 00000011 00000000 -> 10000000 00000110
                0x20,                       // single rune column byte (00 10), with additional 4 bits of padding
                0x01,                       // double rune count
                0x80, 0x04,                 // varint 512 (0.5 * 1024)   -> 00000000 00000000 00000010 00000000 -> 10000000 00000100
                0x60, 0x00, 0x00,           // double rune column combo bytes (011), with additional 21 bits of padding.
                0x01,                       // n-rune count
                0x80, 0x08,                 // varint 1024 (1 * 1024)    -> 00000000 00000000 00000100 00000000 -> 10000000 00001000
                0x03,                       // n for n-rune
                0x00, 0x01, 0x02,           // column bytes (0, 1, 2)
                0x03,                       // BPM changes count
                0x00,                       // time - varint 0 (0 * 1024)                   -> 00000000 00000000 00000000 00000000 -> 00000000
                0x80, 0xc0, 0x07,           // bpm  - varint 122880 (120 * 1024)            -> 00000000 00000001 11100000 00000000 -> 10000000 11000000 00000111
                0x80, 0xc0, 0x02,           // time - varint 40960 (40 * 1024)              -> 00000000 00000000 10100000 00000000 -> 10000000 11000000 00000010
                0x80, 0xe0, 0x08,           // bpm  - varint 143360 (140 * 1024)            -> 00000000 00000010 00110000 00000000 -> 10000000 11100000 00001000
                0xa4, 0xd2, 0x04,           // time - varint 76068 (74.(2857174) * 1024)    -> 00000000 00000001 00101001 00100100 -> 10100100 11010010 00000100
                0x80, 0xc0, 0x07            // bpm  - varint 122880 (120 * 1024)            -> 00000000 00000001 11100000 00000000 -> 10000000 11000000 00000111
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