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
 *                      2013-08-06      Finished initial class.
 *                      
 * *********************************************************************************************************************
 * A model for version information.
 * *********************************************************************************************************************
 */
using System;

namespace CMS.Base
{
    public class Version
    {
        // Fields ******************************************************************************************************
        private int     major,  // The major release version.
                        minor,  // The minor release version.
                        build;  // The build release version.
        // Methods - Constructors **************************************************************************************
        public Version()
        {
            this.major = this.minor = this.build = 0;
        }
        public Version(int major, int minor, int build)
        {
            this.major = major;
            this.minor = minor;
            this.build = build;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The major release version.
        /// </summary>
        public int Major
        {
            get
            {
                return major;
            }
            set
            {
                major = value;
            }
        }
        /// <summary>
        /// The minor release version.
        /// </summary>
        public int Minor
        {
            get
            {
                return minor;
            }
            set
            {
                minor = value;
            }
        }
        /// <summary>
        /// The build release version.
        /// </summary>
        public int Build
        {
            get
            {
                return build;
            }
            set
            {
                build = value;
            }
        }
    }
}