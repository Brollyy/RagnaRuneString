namespace RagnaRuneString.Version1
{
    internal static class BinaryWriterExtensions
    {
        /// <summary>
        /// Varint encoding a rune time rounded to 4th decimal digit.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="time">time</param>
        internal static void WriteRuneTime(this BinaryWriter writer, double time)
        {
            writer.Write7BitEncodedInt((int)double.Round(time * 10000.0));
        }

        /// <summary>
        /// Single rune column array - contains ceil(lengthSingle/4) bytes encoding lengthSingle 2-bit rune columns, with possible zero bits to pad the last byte.
        /// A single rune column byte represents 4 column values, which are all 2-bit. Values are encoded big-endian inside the byte.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="singleRunes">single runes</param>
        internal static void WriteSingleRuneColumns(this BinaryWriter writer, IEnumerable<Rune> singleRunes)
        {
            foreach (var chunk in singleRunes.Chunk(4))
            {
                var runesForByte = new Rune[4];
                Array.Fill(runesForByte, new());
                Array.Copy(chunk, runesForByte, chunk.Length);

                byte singleColumnByte = (byte)(
                    runesForByte[0].lineIndex << 6 |      // xx______
                    runesForByte[1].lineIndex << 4 |      // __xx____
                    runesForByte[2].lineIndex << 2 |      // ____xx__
                    runesForByte[3].lineIndex             // ______xx
                );
                writer.Write(singleColumnByte);
            }
        }

        /// <summary>
        /// Double rune column array - contains 3 * ceil(ceil(lengthDouble/8)/3) bytes encoding lengthDouble 3-bit rune columns combos, with possible zeros to pad the last bytes.
        /// All 6 possible column combinations for a double rune are encoded as 3-bit combo value.
        /// In total, 3 bytes of double rune column data provide column values for 16 rune objects.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="doubleRunes">double runes</param>
        internal static void WriteDoubleRuneColumns(this BinaryWriter writer, IEnumerable<IGrouping<double, Rune>> doubleRunes)
        {
            var columnComboValues = doubleRunes.Select(group => EncodeDoubleRuneColumnComboValue([.. group])).ToList();
            foreach (var chunk in columnComboValues.Chunk(8))
            {
                var comboValsFor3Byte = new int[8];
                Array.Fill(comboValsFor3Byte, 0);
                Array.Copy(chunk, comboValsFor3Byte, chunk.Length);
                writer.WriteDoubleRuneColumns3Byte(comboValsFor3Byte);
            }
        }

        /// <summary>
        /// A double rune column 3-byte represents 8 column combo values. Combo values are encoded big-endian inside the bytes.
        /// All 6 possible column combinations for a double rune are encoded as 3-bit combo value.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="comboValsFor3Byte">8 combo values</param>
        internal static void WriteDoubleRuneColumns3Byte(this BinaryWriter writer, int[] comboValsFor3Byte)
        {
            byte doubleColumnByte = (byte)(
                comboValsFor3Byte[0] << 5 |                 // xxx_____ ________ ________
                comboValsFor3Byte[1] << 2 |                 // ___xxx__ ________ ________
                comboValsFor3Byte[2] >> 1                   // ______xx x_______ ________
            );
            writer.Write(doubleColumnByte);
            doubleColumnByte = (byte)(
                (comboValsFor3Byte[2] & 1) << 7 |           // ______xx x_______ ________
                comboValsFor3Byte[3] << 4 |                 // ________ _xxx____ ________
                comboValsFor3Byte[4] << 1 |                 // ________ ____xxx_ ________
                comboValsFor3Byte[5] >> 2                   // ________ _______x xx______
            );
            writer.Write(doubleColumnByte);
            doubleColumnByte = (byte)(
                (comboValsFor3Byte[5] & 3) << 6 |           // ________ _______x xx______
                comboValsFor3Byte[6] << 3 |                 // ________ ________ __xxx___
                comboValsFor3Byte[7]                        // ________ ________ _____xxx
            );
            writer.Write(doubleColumnByte);
        }

        /// <summary>
        /// Encodes the combination of column values into a double rune column combo value.
        /// </summary>
        /// <param name="runes">2 runes</param>
        /// <returns>Combo value from 0 to 5</returns>
        /// <exception cref="InvalidPayloadException">If the provided runes are not in expected range.</exception>
        internal static int EncodeDoubleRuneColumnComboValue(Rune[] runes)
        {
            if (runes.Length != 2) throw new InvalidPayloadException($"Combo value cannot be computed with {runes.Length} runes.");
            return (runes[0].lineIndex, runes[1].lineIndex) switch
            {
                (0, 1) => 0,
                (1, 0) => 0,
                (0, 2) => 1,
                (2, 0) => 1,
                (0, 3) => 2,
                (3, 0) => 2,
                (1, 2) => 3,
                (2, 1) => 3,
                (1, 3) => 4,
                (3, 1) => 4,
                (2, 3) => 5,
                (3, 2) => 5,
                _ => throw new InvalidPayloadException($"Invalid double rune combo: {(runes[0].lineIndex, runes[1].lineIndex)}")
            };
        }

        /// <summary>
        /// Write n-rune block containing all the information needed to construct n rune objects.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="nRune">n-rune</param>
        internal static void WriteNRune(this BinaryWriter writer, IGrouping<double, Rune> nRune)
        {
            // 1. Rune time
            writer.WriteRuneTime(nRune.Key);

            // 2. Byte describing number of rune objects.
            writer.Write((byte)nRune.Count());

            // 3. Array of varints describing column values.
            foreach (var rune in nRune)
            {
                writer.Write7BitEncodedInt(rune.lineIndex);
            }
        }

        /// <summary>
        /// Write a block containing all the information needed to construct a BPM change object.
        /// </summary>
        /// <remarks>
        /// It just so happens that for both values 4 decimal places provide good enough accuracy, so we reuse <see cref="WriteRuneTime(BinaryWriter, double)"/>.
        /// </remarks>
        /// <param name="bpmChange">BPM change</param>
        internal static void WriteBPMChange(this BinaryWriter writer, BPMChange bpmChange)
        {
            // 1. Start time
            writer.WriteRuneTime(bpmChange.startTime);

            // 2. BPM
            writer.WriteRuneTime(bpmChange.bpm);
        }
    }
}
