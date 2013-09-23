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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/ArticleTextRenderer.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A text-renderer provider class for the articles system.
 * *********************************************************************************************************************
 */
#if TextRenderer
using System;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;
using CMS.Plugins;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A text-renderer provider class for the articles system.
    /// </summary>
    public class ArticleTextRenderer : RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public ArticleTextRenderer(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            if(renderTypes == RenderType.Objects)
            {
                // Article embedding
                ArticleThread th;
                Article a;
                string[] options;
                StringBuilder content;
                int t;
                string tt;
                foreach (Match m in Regex.Matches(text.ToString(), @"\[\[(.+)\]\]", RegexOptions.Multiline))
                {
                    options = m.Groups[1].Value.Split('|');
                    if (options.Length > 0 && options[0].Length > 0 && (th = ArticleThread.load(data.Connector, options[0])) != null && th.UUIDArticleCurrent != null && (a = Article.load(data.Connector, th.UUIDArticleCurrent, Article.Text.Rendered)) != null)
                    {
                        // Replace params in content
                        content = new StringBuilder(a.TextCache);
                        for (int i = 1; i < options.Length; i++)
                        {
                            tt = options[i];
                            t = tt.IndexOf('=');
                            if (t != -1 && t > 0 && t < tt.Length - 2)
                                content.Replace("{{" + tt.Substring(0, t) + "}}", tt.Substring(t + 1));
                            else
                                content.Replace("{{" + i + "}}", tt);
                        }
                        // Replace with content
                        text.Replace(m.Value, content.ToString());
                        // Merge header data - this will be stripped line-by-line for unique items, no need to worry about duplication (due to ArticleHeader class)
                        header.Append(a.HeaderData.compile());
                    }
                }
            }
        }
    }
}
#endif