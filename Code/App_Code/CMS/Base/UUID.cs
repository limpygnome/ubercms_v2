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
 *                      2013-08-08      Added improved validation and replaced fatory methods with parse.
 * 
 * *********************************************************************************************************************
 * A model for representing a universally unique identifier, following the RFC 4122 standard:
 * http://tools.ietf.org/html/rfc4122
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using System.Text.RegularExpressions;

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
        /// Parses a UUID object from the string hex representation of the UUID data.
        /// </summary>
        /// <param name="hex">String hex representation of the UUID. Can contain hyphens or no hypens; case insensitive.</param>
        /// <returns>UUID model or null if invalid.</returns>
        public static UUID parse(string hex)
        {
            return isValid(hex) ? (hex.Length == 32 ? new UUID(hex) : new UUID(hex.Replace("-", ""))) : null;
        }
        /// <summary>
        /// Indicates if a UUID is valid.
        /// </summary>
        /// <param name="data">The data to be tested as a UUID.</param>
        /// <returns>True = valid, false = invalid.</returns>
        public static bool isValid(string data)
        {
            // Validate basic structure
            if (data == null || (data.Length != 32 && data.Length != 36))
                return false;
            // Perform regex pattern test
            // -- Matches hyphens or non-hyhens; chars must be 0-9, a-f or/and A-F.
            return Regex.IsMatch(data, @"^(([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})|([0-9a-fA-F]{32}))$");
        }
        // Methods - Generation ****************************************************************************************
        static readonly char[] hexchars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        /// <summary>
        /// Generates a version 4 UUID.
        /// </summary>
        /// <returns>Model of generated UUID.</returns>
        public static UUID generateVersion4()
        {
            // 12 rand, 4, 3 rand, (8, 9, A or B),  3 rand, 12
            StringBuilder sb = new StringBuilder();
            Random rand = new Random((int)DateTime.Now.ToBinary());
            int i;
            for (i = 0; i < 12; i++) sb.Append(hexchars[rand.Next(0, 15)]);
            sb.Append('4');
            for (i = 0; i < 3; i++) sb.Append(hexchars[rand.Next(0, 15)]);
            sb.Append(hexchars[rand.Next(8, 11)]);
            for (i = 0; i < 15; i++) sb.Append(hexchars[rand.Next(0, 15)]);
            return new UUID(sb.ToString());
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