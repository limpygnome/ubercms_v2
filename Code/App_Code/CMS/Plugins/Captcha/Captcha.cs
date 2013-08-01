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
 *      Path:           /App_Code/CMS/Plugins/Captcha/Captcha.cs
 * 
 *      Change-Log:
 *                      2013-07-06      Created initial class.
 *                      2013-07-23      Updated the way settings are handled.
 * 
 * *********************************************************************************************************************
 * A custom captcha plugin for verifying a human user, as protection against automated/machine requests.
 * *********************************************************************************************************************
 */
using System;
using System.Drawing;
using System.Text;
using CMS.Base;

namespace CMS.Plugins
{
    public class Captcha : Plugin
    {
        // Constants ***************************************************************************************************
        public const string CAPTCHA_KEY = "captcha_code";
        // Constants - Settings ****************************************************************************************
        public const string SETTINGS_RANDOM_TEXT_MIN = "captcah_random_text_min";
        public const string SETTINGS_RANDOM_TEXT_MIN__DESCRIPTION = "The minimum number of random characters to generate.";
        public const int SETTINGS_RANDOM_TEXT_MIN__DEFAULT = 5;

        public const string SETTINGS_RANDOM_TEXT_MAX = "captcha_random_text_max";
        public const string SETTINGS_RANDOM_TEXT_MAX__DESCRIPTION = "The maximum number of random characters to generate.";
        public const int SETTINGS_RANDOM_TEXT_MAX__DEFAULT = 8;

        public const string SETTINGS_WIDTH = "captcha_width";
        public const string SETTINGS_WIDTH__DESCRIPTION = "The width of the captcha image.";
        public const int SETTINGS_WIDTH__DEFAULT = 160;

        public const string SETTINGS_HEIGHT = "captcha_height";
        public const string SETTINGS_HEIGHT__DESCRIPTION = "The height of the captcha image.";
        public const int SETTINGS_HEIGHT__DEFAULT = 70;

        public const string SETTINGS_FONT_SIZE_MIN = "captcha_font_size_min";
        public const string SETTINGS_FONT_SIZE_MIN__DESCRIPTION = "he minimum font-size of the captcha text.";
        public const int SETTINGS_FONT_SIZE_MIN__DEFAULT = 20;

