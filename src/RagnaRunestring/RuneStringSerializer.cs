namespace RagnaRuneString
{
    public class RuneStringSerializer
    {
        private static readonly byte RUNE_STRING_RESERVED = 0;

        /// <summary>
        /// Serializes a given payload into a rune string format.
        /// </summary>
        /// <param name="payload">Payload to be serialized, depends on the version used.</param>
        /// <param name="version">Version of the runestring format.</param>
        /// <exception cref="ArgumentException">When unsupported version is provided.</exception>
        /// <exception cref="InvalidPayloadException">When the provided payload doesn't match the specification for the selected version.</exception>
        /// <returns>Rune string representing payload in selected version.</returns>
        public static string Serialize(object payload, Version version = Version.VERSION_1)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(RUNE_STRING_RESERVED);
            writer.Write(version.ToByte());

            switch (version)
            {
                case Version.VERSION_1:
                    {
                        if (payload is not Version1.RuneStringData runeStringData)
                        {
                            throw new InvalidPayloadException("Invalid payload for version 1");
                        }
                        writer.Write(runeStringData);
                        break;
                    }
                default: throw new ArgumentException("Invalid version");
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Deserializes a valid rune string into an object.
        /// </summary>
        /// <param name="runeString">Serialized rune string</param>
        /// <exception cref="ArgumentException">Input was not in a valid rune string format</exception>
        /// <returns>Deserialized rune string object, type depends on the version encoded in the string.</returns>
        public static object Deserialize(string runeString)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(runeString);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Input is not a valid rune string.", e);
            }

            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            if (reader.ReadByte() != RUNE_STRING_RESERVED)
            {
                throw new ArgumentException("Input is not a valid rune string.");
            }

            var versionByte = reader.ReadByte();
            return versionByte switch
            {
#if DEBUG
                0 => new object(), // Internal testing only
#endif
                1 => reader.ReadRuneStringV1(),
                _ => throw new ArgumentException($"Input encodes an unsupported rune string version {versionByte}")
            };
        }

        /// <summary>
        /// Deserializes a valid version 1 rune string into an object.<br></br>
        /// This method can be used if you only ever need to support version 1 of the format,
        /// otherwise use <see cref="Deserialize(string)"/> and perform a runtime check for the output types for different versions.
        /// </summary>
        /// <param name="input">Serialized version 1 rune string</param>
        /// <returns>Deserialized version 1 rune string data.</returns>
        /// <exception cref="ArgumentException">Input was not in a valid version 1 rune string format</exception>
        public static Version1.RuneStringData DeserializeV1(string runeString)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(runeString);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Input is not a valid rune string.", e);
            }

            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            if (reader.ReadByte() != RUNE_STRING_RESERVED)
            {
                throw new ArgumentException("Input is not a valid rune string.");
            }

            var versionByte = reader.ReadByte();
            return versionByte switch
            {
                1 => reader.ReadRuneStringV1(),
                _ => throw new ArgumentException($"Input is not in version 1 of the format.")
            };
        }
    }

    /// <summary>
    /// Utility extensions for consistent serialization syntax.
    /// </summary>
    internal static class SerializationExtensions
    {
        internal static void Write(this BinaryWriter writer, Version1.RuneStringData runeStringData)
        {
            runeStringData.Write(writer);
        }

        internal static Version1.RuneStringData ReadRuneStringV1(this BinaryReader reader)
        {
            return Version1.RuneStringData.Read(reader);
        }
    }
}
