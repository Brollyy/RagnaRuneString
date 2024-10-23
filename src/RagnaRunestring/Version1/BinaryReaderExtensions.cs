namespace RagnaRuneString.Version1
{
    internal static class BinaryReaderExtensions
    {
        /// <summary>
        /// Varint encodes a rune time multiplied by 1024.
        /// This specific value was chosen to encode the time value compactly while supporting the threshold of 0.0001 that's used for rune equality comparison.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>Rune time value</returns>
        internal static double ReadRuneTime(this BinaryReader reader)
        {
            return reader.Read7BitEncodedInt() / 1024.0;
        }

        /// <summary>
        /// Single Rune column array - contains ceil(lengthSingle/4) bytes encoding lengthSingle 2-bit rune columns, with possible zeros to pad the last byte.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <param name="lengthSingle">Number of single runes.</param>
        /// <returns>All rune column values</returns>
        internal static int[] ReadSingleRuneColumns(this BinaryReader reader, int lengthSingle)
        {
            return Enumerable.Range(0, (lengthSingle + 3) / 4)
                .SelectMany(_ => reader.ReadSingleRuneColumnByte())
                .ToArray();
        }

        /// <summary>
        /// A single rune column byte represents 4 column values, which are all 2-bit. Values are encoded big-endian inside the byte.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>4 rune column values</returns>
        internal static IEnumerable<int> ReadSingleRuneColumnByte(this BinaryReader reader)
        {
            var colByte = reader.ReadByte();
            return [
                colByte >> 6,           // xx______
                (colByte >> 4) & 3,     // __xx____
                (colByte >> 2) & 3,     // ____xx__
                colByte & 3             // ______xx
            ];
        }

        /// <summary>
        /// Double Rune column array - contains 3 * ceil(ceil(lengthDouble/8)/3) bytes encoding lengthDouble 3-bit rune columns combos, with possible zeros to pad the last bytes.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <param name="lengthDouble">Number of double runes.</param>
        /// <returns>All rune column values</returns>
        internal static int[] ReadDoubleRuneColumns(this BinaryReader reader, int lengthDouble)
        {
            var doubleColumnBytes = (lengthDouble + 7) / 8;
            return Enumerable.Range(0, 3 * ((doubleColumnBytes + 2) / 3))
                .SelectMany(_ => reader.ReadDoubleRuneColumn3Byte())
                .ToArray();
        }

        /// <summary>
        /// A double rune column 3-byte represents 8 column combo values. Combo values are encoded big-endian inside the bytes.
        /// All 6 possible column combinations for a double rune are encoded as 3-bit combo value.
        /// In total, 3 bytes of double rune column data provide column values for 16 rune objects.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>4 rune column values</returns>
        internal static IEnumerable<int> ReadDoubleRuneColumn3Byte(this BinaryReader reader)
        {
            var colByte = new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
            var comboValues = new int[] {
                colByte[0] >> 5,                                    // xxx_____ ________ ________
                (colByte[0] >> 2) & 7,                              // ___xxx__ ________ ________
                ((colByte[0] & 3) << 1) | (colByte[1] >> 7),        // ______xx x_______ ________
                (colByte[1] >> 4) & 7,                              // ________ _xxx____ ________
                (colByte[1] >> 7) & 7,                              // ________ ____xxx_ ________
                ((colByte[1] & 1) << 2) | (colByte[2] >> 6),        // ________ _______x xx______
                (colByte[2] >> 3) & 7,                              // ________ ________ __xxx___
                colByte[2] & 7,                                     // ________ ________ _____xxx
            };
            return comboValues.SelectMany(DecodeDoubleRuneColumnComboValue);
        }

        /// <summary>
        /// Decodes double rune column combo value into the combination of column values.
        /// </summary>
        /// <param name="comboValue">Combo value from 0 to 5</param>
        /// <returns>2 column values</returns>
        /// <exception cref="ArgumentException">If the comboValue provided is not in expected range.</exception>
        internal static IEnumerable<int> DecodeDoubleRuneColumnComboValue(int comboValue)
        {
            return comboValue switch
            {
                0 => [0, 1],
                1 => [0, 2],
                2 => [0, 3],
                3 => [1, 2],
                4 => [1, 3],
                5 => [2, 3],
                _ => throw new ArgumentException($"Invalid double rune combo value: {comboValue}")
            };
        }

        /// <summary>
        /// Read n-rune block containing all the information needed to construct n rune objects.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>n rune objects</returns>
        internal static IEnumerable<Rune> ReadNRune(this BinaryReader reader)
        {
            // 1. Rune time
            var time = reader.ReadRuneTime();

            // 2. Byte describing number of rune objects - in general, we'd only expect to see 3 or 4 here, but all valid values are supported.
            var n = reader.ReadByte();

            // 3. Array of bytes describing column values - if values above 3 are given, the rune is still generated here, but doesn't really make any sense in the context of the game.
            var cols = Enumerable.Range(0, n).Select(_ => reader.ReadByte());

            // Reconstruct runes
            return cols.Select(col => new Rune(time, col));
        }

        /// <summary>
        /// Read a block containing all the information needed to construct a BPM change object.
        /// </summary>
        /// <remarks>
        /// It just so happens that for both values 1024 provides good enough accuracy, so we reuse <see cref="ReadRuneTime(BinaryReader)"/>.
        /// </remarks>
        /// <param name="reader">reader</param>
        internal static BPMChange ReadBPMChange(this BinaryReader reader)
        {
            // 1. Start time
            var startTime = reader.ReadRuneTime();

            // 2. BPM
            var bpm = reader.ReadRuneTime();

            // Reconstruct BPM change
            return new BPMChange(bpm, startTime);
        }
    }
}
