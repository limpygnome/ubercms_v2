using System;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;

namespace CMS.Plugins.TRProviders
{
    public class AudioVideo : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public AudioVideo(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder text, RenderType renderTypes)
        {
            // Objects
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                // YouTube
                foreach (Match m in Regex.Matches(text.ToString(), @"\[youtube\]http:\/\/(?:it.|www.)?youtube\.(?:com|.co.uk|.com.br|.fr|.jp|.nl|.pl|.es|.ie)\/watch\?(?:[A-Za-z0-9\&\=_\-]+)?(v=([A-Za-z0-9_\-]+))(?:&[A-Za-z0-9\&\=_\-]+)?\[\/youtube\]", RegexOptions.Multiline))
                    text.Replace(m.Value, @"<iframe width=""420"" height=""315"" src=""//www.youtube.com/embed/" + m.Groups[2].Value + @""" frameborder=""0"" allowfullscreen></iframe>");
                // Vimeo
                foreach (Match m in Regex.Matches(text.ToString(), @"\[vimeo\]http:\/\/(?:www.)?vimeo.com/([0-9]+)\[\/vimeo\]"))
                    text.Replace(m.Value, @"<iframe src=""http://player.vimeo.com/video/" + m.Groups[2].Value + @""" width=""420"" height=""315"" frameborder=""0"" webkitAllowFullScreen mozallowfullscreen allowFullScreen></iframe>");
                // HTML 5 video
                foreach (Match m in Regex.Matches(text.ToString(), @"\[video\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-]+)\[\/video\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<video controls=\"controls\"><source src=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\" >Your browser does not support HTML5 Video - consider using Google Chrome or Mozilla Firefox!</video>");
                // HTML5 audio
                foreach (Match m in Regex.Matches(text.ToString(), @"\[audio\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-]+)\[\/audio\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<audio controls=\"controls\"><source src=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\" >Your browser does not support HTML5 Audio - consider using Google Chrome or Mozilla Firefox!</audio>");
            }
        }
    }
}