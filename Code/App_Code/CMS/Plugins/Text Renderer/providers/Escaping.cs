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
 *      Path:           /App_Code/CMS/Plugins/Text Renderer/providers/Escaping.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A text-renderer provider for escaping common markup.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;

namespace CMS.Plugins.TRProviders
{
    /// <summary>
    /// A text-renderer provider for escaping common markup.
    /// </summary>
    public class Escaping : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public Escaping(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            StringBuilder formatter;
            foreach (Match m in Regex.Matches(text.ToString(), @"\[(noformat|escape|esc|nobbcode)\](.*?)\[\/\1\]", RegexOptions.Singleline))
            {
                formatter = new StringBuilder(m.Groups[2].Value);
                strip(ref formatter);
                text.Replace(m.Value, formatter.ToString());
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Converts characters typically used for rendering and HTML to HTML entity equivalents.
        /// </summary>
        /// <param name="text">The text to be stripped.</param>
        public static void strip(ref StringBuilder text)
        {
            text.Replace("<", "&lt;").Replace(">", "&gt;").Replace("[", "&#91;").Replace("]", "&#93;").Replace("{", "&#123;").Replace("}", "&#125;");
        }
    }
}