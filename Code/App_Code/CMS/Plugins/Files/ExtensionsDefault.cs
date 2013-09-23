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
 *      Path:           /App_Code/CMS/Plugins/Files/ExtensionsDefault.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A static class used for initially creating file extension models.
 * *********************************************************************************************************************
 */
using System;
using UberLib.Connector;

namespace CMS.Plugins.Files
{
    /// <summary>
    /// A static class used for initially creating file extension models.
    /// </summary>
    public static class ExtensionsDefault
    {
        public static void install(Main main, Connector conn)
        {
            Extension ext;
            // Extensions which can render
            {
                ext = new Extension();
                ext.Ext = "mp4";
                ext.Title = "Video - MP4";
                ext.Render_ClassPath = main.GetType().FullName;
                ext.Render_Method = "fileRender_videoMP4";
                ext.Url_Icon = "/content/images/files/icons/mp4.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "jpg";
                ext.Title = "Image - JPG";
                ext.Render_ClassPath = main.GetType().FullName;
                ext.Render_Method = "fileRender_image";
                ext.Url_Icon = "/content/images/files/icons/image.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "png";
                ext.Title = "Image - PNG";
                ext.Render_ClassPath = main.GetType().FullName;
                ext.Render_Method = "fileRender_image";
                ext.Url_Icon = "/content/images/files/icons/image.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "gif";
                ext.Title = "Image - GIF";
                ext.Render_ClassPath = main.GetType().FullName;
                ext.Render_Method = "fileRender_image";
                ext.Url_Icon = "/content/images/files/icons/image.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "bmp";
                ext.Title = "Image - Bitmap";
                ext.Render_ClassPath = main.GetType().FullName;
                ext.Render_Method = "fileRender_image";
                ext.Url_Icon = "/content/images/files/icons/image.png";
                ext.save(conn);
            }
            // Other...
            {
                ext = new Extension();
                ext.Ext = "bat";
                ext.Title = "Windows Batch File";
                ext.Url_Icon = "/content/images/files/icons/bat.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "bin";
                ext.Title = "Binary File";
                ext.Url_Icon = "/content/images/files/icons/bin.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "cab";
                ext.Title = "Windows Cabinet File";
                ext.Url_Icon = "/content/images/files/icons/bin.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "java";
                ext.Title = "Code - Java";
                ext.Url_Icon = "/content/images/files/icons/code.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "cs";
                ext.Title = "Code - C#";
                ext.Url_Icon = "/content/images/files/icons/code.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "sql";
                ext.Title = "Code - SQL";
                ext.Url_Icon = "/content/images/files/icons/code.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "js";
                ext.Title = "Code - JavaScript";
                ext.Url_Icon = "/content/images/files/icons/code.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "lua";
                ext.Title = "Code - Lua";
                ext.Url_Icon = "/content/images/files/icons/code.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "dll";
                ext.Title = "Windows DLL";
                ext.Url_Icon = "/content/images/files/icons/dll.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "exe";
                ext.Title = "Windows Executable File";
                ext.Url_Icon = "/content/images/files/icons/exe.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "flv";
                ext.Title = "Flash Video";
                ext.Url_Icon = "/content/images/files/icons/flv.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "gz";
                ext.Title = "GNU Zip Archive";
                ext.Url_Icon = "/content/images/files/icons/gz.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "htm";
                ext.Title = "Webpage";
                ext.Url_Icon = "/content/images/files/icons/htm.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "html";
                ext.Title = "Webpage";
                ext.Url_Icon = "/content/images/files/icons/html.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "iso";
                ext.Title = "ISO Archive";
                ext.Url_Icon = "/content/images/files/icons/iso.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "jar";
                ext.Title = "Java Archive";
                ext.Url_Icon = "/content/images/files/icons/jar.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "ngrr";
                ext.Title = "Guitar Rig Present";
                ext.Url_Icon = "/content/images/files/icons/ngrr.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "ogg";
                ext.Title = "Video - Ogg";
                ext.Url_Icon = "/content/images/files/icons/ogg.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "pdf";
                ext.Title = "Portable Document Format";
                ext.Url_Icon = "/content/images/files/icons/pdf.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "rar";
                ext.Title = "WinRAR Archive";
                ext.Url_Icon = "/content/images/files/icons/rar.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "swf";
                ext.Title = "SWF Flash File";
                ext.Url_Icon = "/content/images/files/icons/bin.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "tgz";
                ext.Title = "Tape Archive File";
                ext.Url_Icon = "/content/images/files/icons/bin.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "torrent";
                ext.Title = "Torrent";
                ext.Url_Icon = "/content/images/files/icons/torrent.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "txt";
                ext.Title = "Text File";
                ext.Url_Icon = "/content/images/files/icons/txt.png";
                ext.save(conn);
            }
            {
                ext = new Extension();
                ext.Ext = "zip";
                ext.Title = "ZIP Archive";
                ext.Url_Icon = "/content/images/files/icons/zip.png";
                ext.save(conn);
            }
        }
    }
}