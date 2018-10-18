using System;
using NeoSharp.Cryptography;
using NeoSharp.Types;
using NeoSharp.Types.ExtensionMethods;

namespace NeoSharp.Core.Extensions
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Generate Script Hash
        /// </summary>
        /// <param name="script">Script</param>
        /// <returns>ScriptHash</returns>
        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        /// <summary>
        /// Generate SHA256 digests
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        /// <returns>Return SHA256 digests</returns>
        public static byte[] Sha256(this byte[] value, int offset, int count)
        {
            return Crypto.Default.Sha256(value, offset, count);
        }

        /// <summary>
        /// Generate SHA256 digests
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return SHA256 digests</returns>
        public static byte[] Sha256(this byte[] value)
        {
            return Crypto.Default.Sha256(value);
        }

        /// <summary>
        /// Generate SHA256 hash
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        /// <returns>Return SHA256 hash</returns>
        public static byte[] Hash256(this byte[] value, int offset, int count)
        {
            return Crypto.Default.Hash256(value, offset, count);
        }

        /// <summary>
        /// Generate SHA256 hash
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return SHA256 hash</returns>
        public static byte[] Hash256(this byte[] value)
        {
            return Crypto.Default.Hash256(value);
        }

        /// <summary>
        /// Bytarray XOR
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns>Return XOR bytearray</returns>
        public static byte[] XOR(this byte[] x, byte[] y)
        {
            if (y == null) throw new ArgumentNullException(nameof(y));
            if (x.Length != y.Length) throw new ArgumentException(nameof(y));

            var result = new byte[x.Length];
            for (var i = 0; i < x.Length; i++)
            {
                result[i] = (byte)(x[i] ^ y[i]);
            }

            return result;
        }

        public static uint CalculateChecksum(this byte[] array)
        {
            return array.Sha256(0, array.Length).Sha256(0, 32).ToUInt32(0);
        }
    }
}
