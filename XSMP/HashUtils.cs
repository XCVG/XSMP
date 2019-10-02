using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NeoSmart.Utils;

namespace XSMP
{
    public static class HashUtils
    {
        /// <summary>
        /// Converts a byte array to a hex string
        /// </summary>
        /// <remarks>
        /// From https://stackoverflow.com/questions/623104/byte-to-hex-string/623184#623184
        /// </remarks>
        public static string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        /// <summary>
        /// Converts a hex string to a byte array
        /// </summary>
        public static byte[] HexStringToBytes(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            for(int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hexString.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return bytes;
        }

        public static string BytesToBase64String(byte[] bytes)
        {
            return UrlBase64.Encode(bytes);
        }

        public static string HexStringToBase64String(string hexString)
        {
            return BytesToBase64String(HexStringToBytes(hexString));
        }

        

    }
}
