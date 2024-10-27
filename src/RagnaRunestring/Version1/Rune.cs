using RagnaRuneString.Util;

namespace RagnaRuneString.Version1
{
    /// <summary>
    /// This is a representation of a single rune, as defined by the `_runes` section inside the DAT file representing a Ragnarock map.
    /// Only relevant information is stored.
    /// </summary>
    public struct Rune(double time = 0, int lineIndex = 0) : IComparable<Rune>, IEquatable<Rune>
    {
        public double time = time;
        public int lineIndex = lineIndex;

        public readonly int CompareTo(object? obj)
        {
            if (obj is not Rune other)
            {
                throw new ArgumentException($"{obj} is not a rune");
            }
            return CompareTo(other);
        }

        public readonly int CompareTo(Rune other)
        {
            if (Equals(other)) return 0;
            if (DoubleApproxComparer.ApproxCompare(this.time, other.time) == 1) return 1;
            if (DoubleApproxEqualComparer.ApproxEquals(this.time, other.time) && this.lineIndex > other.lineIndex) return 1;
            return -1;
        }

        public readonly bool Equals(Rune other) => DoubleApproxEqualComparer.ApproxEquals(time, other.time) && lineIndex == other.lineIndex;
        public override readonly bool Equals(object? obj) => obj is Rune other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(double.Round(time, 4), lineIndex);
        public static bool operator ==(Rune left, Rune right) => left.Equals(right);
        public static bool operator !=(Rune left, Rune right) => !(left == right);
        public static bool operator <(Rune left, Rune right) => left.CompareTo(right) < 0;
        public static bool operator <=(Rune left, Rune right) => left.CompareTo(right) <= 0;
        public static bool operator >(Rune left, Rune right) => left.CompareTo(right) > 0;
        public static bool operator >=(Rune left, Rune right) => left.CompareTo(right) >= 0;

        public override readonly string ToString() => $"Rune(time={time}, lineIndex={lineIndex})";
    }
}
