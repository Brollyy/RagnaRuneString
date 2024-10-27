namespace RagnaRuneString.Util
{
    internal class DoubleApproxEqualComparer : IEqualityComparer<double>
    {
        private static readonly DoubleApproxEqualComparer SINGLETON = new();

        public static bool ApproxEquals(double x, double y) => SINGLETON.Equals(x, y);
        public bool Equals(double x, double y) => Math.Round(x, 4) == Math.Round(y, 4);
        public int GetHashCode(double obj) => double.Round(obj, 4).GetHashCode();
    }

    internal class DoubleApproxComparer : IComparer<double>
    {
        private static readonly DoubleApproxComparer SINGLETON = new();

        public static int ApproxCompare(double x, double y) => SINGLETON.Compare(x, y);

        public int Compare(double x, double y) => double.Round(x, 4).CompareTo(double.Round(y, 4));
    }
}
