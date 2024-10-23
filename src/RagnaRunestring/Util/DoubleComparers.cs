namespace RagnaRuneString.Util
{
    internal class DoubleApproxEqualComparer : IEqualityComparer<double>
    {
        private static readonly DoubleApproxEqualComparer SINGLETON = new();

        public static bool ApproxEquals(double x, double y) => SINGLETON.Equals(x, y);

        public bool Equals(double x, double y)
        {
            return Math.Abs(x - y) <= 0.0001;
        }

        public int GetHashCode(double obj)
        {
            return double.Round(obj, 3).GetHashCode(); // We need this rounding to make sure all close-enough values will fall into the same bucket.
        }
    }

    internal class DoubleApproxComparer : IComparer<double>
    {
        private static readonly DoubleApproxComparer SINGLETON = new();

        public static int ApproxCompare(double x, double y) => SINGLETON.Compare(x, y);

        public int Compare(double x, double y)
        {
            if (Math.Abs(x - y) <= 0.0001) return 0;
            return x < y ? -1 : 1;
        }
    }
}
