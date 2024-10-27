using RagnaRuneString.Util;

namespace RagnaRuneString.Version1
{
    /// <summary>
    /// Represents contents of the rune string in this version.
    /// </summary>
    public struct RuneStringData(IEnumerable<Rune> runes, IEnumerable<BPMChange> bpmChanges) : IEquatable<RuneStringData>
    {
        public SortedSet<Rune> runes = new(runes);
        public SortedSet<BPMChange> bpmChanges = new(bpmChanges);

        // TODO: add timing changes to ensure that full information about rune placement can be recovered and Edda can implement pasting logic independent of the rune string source

        internal readonly void Write(BinaryWriter writer)
        {
            WriteRunes(writer);
            WriteBPMChanges(writer);
        }

        internal readonly void WriteRunes(BinaryWriter writer)
        {
            if (runes == null || runes.Count == 0)
            {
                // All three sections empty, which we indicate with 0 length arrays.
                writer.Write7BitEncodedInt(0);
                writer.Write7BitEncodedInt(0);
                writer.Write7BitEncodedInt(0);
                return;
            }

            // Runes block is split into three sections, depending on how many runes occur on the same timing:
            // 1. Single rune section
            // 2. Double rune section
            // 3. n-rune section (triple or quadruple runes, not typically used, but theoretically possible)

            #region Single rune section
            var singleRunes = GetRowRunes(runes, 1).Select(group => group.Single()).ToList();

            // 1. Leading varint specifying number of single runes encoded.
            writer.Write7BitEncodedInt(singleRunes.Count);

            // Array of single runes is encoded as two arrays: first for rune timing, second for rune columns.
            // This allows us to pack the 2-bit column values more compactly than by storing rune data congruently.

            // 2. Single rune timing array - contains lengthSingle varints encoding single rune beats.
            singleRunes.ForEach(rune => writer.WriteRuneTime(rune.time));

            // 3. Single rune column array.
            writer.WriteSingleRuneColumns(singleRunes);
            #endregion

            #region Double rune section
            var doubleRunes = GetRowRunes(runes, 2);

            // 1. Leading varint specifying number of double runes encoded. This needs to be multiplied by 2 to get actual number of rune objects.
            writer.Write7BitEncodedInt(doubleRunes.Count);

            // Array of double runes is encoded as two arrays: first for rune timing, second for rune columns.
            // This allows us to pack the column values more compactly than by storing rune data congruently.

            // 2. Double rune timing array - contains lengthDouble varints encoding double rune beats.
            doubleRunes.ForEach(group => writer.WriteRuneTime(group.Key));

            // 3. Double rune column array.
            writer.WriteDoubleRuneColumns(doubleRunes);
            #endregion

            #region n-rune section
            // 1. Leading varint specifying number of n-runes encoded. The actual number of rune objects will vary.
            List<IGrouping<double, Rune>> nRunes = [.. GetRowRunes(runes, 3), .. GetRowRunes(runes, 4)];
            writer.Write7BitEncodedInt(nRunes.Count);

            // Array of n-runes is encoded as congruent rune data items to simplify the reading process.
            // We don't typically expect to have many of those runes, so there's no reason to optimize space for them.
            nRunes.ForEach(writer.WriteNRune);
            #endregion
        }

        internal readonly void WriteBPMChanges(BinaryWriter writer)
        {
            if (bpmChanges == null || bpmChanges.Count == 0)
            {
                // BPM changes block empty, which we indicate with 0 length array.
                writer.Write7BitEncodedInt(0);
                return;
            }

            // BPM changes block

            // 1. Leading varint specifying number of BPM changes encoded.
            writer.Write7BitEncodedInt(bpmChanges.Count);

            // 2. Array of BPM change items.
            foreach (var item in bpmChanges)
            {
                writer.WriteBPMChange(item);
            }
        }

        internal static RuneStringData Read(BinaryReader reader)
        {
            var runes = ReadRunes(reader);
            var bpmChanges = ReadBPMChanges(reader);

            // Successfully read a rune string!
            return new RuneStringData(runes, bpmChanges);
        }