        public const string SETTINGS_FONT_SIZE_MAX = "captcha_font_size_max";
        public const string SETTINGS_FONT_SIZE_MAX__DESCRIPTION = "The maximum font-size of the captcha text.";
        public const int SETTINGS_FONT_SIZE_MAX__DEFAULT = 22;
        // Methods - Constructors **************************************************************************************
        public Captcha(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo)
            : base(uuid, title, directory, state, handlerInfo)
        { }
        // Methods - Handlers ******************************************************************************************
        public override bool handler_handleRequest(Data data)
        {
            switch (data.PathInfo.ModuleHandler)
            {
                case "captcha":
                    return pageCaptcha(data);
                default:
                    return false;
            }
        }
        public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            // Install settings
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_RANDOM_TEXT_MIN, SETTINGS_RANDOM_TEXT_MIN__DESCRIPTION, SETTINGS_RANDOM_TEXT_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_RANDOM_TEXT_MAX, SETTINGS_RANDOM_TEXT_MAX__DESCRIPTION, SETTINGS_RANDOM_TEXT_MAX__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_WIDTH, SETTINGS_WIDTH__DESCRIPTION, SETTINGS_WIDTH__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_HEIGHT, SETTINGS_HEIGHT__DESCRIPTION, SETTINGS_HEIGHT__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_FONT_SIZE_MIN, SETTINGS_FONT_SIZE_MIN__DESCRIPTION, SETTINGS_FONT_SIZE_MIN__DEFAULT);
            Core.Settings.setInt(this, Settings.SetAction.AddOrUpdate, SETTINGS_FONT_SIZE_MAX, SETTINGS_FONT_SIZE_MAX__DESCRIPTION, SETTINGS_FONT_SIZE_MAX__DEFAULT);
            Core.Settings.save(conn);
            return true;
        }
        public override bool uninstall(UberLib.Connector.Connector conn, ref StringBuilder messageOutput)
        {
            // Uninstall settings
            Core.Settings.remove(UUID);
            return true;
        }
        public override bool enable(UberLib.Connector.Connector conn, ref StringBuilder messageOutput)
        {
            // Install templates
            if (!Core.Templates.install(conn, this, PathTemplates, ref messageOutput))
                return false;
            // Install directives
            if (!Base.BaseUtils.preprocessorDirective_Add("captcha", ref messageOutput))
                return false;
            // Reserve URLs
            if (!BaseUtils.urlRewritingInstall(this, new string[] { "CAPTCHA" }, ref messageOutput))
                return false;
            return true;
        }
        public override bool disable(UberLib.Connector.Connector conn, ref StringBuilder messageOutput)
        {
            // Unreserve URLs
            if (!BaseUtils.urlRewritingUninstall(this, ref messageOutput))
                return false;
            // Remove directives
            if (!BaseUtils.preprocessorDirective_Remove("CAPTCHA", ref messageOutput))
                return false;
            // Remove templates
            if (!Core.Templates.uninstall(this, ref messageOutput))
                return false;
            return true;
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Indicates if the specified string is the captcah text. The captcha, regardless of being correct, is
        /// reset when invoking this method.
        /// </summary>
        /// <param name="text">The text from the user to verify they're human.</param>
        /// <returns>True if the text is valid, false if the text is not valid.</returns>
        public static bool isCaptchaCorrect(Data data)
        {
            string code = data.Request.Form["captcha"];
            if (code == null || code.Length == 0)
                return false;
            object temp = System.Web.HttpContext.Current.Session[CAPTCHA_KEY];
            if (temp != null)
            {
                System.Web.HttpContext.Current.Session[CAPTCHA_KEY] = null;
                return (string)temp == code;
            }
            return false;
        }
        /// <summary>
        /// Adds any necessary modifications to the page of the current request for the captcha plugin to operate, such
        /// as any directives, styling and JavaScript.
        /// </summary>
        /// <param name="data"></param>
        public static void hookPage(Data data)
        {
            // Setup the captcha variable for template rendering
            data.setFlag("Captcha");
            // Add JavaScript
            BaseUtils.headerAppendJs("/content/js/captcha.js", ref data);
        }
        private readonly string[] captchaFonts = { "Arial", "Verdana", "Times New Roman", "Tahoma", "Helvetica" };
        private bool pageCaptcha(Data data)
        {
            // Generate random string for the captcha
            Random rand = new Random((int)DateTime.Now.ToBinary());
            string text;
            {
                int chars = rand.Next(Core.Settings[SETTINGS_RANDOM_TEXT_MIN].get<int>(), Core.Settings[SETTINGS_RANDOM_TEXT_MAX].get<int>());
                text = BaseUtils.generateRandomString(chars);
            }
            // Setup render parameters
            int width = Core.Settings[SETTINGS_WIDTH].get<int>();
            int height = Core.Settings[SETTINGS_HEIGHT].get<int>();
            int strikesA = rand.Next(15, 35);
            int strikesB = rand.Next(15, 35);
            Font font = new Font(captchaFonts[rand.Next(captchaFonts.Length - 1)], (float)rand.Next(Core.Settings[SETTINGS_FONT_SIZE_MIN].get<int>(), Core.Settings[SETTINGS_FONT_SIZE_MAX].get<int>()), FontStyle.Regular, GraphicsUnit.Pixel);
            // Begin rendering
            Bitmap temp = new Bitmap(width, height);
            Graphics gi = Graphics.FromImage(temp);
            // -- Initial strike-through
            pageCaptchaStrikeThrough(ref rand, strikesA, ref gi, width, height, 1, 2);
            // -- Render text
            int midY = (height / 2) - (int)(gi.MeasureString(text, font).Height / 2);
            int offset = rand.Next(0, 20);
            string charr;
            for (int i = 0; i < text.Length; i++)
            {
                charr = text.Substring(i, 1);
                gi.DrawString(charr, font, new SolidBrush(Color.FromArgb(rand.Next(0, 180), rand.Next(0, 180), rand.Next(0, 180))), new Point(rand.Next(0, 5) + offset, midY + rand.Next(-10, 10)));
                offset += (int)gi.MeasureString(charr, font).Width;
            }
            // -- Final strike-through
            pageCaptchaStrikeThrough(ref rand, strikesB, ref gi, width, height, 1, 1);
            // -- Random colour boxes
            int w2 = width / 2;
            int h2 = height / 2;
            gi.FillRectangle(new SolidBrush(Color.FromArgb(rand.Next(20, 70), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255))), 0, 0, w2, h2);
            gi.FillRectangle(new SolidBrush(Color.FromArgb(rand.Next(20, 70), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255))), w2, h2, w2, h2);
            gi.FillRectangle(new SolidBrush(Color.FromArgb(rand.Next(20, 70), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255))), w2, 0, w2, h2);
            gi.FillRectangle(new SolidBrush(Color.FromArgb(rand.Next(20, 70), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255))), 0, h2, w2, h2);
            int w8 = width / 8;
            int h8 = height / 8;
            int w4 = width / 4;
            int h4 = height / 4;
            for (int i = 0; i < 5; i++)
                gi.FillRectangle(new SolidBrush(Color.FromArgb(rand.Next(10, 40), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255))), rand.Next(w8, w2), rand.Next(h8, h2), rand.Next(w4, width), rand.Next(h4, height));
#if DEBUG
            gi.FillRectangle(new SolidBrush(Color.FromArgb(90, 255, 255, 255)), 0, 0, width, height);
            gi.DrawString("Debug mode:\n" + text, new Font("Verdana", 15.0f, FontStyle.Bold), new SolidBrush(Color.Black), 0.0f, 0.0f);
#endif
            gi.Dispose();
            // Write image to stream
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                temp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.WriteTo(data.Response.OutputStream);
            }
            temp.Dispose();
            // Set session variable
            System.Web.HttpContext.Current.Session[CAPTCHA_KEY] = text;
            // Setup the response parameters
            data.OutputContent = false;
            data.Response.ContentType = "image/png";
            return true;
        }
        private static void pageCaptchaStrikeThrough(ref Random rand, int strikes, ref Graphics gi, int width, int height, int minLineWidth, int maxLineWidth)
        {
            for (int i = 0; i < strikes; i++)
                gi.DrawLine(new Pen(Color.FromArgb(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)), rand.Next(minLineWidth, maxLineWidth)), new Point(rand.Next(0, width), rand.Next(0, height)), new Point(rand.Next(0, width), rand.Next(0, height)));
        }
    }
}