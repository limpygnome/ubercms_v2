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
 *      Path:           /App_Code/CMS/InstallScripts/Base.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * The base install-script class; install-scripts are used to install the CMS.
 * *********************************************************************************************************************
 */
using System;
using System.Text;

namespace CMS.InstallScripts
{
    /// <summary>
    /// The base install-script class; install-scripts are used to install the CMS.
    /// </summary>
    public abstract class Base
    {
        // Methods *****************************************************************************************************
        /// <summary>
        /// Installs the CMS.
        /// </summary>
        /// <param name="messageOutput">Any output from the CMS's installation.</param>
        /// <returns>True = success, false = failed to install CMS correctly.</returns>
        public virtual bool install(ref StringBuilder messageOutput)
        {
            messageOutput.AppendLine("Install method of script not defined.");
            return false;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The title of the install-script.
        /// </summary>
        public virtual string Title
        {
            get
            {
                return "Untitled Install Script";
            }
        }
        /// <summary>
        /// The version of the install-script.
        /// </summary>
        public virtual CMS.Base.Version Version
        {
            get
            {
                return new CMS.Base.Version(1, 0, 0);
            }
        }
        /// <summary>
        /// The author of the install-script; this may contain e-mails, websites or any data at all.
        /// </summary>
        public virtual string Author
        {
            get
            {
                return "unknown";
            }
        }
    }
}