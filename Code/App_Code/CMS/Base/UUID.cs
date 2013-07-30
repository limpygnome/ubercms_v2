/*                       ____               ____________
 *                      |    |             |            |
 *                      |    |             |    ________|
 *                      |    |             |   |
 *                      |    |             |   |    
 *                      |    |             |   |    ____
 *                      |    |             |   |   |    |
 *                      |    |_______      |   |___|    |
 *                      |            |  _  |            |
 *                      |____________| |_| |____________|
 *                        
 *      Author(s):      limpygnome (Marcus Craske)              limpygnome@gmail.com
 * 
 *      License:        Creative Commons Attribution-ShareAlike 3.0 Unported
 *                      http://creativecommons.org/licenses/by-sa/3.0/
 * 
 *      Path:           /App_Code/CMS/Base/UUID.cs
 * 
 *      Change-Log:
 *                      2013-07-21      Created initial class.
 *                      2013-07-30      Added Bytes property and changed the SQLValue property to NumericHexString.
 * 
 * *********************************************************************************************************************
 * A model for representing a universally unique identifier, following the RFC 4122 standard:
 * http://tools.ietf.org/html/rfc4122
 * *********************************************************************************************************************
 */
using System;
using System.Text;

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
            this.hex = hex.ToUpper();
            this.hashcode = int.Parse(hex.Substring(0, 7), System.Globalization.NumberStyles.HexNumber);
        }
        // Methods *****************************************************************************************************
        public override int GetHashCode()
        {
            return hashcode;
        }
        public override bool Equals(object obj)
        {
            UUID t = obj as UUID;
            return t != null && t.hashcode == hashcode && t.hex == hex;
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
        /// The string hex value (0x...) of the UUID.
        /// </summary>
        public string NumericHexString
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
        /// <summary>
        /// The bytes of the hex string. Fetching this value will regenerate the bytes each time (expensive).
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                byte[] buffer = new byte[16];
                for(int i = 0; i < 32; i +=2)
                    buffer[i / 2] = (byte)int.Parse(hex.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                return buffer;
            }
        }
    }
}