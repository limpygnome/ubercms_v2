using System;

namespace CMS.Base
{
    /// <summary>
    /// A model for representing a universally unique identifier, following the RFC 4122 standard:
    /// http://tools.ietf.org/html/rfc4122
    /// </summary>
    public class UUID
    {
        // Fields ******************************************************************************************************
        private string  hex;        // The internal hex string of the UUID; this should be 32 characters (no hyphen's).
        private int     hashcode;   // The hash value of this object based on the hex-string. 
        // Methods - Constructors **************************************************************************************
        private UUID(string hex)
        {
            this.hex = hex;
            this.hashcode = int.Parse(hex.Substring(0, 8), System.Globalization.NumberStyles.HexNumber);
        }
        // Methods *****************************************************************************************************
        public override int GetHashCode()
        {
            return hashcode;
        }
        // Methods - Factory Creators **********************************************************************************
        /// <summary>
        /// Creates a new UUID object; the string should be 32 characters (no hypthens). Protection against null and
        /// empty strings.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static UUID createFromHex(string hex)
        {
            if (hex == null || hex.Length != 32)
                return null;
            return new UUID(hex);
        }
        /// <summary>
        /// Creates a new UUID object; the string should be 36 characters with hypthens. Protection against null and
        /// empty strings.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static UUID createFromHexHyphens(string hex)
        {
            if (hex == null || hex.Length != 36)
                return null;
            return createFromHex(hex.Replace("-", ""));
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The SQL hex value (0x...) string value of the UUID; this is to be used when updating the database; for
        /// example: `SELECT * FROM acme WHERE uuid=0x...;`. This value must NOT be wrapped in semi-colons, else
        /// it will be interpreted as a string.
        /// </summary>
        public string SQLValue
        {
            get
            {
                return "0x" + hex;
            }
        }
        /// <summary>
        /// The hex-string of the UUID without hyphens.
        /// </summary>
        public string Hex
        {
            get
            {
                return hex;
            }
        }
        /// <summary>
        /// The hex-string of the UUID with hyphens (8-4-4-4-12 ~ where each number is the number of hex characters).
        /// </summary>
        public string HexHyphens
        {
            get
            {
                return hex.Substring(0, 8) + "-" + hex.Substring(8, 4) + "-" + hex.Substring(12, 4) + "-" + hex.Substring(16, 4) + "-" + hex.Substring(20, 12);
            }
        }
    }
}