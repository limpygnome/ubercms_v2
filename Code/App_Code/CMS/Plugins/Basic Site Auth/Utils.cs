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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/Utils.cs
 * 
 *      Change-Log:
 *                      2013-07-23      Created initial class.
 * 
 * *********************************************************************************************************************
 * A utility class for shared common code within the basic site authentication plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A utility class for shared common code within the basic site authentication plugin.
    /// </summary>
    public static class Utils
    {
        // Methods - Validation ****************************************************************************************
        /// <summary>
        /// Validates an e-mail address.
        /// </summary>
        /// <param name="email">The e-mail to be tested.</param>
        /// <returns>True = valid, false = invalid.</returns>
        public static bool validEmail(string email)
        {
            const string regexPattern =
                    @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
         + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
         + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
         + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,8})$";
            return Regex.IsMatch(email, regexPattern);
        }
        /// <summary>
        /// Generates a hash for a piece of string data, primarily intended for usage with user passwords.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="data">The string to be hashed.</param>
        /// <param name="uniqueSalt">A salt unique for this hash; this must have a length of at least two!</param>
        /// <returns></returns>
        public static string generateHash(BasicSiteAuth bsa, string data, string uniqueSalt)
        {
            int usLenHalf = uniqueSalt.Length / 2;
            byte[] rawData = Encoding.UTF8.GetBytes(uniqueSalt.Substring(0, usLenHalf) + data + uniqueSalt.Substring(usLenHalf));
            byte[] rawSalt1 = Encoding.UTF8.GetBytes(bsa.Salt1);
            byte[] rawSalt2 = Encoding.UTF8.GetBytes(bsa.Salt2);
            int usLenInd = uniqueSalt.Length - 1;
            // Apply salt
            int s1, s2;
            long buffer;
            for (int i = 0; i < rawData.Length; i++)
            {
                buffer = 0;
                // Change the value of the current byte
                for (s1 = 0; s1 < rawSalt1.Length; s1++)
                    for (s2 = 0; s2 < rawSalt2.Length; s2++)
                        buffer = rawData[i] + ((bsa.Salt1.Length + rawData[i]) * (rawSalt1[s1] + bsa.Salt2.Length) * (rawSalt2[s2] + rawData.Length));
                // Apply third (unique user) hash
                buffer |= uniqueSalt[i % usLenInd];
                // Round it down within numeric range of byte
                while (buffer > byte.MaxValue)
                    buffer -= byte.MaxValue;
                // Check the value is not below 0
                if (buffer < 0) buffer = 0;
                // Reset the byte value
                rawData[i] = (byte)buffer;
            }
            // Hash the byte-array
            HashAlgorithm hasher = new SHA512Managed();
            Byte[] computedHash = hasher.ComputeHash(rawData);
            // Convert to base64 and return
            return Convert.ToBase64String(computedHash);
        }
    }
}