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
 *      Path:           /CMS/Base/Version.cs
 * 
 *      Change-Log:
 *                      2013-07-23      Created initial class.
 *                      
 * *********************************************************************************************************************
 * Version information about the CMS.
 * *********************************************************************************************************************
 */
using System;

namespace CMS.Base
{
    public static class Version
    {
        /// <summary>
        /// The current version major.
        /// </summary>
        public int Major
        {
            get
            {
                return 2;
            }
        }
        /// <summary>
        /// The current version minor.
        /// </summary>
        public int Minor
        {
            get
            {
                return 0;
            }
        }
        /// <summary>
        /// The current version build.
        /// </summary>
        public int Build
        {
            get
            {
                return 0;
            }
        }
    }
}