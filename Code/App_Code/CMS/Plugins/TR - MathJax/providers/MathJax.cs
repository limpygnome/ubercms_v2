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
 *      Path:           /App_Code/CMS/Plugins/TR - MathJax/providers/MathJax.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A text-renderer provider for MathJax, used for rendering mathematical syntax.
 * *********************************************************************************************************************
 */
using CMS.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CMS.Plugins.TRProviders
{
    /// <summary>
    /// A text-renderer provider for MathJax, used for rendering mathematical LaTeX syntax.
    /// </summary>
    public class MathJax : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public MathJax(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                // Search for evidence of MathJax use; if found, add to header
                bool found = false;
                if (Regex.IsMatch(text.ToString(), @"\$\$(?:.+?)\$\$", RegexOptions.Singleline))
                    found = true;
                if (Regex.IsMatch(text.ToString(), @"\\begin", RegexOptions.Singleline))
                    found = true;
                if (found)
                {
                    string hdata = @"
<script type=""text/x-mathjax-config"">
  MathJax.Hub.Config({
    tex2jax: {
      inlineMath: [ ['$','$'], [""\\("",""\\)""] ],
      processEscapes: true
    }
  });
</script>
<script type=""text/javascript""
   src=""http://cdn.mathjax.org/mathjax/latest/MathJax.js?config=TeX-AMS_HTML"">
</script>";
                    hdata = hdata.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                    BaseUtils.headerAppend(hdata, ref data, ref header, "mathjax");
                }
            }
        }
    }
}