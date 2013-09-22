using CMS.Base;
using CMS.BasicSiteAuth;
using CMS.BasicSiteAuth.Models;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CMS.Plugins.Files
{
    public class FileRP : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public FileRP(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            // Objects
            if ((renderTypes & RenderType.TextFormatting) == RenderType.TextFormatting)
            {
                // Match file-links
                string path;
                File f;
                foreach (Match m in Regex.Matches(text.ToString(), @"\[file_link\=(.*?)\]", RegexOptions.Singleline))
                {
                    path = Main.formatPath(m.Groups[1].Value);
                    // Load the file information
                    f = File.load(data.Connector, path);
                    text.Replace(m.Value, "<a href=\"/files/item" + Main.urlEncodePath(path) + "\">" + (f == null ? "(file not found)" : HttpUtility.HtmlEncode(f.Filename) + " (" + BaseUtils.getBytesString(f.Size) + ")") + "</a>");
                }
            }
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                // Match file-embeds of [file=path]...options...[/file]
                foreach (Match m in Regex.Matches(text.ToString(), @"\[file\=(.*?)\](.*?)\[\/file\]", RegexOptions.Singleline))
                    embed(data, ref text, m, Main.formatPath(m.Groups[1].Value), m.Groups[2].Value);
                // Match file-embeds of [file=path]
                foreach (Match m in Regex.Matches(text.ToString(), @"\[file\=(.*?)\]", RegexOptions.Singleline))
                    embed(data, ref text, m, Main.formatPath(m.Groups[1].Value), null);
            }
        }
        private void embed(Data data, ref StringBuilder text, Match m, string path, string options)
        {
            File f;
            Directory d;
            // Load file and directory
            if ((f = File.load(data.Connector, path)) != null && (d = Directory.load(data.Connector, f.DirectoryID)) != null)
            {
                // Render embed
                if (f.Extension != null && f.Extension.Renderer != null)
                {
                    string obj = (string)f.Extension.Renderer.Invoke(null, new object[] { data, d, f, Main.urlEncodePath(path), path, options });
                    if (obj != null)
                        text.Replace(m.Value, obj);
                }
            }
        }
    }
}