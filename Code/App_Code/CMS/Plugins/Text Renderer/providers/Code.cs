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
 *      Path:           /App_Code/CMS/Plugins/Text Renderer/providers/Code.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A text-renderer provider for code content.
 * *********************************************************************************************************************
 */
using CMS.Base;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CMS.Plugins.TRProviders
{
    /// <summary>
    /// A text-renderer provider for code content.
    /// </summary>
    public class Code : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public Code(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            // Objects
            if((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                syntaxHighlighter(data, ref header, ref text);
                syntaxPastebin(data, ref header, ref text);
            }
        }
        // Methods *****************************************************************************************************
        private void syntaxPastebin(Data data, ref StringBuilder header, ref StringBuilder text)
        {
            foreach (Match m in Regex.Matches(text.ToString(), @"\[pastebin\]http://(?:www.)?pastebin.com/([a-zA-Z0-9]+)\[\/pastebin\]", RegexOptions.Singleline))
                text.Replace(m.Value, "<script src=\"http://pastebin.com/embed_js.php?i=" + m.Groups[1].Value + "\"></script>");
        }
        private void syntaxHighlighter(Data data, ref StringBuilder header, ref StringBuilder text)
        {
            // Current version used: 3.0.83
            StringBuilder code;
            string t;
            foreach (Match m in Regex.Matches(text.ToString(), @"\[code=(applescript|as3|bash|coldfusion|cpp|csharp|c#|css|delphi|diff|erlang|groovy|java|javafx|jscript|perl|php|plain|powershell|python|ruby|sass|scala|sql|vb|xml)\](.*?)\[\/code\]", RegexOptions.Singleline))
            {
                code = new StringBuilder(m.Groups[2].Value);
                // Strip to avoid re-rendering of code by other providers by accident
                Escaping.strip(ref code);
				// Preserve line-breaks
                code.Replace("\n", CMS.Plugins.TRProviders.Text.replaceChars);
                // Append start and end tags
                code.Insert(0, "<div class=\"code\"><pre class=\"brush: " + m.Groups[1].Value + "\">");
                code.Append("</pre></div>");
                // Include language core
                string path;
                switch (m.Groups[1].Value)
                {
                    case "applescript":
                        path = "shAppleScript.js";      break;
                    case "as3":
                        path = "shBrushAS3.js";         break;
                    case "bash":
                        path = "shBrushBash.js";        break;
                    case "coldfusion":
                        path = "shBrushColdFusion.js";  break;
                    case "cpp":
                        path = "shBrushCpp.js";         break;
                    case "csharp":
                    case "c#":
                        path = "shBrushCSharp.js";      break;
                    case "css":
                        path = "shBrushCss.js";         break;
                    case "delphi":
                        path = "shBrushDelphi.js";      break;
                    case "diff":
                        path = "shBrushDiff.js";        break;
                    case "erlang":
                        path = "shBrushErlang.js";      break;
                    case "groovy":
                        path = "shBrushGroovy.js";      break;
                    case "java":
                        path = "shBrushJava.js";        break;
                    case "javafx":
                        path = "shBrushJavaFX.js";      break;
                    case "jscript":
                        path = "shBrushJScript.js";     break;
                    case "perl":
                        path = "shBrushPerl.js";        break;
                    case "php":
                        path = "shBrushPhp.js";         break;
                    case "powershell":
                        path = "shBrushPowerShell.js";  break;
                    case "python":
                        path = "shBrushPython.js";      break;
                    case "ruby":
                        path = "shBrushRuby.js";        break;
                    case "sass":
                        path = "shBrushSass.js";        break;
                    case "scala":
                        path = "shBrushScala.js";       break;
                    case "sql":
                        path = "shBrushSql.js";         break;
                    case "vb":
                        path = "shBrushVb.js";          break;
                    case "xml":
                        path = "shBrushXml.js";         break;
                    case "plain":
                    default:
                        path = "shBrushPlain.js";       break;
                }
                BaseUtils.headerAppendJs("/content/js/syntaxhighlighter/shCore.js", ref data, ref header);
                BaseUtils.headerAppendJs("/content/js/syntaxhighlighter/" + path, ref data, ref header);
                BaseUtils.headerAppendCss("/content/css/syntaxhighlighter/shCore.css", ref data, ref header);
                BaseUtils.headerAppendCss("/content/css/syntaxhighlighter/shThemeDefault.css", ref data, ref header);
                // Replace text
                text.Replace(m.Value, code.ToString());
                // Add call to the end of the document (if not already added)
                if (!data.isKeySet("syntaxhighlighter"))
                {
                    data.setFlag("syntaxhighlighter");
                    text.Append("<script type=\"text/javascript\">SyntaxHighlighter.all()</script>");
                }
            }
        }
    }
}