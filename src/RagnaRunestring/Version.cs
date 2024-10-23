namespace RagnaRuneString
{
    /// <summary>
    /// Supported versions of the RagnaRuneString format.
    /// </summary>
    public enum Version
    {
#if DEBUG
        /// <summary>
        /// For internal testing only.
        /// </summary>
        VERSION_0 = 0,
#endif
        /// <summary>
        /// Basic list of runes and BPM changes
        /// </summary>
        VERSION_1 = 1
    }

    internal static class VersionExtensions
    {
        internal static byte ToByte(this Version version) { return (byte)version; }
    }
}
