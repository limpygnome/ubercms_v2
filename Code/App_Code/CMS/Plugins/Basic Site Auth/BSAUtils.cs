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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/BSAUtils.cs
 * 
 *      Change-Log:
 *                      2013-07-23      Created initial class.
 * 
 * *********************************************************************************************************************
 * A utility class for shared common code within the basic site authentication plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Text.RegularExpressions;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A utility class for shared common code within the basic site authentication plugin.
    /// </summary>
    public static class BSAUtils
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
    }
}