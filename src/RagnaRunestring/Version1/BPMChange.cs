using RagnaRuneString.Util;

namespace RagnaRuneString.Version1
{
    /// <summary>
    /// This is a representation of a BPM change, as defined by the `_BPMChanges` section inside the DAT file representing a Ragnarock map.
    /// Only relevant information is stored.<br></br>
    /// For consistency, global BPM is also represented as a BPM change with `startTime = 0`.
    /// </summary>
    public struct BPMChange(double bpm, double startTime = 0) : IComparable<BPMChange>
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
        public override readonly bool Equals(object? obj) => obj is BPMChange other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(double.Round(startTime), double.Round(bpm));

        public override readonly string ToString() => $"BPMChange(startTime={startTime}, bpm={bpm})";
    }
}