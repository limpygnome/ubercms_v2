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
 *      Path:           /App_Code/CMS/Plugins/Text Renderer/providers/AudioVideo.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A text-renderer provider for audio and video content.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;

namespace CMS.Plugins.TRProviders
{
    /// <summary>
    /// A text-renderer provider for audio and video content.
    /// </summary>
    public class AudioVideo : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public AudioVideo(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            // Objects
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                // YouTube
                foreach (Match m in Regex.Matches(text.ToString(), @"\[youtube\](?:http|https):\/\/(?:it.|www.)?youtube\.(?:com|.co.uk|.com.br|.fr|.jp|.nl|.pl|.es|.ie)\/watch\?(?:[A-Za-z0-9\&\=_\-]+)?(v=([A-Za-z0-9_\-]+))(?:&[A-Za-z0-9\&\=_\-]+)?\[\/youtube\]", RegexOptions.Singleline))
                    text.Replace(m.Value, @"<iframe width=""420"" height=""315"" src=""//www.youtube.com/embed/" + m.Groups[1].Value + @""" frameborder=""0"" allowfullscreen></iframe>");
                // Vimeo
                foreach (Match m in Regex.Matches(text.ToString(), @"\[vimeo\](?:http|https):\/\/(?:www.)?vimeo.com/([0-9]+)\[\/vimeo\]", RegexOptions.Singleline))
                    text.Replace(m.Value, @"<iframe src=""http://player.vimeo.com/video/" + m.Groups[1].Value + @""" width=""420"" height=""315"" frameborder=""0"" webkitAllowFullScreen mozallowfullscreen allowFullScreen></iframe>");
                // HTML 5 video
                foreach (Match m in Regex.Matches(text.ToString(), @"\[video\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-]+)\[\/video\]", RegexOptions.Singleline))
                    text.Replace(m.Value, "<video controls=\"controls\"><source src=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\" >Your browser does not support HTML5 Video - consider using Google Chrome or Mozilla Firefox!</video>");
                // HTML5 audio
                foreach (Match m in Regex.Matches(text.ToString(), @"\[audio\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-]+)\[\/audio\]", RegexOptions.Singleline))
                    text.Replace(m.Value, "<audio controls=\"controls\"><source src=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\" >Your browser does not support HTML5 Audio - consider using Google Chrome or Mozilla Firefox!</audio>");
            }
        }
    }
}