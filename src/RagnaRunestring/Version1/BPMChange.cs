using RagnaRuneString.Util;

namespace RagnaRuneString.Version1
{
    /// <summary>
    /// This is a representation of a BPM change, as defined by the `_BPMChanges` section inside the DAT file representing a Ragnarock map.
    /// Only relevant information is stored.<br></br>
    /// For consistency, global BPM is also represented as a BPM change with `startTime = 0`.
    /// </summary>
    public struct BPMChange(double bpm, double startTime = 0) : IComparable<BPMChange>, IEquatable<BPMChange>
    {
        public double bpm = bpm;
        public double startTime = startTime;

        public readonly int CompareTo(object obj)
        {
            if (obj is not BPMChange other)
            {
                throw new ArgumentException($"{obj} is not a BPM change");
            }
            return CompareTo(other);
        }

        public readonly int CompareTo(BPMChange other) => DoubleApproxComparer.ApproxCompare(startTime, other.startTime);

        public readonly bool Equals(BPMChange other) => DoubleApproxEqualComparer.ApproxEquals(startTime, other.startTime) && DoubleApproxEqualComparer.ApproxEquals(bpm, other.bpm);
        public override readonly bool Equals(object? obj) => obj is BPMChange other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(double.Round(startTime, 4), double.Round(bpm, 4));
        public static bool operator ==(BPMChange left, BPMChange right) => left.Equals(right);
        public static bool operator !=(BPMChange left, BPMChange right) => !(left == right);
        public static bool operator <(BPMChange left, BPMChange right) => left.CompareTo(right) < 0;
        public static bool operator <=(BPMChange left, BPMChange right) => left.CompareTo(right) <= 0;
        public static bool operator >(BPMChange left, BPMChange right) => left.CompareTo(right) > 0;
        public static bool operator >=(BPMChange left, BPMChange right) => left.CompareTo(right) >= 0;

        public override readonly string ToString() => $"BPMChange(startTime={startTime}, bpm={bpm})";
    }
}