        internal static IEnumerable<Rune> ReadRunes(BinaryReader reader)
        {
            // Runes block is split into three sections, depending on how many runes occur on the same timing:
            // 1. Single rune section
            // 2. Double rune section
            // 3. n-rune section (triple or quadruple runes, not typically used, but theoretically possible)

            #region Single rune section
            // 1. Leading varint specifying number of single runes encoded.
            var lengthSingle = reader.Read7BitEncodedInt();

            // Array of single runes is encoded as two arrays: first for rune timing, second for rune columns.
            // This allows us to pack the 2-bit column values more compactly than by storing rune data congruently.

            // 2. Single Rune timing array - contains lengthSingle varints encoding single rune beats.
            var singleBeats = Enumerable.Range(0, lengthSingle).Select(_ => reader.ReadRuneTime()).ToArray();

            // 3. Single Rune column array.
            var singleColumns = reader.ReadSingleRuneColumns(lengthSingle);

            // Reconstruct single runes
            var singleRunes = Enumerable.Range(0, lengthSingle).Select(i => new Rune(singleBeats[i], singleColumns[i]));
            #endregion

            #region Double rune section
            // 1. Leading varint specifying number of double runes encoded. This needs to be multiplied by 2 to get actual number of rune objects.
            var lengthDouble = reader.Read7BitEncodedInt();

            // Array of double runes is encoded as two arrays: first for rune timing, second for rune columns.
            // This allows us to pack the column values more compactly than by storing rune data congruently.

            // 2. Double Rune timing array - contains lengthDouble varints encoding double rune beats.
            var doubleBeats = Enumerable.Range(0, lengthDouble).Select(_ => reader.ReadRuneTime()).ToArray();

            // 3. Double Rune column array.
            var doubleColumns = reader.ReadDoubleRuneColumns(lengthDouble);

            // Reconstruct double runes
            var doubleRunes = Enumerable.Range(0, lengthDouble).SelectMany(i => new Rune[] {
                    new(doubleBeats[i], doubleColumns[2 * i]),
                    new(doubleBeats[i], doubleColumns[2 * i + 1])
                });
            #endregion

            #region n-rune section
            // 1. Leading varint specifying number of n-runes encoded. The actual number of rune objects will vary.
            var lengthN = reader.Read7BitEncodedInt();

            // Array of n-runes is encoded as congruent rune data items to simplify the reading process.
            // We don't typically expect to have many of those runes, so there's no reason to optimize space for them.
            var nRunes = Enumerable.Range(0, lengthN).SelectMany(_ => reader.ReadNRune());
            #endregion

            return [.. singleRunes, .. doubleRunes, .. nRunes];
        }

        internal static IEnumerable<BPMChange> ReadBPMChanges(BinaryReader reader)
        {
            // BPM changes block

            // 1. Leading varint specifying number of BPM changes encoded.
            var length = reader.Read7BitEncodedInt();

            // 2. Array of BPM change items.
            var bpmChanges = Enumerable.Range(0, length).Select(_ => reader.ReadBPMChange()).ToArray();

            return bpmChanges;
        }

        private static List<IGrouping<double, Rune>> GetRowRunes(SortedSet<Rune> runes, int i)
        {
            return runes
                .GroupBy(rune => rune.time, new DoubleApproxEqualComparer())
                .Where(group => group.Count() == i)
                .ToList();
        }

        public readonly bool Equals(RuneStringData other) => runes.SetEquals(other.runes) && bpmChanges.SetEquals(other.bpmChanges);
        public override readonly bool Equals(object? obj) => obj is RuneStringData other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(runes, bpmChanges);

        public override readonly string ToString() => $"RuneStringData(runes=SortedSet<Rune>[{runes.Count}], bpmChanges=SortedSet<BPMChange>[{bpmChanges.Count}])";

        public static bool operator ==(RuneStringData left, RuneStringData right) => left.Equals(right);

        public static bool operator !=(RuneStringData left, RuneStringData right) => !(left == right);
    }
}
