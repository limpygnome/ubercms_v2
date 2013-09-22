using CMS.Base;
using CMS.BasicSiteAuth;
using CMS.BasicSiteAuth.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using UberLib.Connector;

namespace CMS.Plugins.Files
{
    public class Main : Plugin
    {
        // Enums *******************************************************************************************************
        public enum Flags
        {
            None = 0,
            NonExistent = 1
        }
        // Methods - Constructors **************************************************************************************
        public Main() { }
        public Main(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        { }
        // Methods - Variables *****************************************************************************************
        private bool readOnly = false;      // Indicates if the files-system is being rebuilt and in read-only mode.
        private Thread rbThread = null;     // The rebuild thread.
        // Methods - CMS ***********************************************************************************************
        public override bool install(Connector conn, ref StringBuilder messageOutput)
        {
            // Install SQL
            if (!BaseUtils.executeSQL(PathSQL + "/install.sql", conn, ref messageOutput))
                return false;
            // Install settings

            // Install extensions
            ExtensionsDefault.install(this, conn);
            // Install text-renderer provider
#if TextRenderer
            TextRenderer tr = (TextRenderer)Core.Plugins[UUID.parse(TextRenderer.TR_UUID)];
            if (tr != null)
            {
                RenderProvider rp = new FileRP(UUID.parse("eea4cf35-2d32-4238-b0f2-9bf5aea45b3a"), this.UUID, "Files Embedding", "Allows embedding of files from the files-plugin.", true, 0);
                if (!rp.save(tr, conn))
                {
                    messageOutput.AppendLine("Failed to create '" + rp.Title + "' text renderer provider!");
                    return false;
                }
            }
#endif
            return true;
        }
        public override bool uninstall(Connector conn, ref StringBuilder messageOutput)
        {
            // Uninstall SQL
            if (!BaseUtils.executeSQL(PathSQL + "/uninstall.sql", conn, ref messageOutput))
                return false;
            // Uninstall settings
            Core.Settings.remove(conn, this);
            // Remove text-renderer
#if TextRenderer
            TextRenderer tr = (TextRenderer)Core.Plugins[UUID.parse(TextRenderer.TR_UUID)];
            if (tr != null)
                tr.providersRemove(conn, UUID);
#endif
            return true;
        }
        public override bool enable(Connector conn, ref StringBuilder messageOutput)
        {
            // Install templates
            if (!Core.Templates.install(conn, this, PathTemplates, ref messageOutput))
                return false;
            // Install content
            if (!BaseUtils.contentInstall(PathContent, Core.PathContent, true, ref messageOutput))
                return false;
            // Install URLs
            if (!BaseUtils.urlRewritingInstall(conn, this, new string[] { "files" }, ref messageOutput))
                return false;
            // Check files dir exists
            if (!System.IO.Directory.Exists(PathFiles))
                System.IO.Directory.CreateDirectory(PathFiles);
            return true;
        }
        public override bool disable(Connector conn, ref StringBuilder messageOutput)
        {
            // Uninstall URL rewriting
            if (!BaseUtils.urlRewritingUninstall(conn, this, ref messageOutput))
                return false;
            // Uninstall content
            if (!BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput))
                return false;
            // Uninstall templates
            if (!Core.Templates.uninstall(conn, this, ref messageOutput))
                return false;
            return true;
        }
        public override bool handler_handleRequest(Data data)
        {
            // Load BSA user model
            User user = BasicSiteAuth.BasicSiteAuth.getCurrentUser(data);
            // Handle page
            switch (data.PathInfo[0])
            {
                case "files":
                    switch (data.PathInfo[1])
                    {
                        case null:
                        case "view":
                            if (!pageExplorer(data, user))
                                return false;
                            break;
                        case "item":
                            if (!pageItem(data, user))
                                return false;
                            break;
                        case "upload":
                            if (!pageUpload(data, user))
                                return false;
                            break;
                        default:
                            return false;
                    }
                    break;
                default:
                    return false;
            }
            // Add styling
            BaseUtils.headerAppendCss("/content/css/files.css", ref data);
            return true;
        }
        // Methods - Pages *********************************************************************************************
        private bool pageUpload(Data data, User user)
        {
            // Check the user is allowed to upload files
            if (user == null || !user.UserGroup.Administrator)
                return false;
            // Check for postback
            string error = null;
            string success = null;
            HttpPostedFile pbFile = data.Request.Files["files_upload"];
            string pbDestination = data.Request.Form["files_destination"];
            bool pbOptionsOverwrite = data.Request.Form["files_options_overwrite"] != null;
            bool pbOptionsRedirect = data.Request.Form["files_options_redirect"] != null;
            if (pbDestination != null && pbFile != null && pbFile.ContentLength > 0 && pbFile.FileName != null && pbFile.FileName.Length > 0)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    // Check the format of the path
                    if (!pbDestination.StartsWith("/"))
                        pbDestination = "/" + pbDestination;
                    if (pbDestination.Length > 1 && pbDestination.EndsWith("/"))
                        pbDestination = pbDestination.Substring(0, pbDestination.Length - 1);
                    // Build paths
                    string destRel = pbDestination + (pbDestination.Length > 1 ? "/" : string.Empty) + pbFile.FileName;
                    string dest = PathFiles + destRel;
                    string destDir = PathFiles + pbDestination;
                    // Check the directory exists, else create it
                    bool newDir = false;
                    try
                    {
                        if (!System.IO.Directory.Exists(destDir))
                        {
                            System.IO.Directory.CreateDirectory(destDir);
                            newDir = true;
                        }
                    }
                    catch(Exception ex)
                    {
                        error = "Could not check/create destination directory - '" + ex.Message + "'!";
                    }
                    if(error == null)
                    {
                        // Attempt to save the file to the destination - check no file exists
                        bool exists;
                        if ((exists = ((File.load(data.Connector, destRel) != null || System.IO.File.Exists(dest)))) && !pbOptionsOverwrite)
                            error = "File already exists!";
                        else
                        {
                            if (exists)
                            {
                                try
                                {
                                    System.IO.File.Delete(dest);
                                }
                                catch(Exception ex)
                                {
                                    error = "A file already exists at the specified path; failed to delete it: '" + ex.Message + "'!";
                                }
                            }
                            if (error == null)
                            {
                                // Save to file-system
                                pbFile.SaveAs(dest);
                                // Run rebuild
                                if (!rebuild(newDir ? "/" : pbDestination))
                                    error = "Successfully uploaded file, but failed to run rebuild on structure - please run it manually!";
                                if (pbOptionsRedirect)
                                    BaseUtils.redirectAbs(data, "/files/item" + urlEncodePath(pbDestination) + (pbDestination.Length > 1 ? "/" : string.Empty) + HttpUtility.UrlEncode(pbFile.FileName));
                                else
                                    success = "Successfully uploaded file '" + pbFile.FileName + "'!";
                            }
                        }
                    }
                }
            }
            // Set data
            data["Title"] = "Files - Upload New File";
            data["Content"] = Core.Templates.get(data.Connector, "files/upload");
            data["files_destination"] = pbDestination != null && pbDestination.Length > 0 ? HttpUtility.HtmlEncode(pbDestination) : data.Request.QueryString["dest"] != null ? HttpUtility.HtmlEncode(data.Request.QueryString["dest"]) : "/";
            if (pbOptionsOverwrite)
                data.setFlag("files_options_overwrite");
            if (pbOptionsRedirect)
                data.setFlag("files_options_redirect");
            if (error != null)
                data["files_error"] = HttpUtility.HtmlEncode(error);
            if (success != null)
                data["files_success"] = HttpUtility.HtmlEncode(success);
            return true;
        }
        // Methods - Pages - Explorer **********************************************************************************
        public bool pageExplorer(Data data, User user)
        {
            // Build the path being viewed
            string path;
            string pathRaw;
            {
                StringBuilder bufferRaw = new StringBuilder("/");
                StringBuilder buffer = new StringBuilder("/");
                int i = 2;
                string t;
                while ((t = data.PathInfo[i++]) != null)
                {
                    bufferRaw.Append(t).Append('/');
                    buffer.Append(HttpUtility.UrlEncode(t)).Append('/');
                }
                if (buffer.Length > 1)
                    buffer.Remove(buffer.Length - 1, 1);
                if (bufferRaw.Length > 1)
                    bufferRaw.Remove(bufferRaw.Length - 1, 1);
                path = buffer.ToString();
                pathRaw = bufferRaw.ToString();
            }
            // Fetch the info of the current directory
            Directory dir = Directory.load(data.Connector, pathRaw);
            if (dir == null)
                return false;
            // Check for action
            switch (data.Request.QueryString["action"])
            {
                case "view":
                case null:
                    if (!pageExplorer_view(data, user, dir, path, pathRaw))
                        return false;
                    break;
                case "rebuild":
                    if (!pageExplorer_rebuild(data, user, dir, path, pathRaw))
                        return false;
                    break;
                case "edit":
                    if (!pageExplorer_edit(data, user, dir, path, pathRaw))
                        return false;
                    break;
                case "delete":
                    if (!pageExplorer_delete(data, user, dir, path, pathRaw))
                        return false;
                    break;
                default:
                    return false;
            }
            // Set common data
            data["explorer_url"] = "/files/view" + path;
            return true;
        }
        public bool pageExplorer_view(Data data, User user, Directory dir, string path, string pathRaw)
        {
            // Fetch and build dirs view
            {
                StringBuilder viewDirs = new StringBuilder();
                string template = Core.Templates.get(data.Connector, "files/explorer_directory");
                StringBuilder buffer;
                foreach (Directory d in dir.getSubDirs(data.Connector))
                {
                    buffer = new StringBuilder(template);
                    buffer.Replace("<DIRID>", d.DirectoryID.ToString());
                    buffer.Replace("<DIRECTORY_NAME>", HttpUtility.HtmlEncode(d.DirectoryName));
                    buffer.Replace("<DIRECTORY_URL>", "/files/view" + urlEncodePath(d.DirectoryPath));
                    buffer.Replace("<DESCRIPTION>", formatDescription(d.Description));
                    viewDirs.Append(buffer.ToString());
                }
                if (viewDirs.Length > 0)
                    data["files_explorer_dirs"] = viewDirs.ToString();
            }
            // Fetch and build files view
            {
                StringBuilder viewFiles = new StringBuilder();
                string template = Core.Templates.get(data.Connector, "files/explorer_file");
                StringBuilder buffer;
                foreach (File f in dir.getFiles(data.Connector))
                {
                    buffer = new StringBuilder(template);
                    buffer.Replace("<FILEID>", f.FileID.ToString());
                    buffer.Replace("<FILE>", HttpUtility.HtmlEncode(f.Filename));
                    buffer.Replace("<FILE_URL>", "/files/item" + (path.Length > 1 ? path + "/" : "/") + HttpUtility.UrlEncode(f.Filename));
                    buffer.Replace("<DESCRIPTION>", formatDescription(f.Description));
                    buffer.Replace("<SIZE>", HttpUtility.HtmlEncode(BaseUtils.getBytesString(f.Size)));
                    buffer.Replace("<DATETIME_CREATED>", HttpUtility.HtmlEncode(BaseUtils.dateTimeToHumanReadable(f.DateTime_Created)));
                    buffer.Replace("<DATETIME_CREATED_FULL>", HttpUtility.HtmlEncode(f.DateTime_Created.ToString()));
                    buffer.Replace("<DATETIME_MODIFIED>", HttpUtility.HtmlEncode(BaseUtils.dateTimeToHumanReadable(f.DateTime_Modified)));
                    buffer.Replace("<DATETIME_MODIFIED_FULL>", HttpUtility.HtmlEncode(f.DateTime_Modified.ToString()));
                    if (f.Extension != null)
                    {
                        buffer.Replace("<EXTENSION>", HttpUtility.HtmlEncode(f.Extension.Title));
                        buffer.Replace("<EXTENSION_ICON_URL>", f.Extension.Url_Icon != null && f.Extension.Url_Icon.Length > 0 ? f.Extension.Url_Icon : "/content/images/files/icons/unknown.png");
                    }
                    else
                    {
                        buffer.Replace("<EXTENSION>", "Unknown");
                        buffer.Replace("<EXTENSION_ICON_URL>", "/content/images/files/icons/unknown.png");
                    }
                    viewFiles.Append(buffer.ToString());
                }
                if (viewFiles.Length > 0)
                    data["files_explorer_files"] = viewFiles.ToString();
            }
            // Set data
            data["Title"] = "Files - View - " + path;
            data["Content"] = Core.Templates.get(data.Connector, "files/explorer");
            if (user != null && user.UserGroup.Administrator)
                data.setFlag("files_explorer_admin");
            if (path.Length > 1)
            {
                int li = path.LastIndexOf('/');
                if (li != -1)
                    data["files_explorer_url_top"] = "/files/view" + path.Substring(0, li);
            }
            if (dir.Description != null && dir.Description.Length > 0)
                data["files_explorer_description"] = formatDescription(dir.Description);
            data["explorer_path"] = path;
            return true;
        }
        private bool pageExplorer_edit(Data data, User user, Directory dir, string path, string pathRaw)
        {
            // Check the user is an administrator
            if (user == null || !user.UserGroup.Administrator)
                return false;
            // Check for postback
            string error = null;
            string pbDescription = data.Request.Form["explorer_description"];
            if (pbDescription != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    dir.Description = pbDescription;
                    if (!dir.save(data.Connector))
                        error = "Failed to persist changes!";
                    else
                        BaseUtils.redirectAbs(data, "/files/view" + path);
                }
            }
            // Set data
            data["Title"] = "Files - Edit - " + path;
            data["Content"] = Core.Templates.get(data.Connector, "files/explorer_edit");
            if (error != null)
                data["explorer_error"] = HttpUtility.HtmlEncode(error);
            data["directory_description"] = HttpUtility.HtmlEncode(pbDescription ?? dir.Description ?? string.Empty);
            return true;
        }
        private bool pageExplorer_delete(Data data, User user, Directory dir, string path, string pathRaw)
        {
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Check the user is an administrator
            if (user == null || !user.UserGroup.Administrator)
                return false;
            // Check for postback
            string error = null;
            if (data.Request.Form["files_delete"] != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Invalid captcha verification code!";
#endif
                if (error == null)
                {
                    // Delete the directory
                    if (readOnly)
                        error = "Unable to delete directory; the file-system is in read-only mode!";
                    else if (!directoryDelete(dir))
                        error = "Failed to delete directory, an error occurred!";
                    else
                        // Redirect to top directory
                        BaseUtils.redirectAbs(data, "/files/view" + pathRoot(path));
                }
            }
            // Set data
            data["Title"] = "Files - Delete - " + path;
            data["Content"] = Core.Templates.get(data.Connector, "files/explorer_delete");
            if (error != null)
                data["explorer_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageExplorer_rebuild(Data data, User user, Directory dir, string path, string pathRaw)
        {
            // Check the user is an administrator
            if (user == null || !user.UserGroup.Administrator)
                return false;
            // Check for postback
            string error = null;
            if (data.Request.Form["files_rebuild"] != null)
            {
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    // Launch rebuilder thread if available
                    if (readOnly)
                        error = "Unable to rebuild files; the file-system is in read-only mode (possibly already rebuilding)!";
                    else if (!rebuild(pathRaw))
                        error = "An error occurred launching the rebuilder!";
                    else
                    {
                        // Redirect to the directory
                        BaseUtils.redirectAbs(data, "/files/view" + path);
                    }
                }
            }
            // Set data
            data["Title"] = "Files - Rebuild - " + path;
            data["Content"] = Core.Templates.get(data.Connector, "files/explorer_rebuild");
            if (error != null)
                data["explorer_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        // Methods - Pages - Item **************************************************************************************
        private bool pageItem(Data data, User user)
        {
            // Compile file-path
            string path;
            string pathRaw;
            {
                StringBuilder buffer = new StringBuilder("/");
                StringBuilder bufferRaw = new StringBuilder("/");
                int i = 2;
                string t;
                while ((t = data.PathInfo[i++]) != null)
                {
                    buffer.Append(HttpUtility.UrlEncode(t)).Append('/');
                    bufferRaw.Append(t).Append('/');
                }
                if (buffer.Length > 1)
                    buffer.Remove(buffer.Length - 1, 1);
                if (bufferRaw.Length > 1)
                    bufferRaw.Remove(bufferRaw.Length - 1, 1);
                path = buffer.ToString();
                pathRaw = bufferRaw.ToString();
            }
            // Load file and directory models
            File file;
            Directory dir;
            if((file = File.load(data.Connector, pathRaw)) == null || (dir = Directory.load(data.Connector, file.DirectoryID)) == null)
                return false;
            // Handle action
            switch (data.Request.QueryString["action"])
            {
                case null:
                case "view":
                    if (!pageItem_view(data, user, dir, file, path, pathRaw))
                        return false;
                    break;
                case "edit":
                    if (!pageItem_edit(data, user, dir, file, path, pathRaw))
                        return false;
                    break;
                case "delete":
                    if (!pageItem_delete(data, user, dir, file, path, pathRaw))
                        return false;
                    break;
                case "download":
                    if (!pageItem_download(data, user, dir, file, path, pathRaw, true))
                        return false;
                    break;
                case "stream":
                    if (!pageItem_download(data, user, dir, file, path, pathRaw, false))
                        return false;
                    break;
                default:
                    return false;
            }
            // Set data
            data["files_item_url"] = "/files/item" + path;
            // Add styling
            BaseUtils.headerAppendCss("/content/css/files.css", ref data);
            return true;
        }
        private bool pageItem_view(Data data, User user, Directory dir, File file, string path, string pathRaw)
        {
            // Fetch media renderer for displaying the content
            if (file.Extension != null && file.Extension.Renderer != null)
            {
                string obj = (string)file.Extension.Renderer.Invoke(null, new object[] { data, dir, file, path, pathRaw, null });
                if (obj != null)
                    data["files_item_render"] = obj;
            }
            // Set data
            data["Title"] = "Files - " + HttpUtility.HtmlEncode(file.Filename);
            data["Content"] = Core.Templates.get(data.Connector, "files/item_view");
            string url = pathRoot(path);
            if (file.Description != null && file.Description.Length > 0)
                data["files_item_description"] = formatDescription(file.Description);
            data["files_item_path"] = url;
            data["files_item_path_url"] = "/files/view" + url;
            data["files_item_embed_path"] = path;
            data["files_item_size"] = HttpUtility.HtmlEncode(BaseUtils.getBytesString(file.Size));
            data["files_item_created"] = HttpUtility.HtmlEncode(BaseUtils.dateTimeToHumanReadable(file.DateTime_Created));
            data["files_item_created_full"] = HttpUtility.HtmlEncode(file.DateTime_Created.ToString());
            data["files_item_modified"] = HttpUtility.HtmlEncode(BaseUtils.dateTimeToHumanReadable(file.DateTime_Modified));
            data["files_item_modified_full"] = HttpUtility.HtmlEncode(file.DateTime_Modified.ToString());
            if (user != null && user.UserGroup.Administrator)
                data.setFlag("files_item_admin");
            return true;
        }
        private bool pageItem_edit(Data data, User user, Directory dir, File file, string path, string pathRaw)
        {
            // Check the user is an administrator
            if (user == null || !user.UserGroup.Administrator)
                return false;
            // Check postback
            string error = null;
            string pbDescription = data.Request.Form["files_item_description"];
            if (pbDescription != null)
            {
                // Check security
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
                if (error == null)
                {
                    file.Description = pbDescription;
                    if (!file.save(data.Connector))
                        error = "Failed to persist changes!";
                    else
                        BaseUtils.redirectAbs(data, "/files/item" + path);
                }
            }
            // Set data
            data["Title"] = "Files - " + HttpUtility.HtmlEncode(file.Filename) + " - Edit";
            data["Content"] = Core.Templates.get(data.Connector, "files/item_edit");
            data["files_item_description"] = HttpUtility.HtmlEncode(pbDescription ?? file.Description ?? string.Empty);
            if (error != null)
                data["files_error"] = HttpUtility.HtmlEncode(error);
            return true;
        }
        private bool pageItem_delete(Data data, User user, Directory dir, File file, string path, string pathRaw)
        {
#if CAPTCHA
            Captcha.hookPage(data);
#endif
            // Check the user is an administrator
            if (user == null || !user.UserGroup.Administrator)
                return false;
            // Check postback
            string error = null;
            if (data.Request.Form["file_delete"] != null)
            {
                // Check security
#if CSRFP
                if (!CSRFProtection.authenticated(data))
                    error = "Invalid request; please try again!";
#endif
#if CAPTCHA
                if (error == null && !Captcha.isCaptchaCorrect(data))
                    error = "Invalid captcha verification code!";
#endif
                if (error == null)
                {
                    fileDelete(dir, file);
                    BaseUtils.redirectAbs(data, "/files/view" + pathRoot(path));
                }
            }
            // Set data
            data["Title"] = "Files - " + HttpUtility.HtmlEncode(file.Filename) + " - Delete";
            data["Content"] = Core.Templates.get(data.Connector, "files/item_delete");
            if (error != null)
                data["files_error"] = HttpUtility.HtmlEncode(error);
            data["files_item_fullpath"] = HttpUtility.HtmlEncode(path);
            return true;
        }
        private bool pageItem_download(Data data, User user, Directory dir, File file, string path, string pathRaw, bool countDownload)
        {
            string fullPath = dir.PhysicalPath + "/" + file.Filename;
            // Output the file
            data.OutputContent = false;
            if (!System.IO.File.Exists(fullPath))
            {
                data.Response.StatusCode = 500;
                data.Response.Write("Failed to physically locate the file, please try your request again or contact us immediately!");
                return true;
            }
            try
            {
                // Open a stream to the file
                FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                if (!fs.CanRead)
                {
                    data.Response.StatusCode = 500;
                    data.Response.Write("Unable to read file, please try again or contact us immediately!");
                    return true;
                }
                // Clear the response so far, as well as disable the buffer
                data.Response.Clear();
                data.Response.Buffer = false;
                // Based on RFC 2046 - page 4 - media types - point 5 - http://www.rfc-editor.org/rfc/rfc2046.txt - http://stackoverflow.com/questions/1176022/unknown-file-type-mime
                data.Response.ContentType = "application/octet-stream";
                // Begin writing the file-stream to the client
                BinaryReader bin = new BinaryReader(fs);
                long startRead = 0;
                // Read the range of bytes requested - allows downloads to be continued
                try
                {
                    if (data.Response.Headers["Range"] != null)
                    {
                        string[] range = data.Response.Headers["Range"].Split(new char[] { '=', '-' }); // RFC 2616 - section 14.35
                        // Ensure there are at least two parts
                        if (range.Length >= 2)
                        {
                            // Attempt to parse the requested bytes
                            long.TryParse(range[1], out startRead);
                            // Ensure its inclusive of 0 to size of file, else reset it to zero
                            if (startRead < 0 || startRead >= fs.Length) startRead = 0;
                            else
                                // Write the range of bytes being sent - RFC 2616 - section 14.16
                                data.Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startRead, fs.Length - 1, fs.Length));
                        }
                    }
                }
                catch (PlatformNotSupportedException) { }
                // Specify the number of bytes being sent
                data.Response.AddHeader("Content-Length", (fs.Length - startRead).ToString());
                // Specify other headers
                string lastModified = file.DateTime_Modified.ToString();
                data.Response.AddHeader("Connection", "Keep-Alive");
                data.Response.AddHeader("Last-Modified", lastModified);
                data.Response.AddHeader("ETag", HttpUtility.UrlEncode(path, System.Text.Encoding.UTF8) + lastModified); // Unique entity identifier
                data.Response.AddHeader("Accept-Ranges", "bytes");
                data.Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(file.Filename));
                data.Response.StatusCode = 206;
                // Start the stream at the offset
                bin.BaseStream.Seek(startRead, SeekOrigin.Begin);
                const int chunkSize = 16384; // 16kb will be transferred at a time
                // Write bytes whilst the user is connected in chunks of 1024 bytes
                int maxChunks = (int)Math.Ceiling((double)(fs.Length - startRead) / chunkSize);
                int i;
                for (i = 0; i < maxChunks && data.Response.IsClientConnected; i++)
                {
                    data.Response.BinaryWrite(bin.ReadBytes(chunkSize));
                    data.Response.Flush();
                }
                //if (i >= maxChunks)
                //{
                    // Download was successful - log it
                //}
                bin.Close();
                fs.Close();
            }
            catch(Exception ex)
            {
                data.Response.StatusCode = 500;
                data.Response.Write("Failed to get file, please try your request again or contact us immediately!" + ex.Message + " - " + ex.StackTrace);
            }
            return true;
        }
        // Methods - File Renderer's ************************************************************************************
        public static string fileRender_image(Data data, Directory dir, File file, string path, string pathRaw, string options)
        {
            StringBuilder buffer = new StringBuilder(Core.Templates.get(data.Connector, "files/render_image"));
            buffer.Replace("<FILENAME>", HttpUtility.HtmlEncode(file.Filename));
            buffer.Replace("<URL>", HttpUtility.HtmlEncode("/files/item" + path + "?action=stream"));
            return buffer.ToString();
        }
        public static string fileRender_videoMP4(Data data, Directory dir, File file, string path, string pathRaw, string options)
        {
            StringBuilder buffer = new StringBuilder(Core.Templates.get(data.Connector, "files/render_video_mp4"));
            buffer.Replace("<FILENAME>", HttpUtility.HtmlEncode(file.Filename));
            buffer.Replace("<URL>", HttpUtility.HtmlEncode("/files/item" + path + "?action=stream"));
            return buffer.ToString();
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Cleans-up a raw relative path.
        /// 
        /// Note: can be used for files and directories.
        /// </summary>
        /// <param name="path">Raw relative path (no URL encoding etc).</param>
        /// <returns>Formatted path.</returns>
        public static string formatPath(string path)
        {
            if (!path.StartsWith("/"))
                path = "/" + path;
            if (path.Length > 1 && path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);
            return path;
        }
        /// <summary>
        /// Formats a description, including HTML encoding.
        /// </summary>
        /// <param name="text">The description to be formatted.</param>
        /// <returns>The description formatted.</returns>
        public static string formatDescription(string text)
        {
            if (text == null)
                return string.Empty;
            return HttpUtility.HtmlEncode(text).Replace("\r", string.Empty).Replace("\n", "<br />");
        }
        /// <summary>
        /// Encodes a raw path to a url-encoded path.
        /// </summary>
        /// <param name="path">The raw relative path.</param>
        /// <returns>Url-encoded relative path.</returns>
        public static string urlEncodePath(string path)
        {
            StringBuilder buffer = new StringBuilder();
            string[] parts = path.Split('/');
            foreach (string p in parts)
                buffer.Append(HttpUtility.UrlEncode(p)).Append('/');
            if (buffer.Length > 0)
                buffer.Remove(buffer.Length - 1, 1);
            return buffer.ToString();
        }
        /// <summary>
        /// Gets the root directory of a path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Root path of path provided.</returns>
        public static string pathRoot(string path)
        {
            if (path.Length > 1)
            {
                int ind = path.LastIndexOf('/');
                if (ind != -1 && ind > 0)
                    return path.Substring(0, ind);
                else
                    return "/";
            }
            else
                return "/";
        }
        /// <summary>
        /// Deletes a directory and launches rebuilding.
        /// </summary>
        /// <param name="dir">The directory to delete.</param>
        /// <returns>True = success, false = error.</returns>
        public bool directoryDelete(Directory dir)
        {
            lock (this)
            {
                if (dir == null || !System.IO.Directory.Exists(dir.PhysicalPath))
                    return false;
                // Delete directory
                try
                {
                    System.IO.Directory.Delete(dir.PhysicalPath, true);
                }
                catch { }
                try
                {
                    if (dir.DirectoryPath == "/")
                        System.IO.Directory.CreateDirectory(PathFiles);
                }
                catch { }
                // Rebuild parent directory
                return rebuild(pathRoot(dir.DirectoryPath));
            }
        }
        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="dir">The directory of the file to be deleted.</param>
        /// <param name="file">The file to be deleted.</param>
        /// <returns>True = success, false = error.</returns>
        public bool fileDelete(Directory dir, File file)
        {
            lock (this)
            {
                string path = dir.PhysicalPath + "/" + file.Filename;
                if (file == null || !System.IO.File.Exists(path))
                    return false;
                // Delete file
                try
                {
                    System.IO.File.Delete(path);
                }
                catch { }
                // Rebuild directory
                return rebuild(dir.DirectoryPath);
            }
        }
        // Methods - Rebuilding ****************************************************************************************
        public bool rebuild(string path)
        {
            lock (this)
            {
                if (readOnly)
                    return false;
                readOnly = true;
                rbThread = new Thread(delegate()
                    {
                        rebuildThreadWorker(path);
                    });
                rbThread.Start();
                return true;
            }
        }
        private void rebuildThreadWorker(string relativePath)
        {
            if (relativePath == null || relativePath.Length == 0)
                relativePath = "/";
            else if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;
            relativePath = relativePath.Replace("\\", "/");
            // Create connector
            Connector conn = Core.connectorCreate(true);
            // Begin transaction
            conn.queryExecute("BEGIN;");
            // Set non-existent flag on all files and dirs being rebuilt
            int ne = (int)Flags.NonExistent;
            PreparedStatement ps = new PreparedStatement("UPDATE fi_dir SET flags = (flags | ?ne) WHERE (dir_path=?path) OR (dir_path LIKE ?pathlk); UPDATE fi_file AS f SET f.flags = (flags | ?ne) WHERE (SELECT COUNT('') FROM fi_dir AS d WHERE d.dirid=f.dirid AND (d.dir_path LIKE ?pathlk OR d.dir_path=?path))=1;");
            ps["ne"] = ne;
            ps["path"] = relativePath;
            ps["pathlk"] = relativePath.Length == 1 ? "/%" : relativePath + "/%";
            conn.queryExecute(ps);
            // Recursively check each file
            rebuildThreadWorkerRecurse(conn, PathFiles.Length, PathFiles + relativePath, -1);
            // Drop any dirs and files with non-existent flag
            ps = new PreparedStatement("DELETE FROM fi_dir WHERE (flags & ?ne)=?ne; DELETE FROM fi_file WHERE (flags & ?ne)=?ne;");
            ps["ne"] = ne;
            conn.queryExecute(ps);
            // Commit changes
            conn.queryExecute("COMMIT;");
            // Reset read-only mode
            lock (this)
            {
                rbThread = null;
                readOnly = false;
            }
        }
        private void rebuildThreadWorkerRecurse(Connector conn, int baseLength, string dir, int parentDirid)
        {
            // Ensure the directory exists
            if (!System.IO.Directory.Exists(dir))
                return;
            // Lookup the dirid and check it exists
            try
            {
                PreparedStatement ps;
                int dirid;
                {
                    string relDir = dir.Substring(baseLength);
                    ps = new PreparedStatement("SELECT dirid FROM fi_dir WHERE dir_path=?dir_path;");
                    ps["dir_path"] = relDir;
                    Result res = conn.queryRead(ps);
                    if (res.Count != 1)
                    {
                        SQLCompiler sql = new SQLCompiler();
                        sql["dir_path"] = relDir;
                        sql["dir_name"] = System.IO.Path.GetFileName(relDir);
                        if (parentDirid != -1)
                            sql["parent_dirid"] = parentDirid;
                        res = sql.executeInsert(conn, "fi_dir", "dirid");
                        dirid = int.Parse(res[0]["dirid"]);
                    }
                    else
                    {
                        dirid = int.Parse(res[0]["dirid"]);
                        // Update the directory's flag
                        ps = new PreparedStatement("UPDATE fi_dir SET flags = flags & ~?ne WHERE dirid=?dirid;");
                        ps["dirid"] = dirid;
                        ps["ne"] = (int)Flags.NonExistent;
                        conn.queryExecute(ps);
                    }
                }
                // Check files
                {
                    StringBuilder flagsFiles = new StringBuilder();
                    StringBuilder insertFiles = new StringBuilder();
                    string file;
                    Result res;
                    FileInfo fi;
                    SQLCompiler sql;
                    foreach (string t in System.IO.Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
                    {
                        file = System.IO.Path.GetFileName(t);
                        ps = new PreparedStatement("SELECT * FROM view_fi_file WHERE dirid=?dirid AND file=?file;");
                        ps["dirid"] = dirid;
                        ps["file"] = file;
                        fi = new FileInfo(t);
                        // Check the file exists
                        if ((res = conn.queryRead(ps)).Count == 1)
                        {
                            // Add file to have its flag updated
                            flagsFiles.Append("fileid='").Append(SQLUtils.escape(res[0]["fileid"])).Append("' OR ");
                            // Check if the size/created/modified has changed
                            sql = new SQLCompiler();
                            if (res[0].get2<int>("size") != fi.Length)
                                sql["size"] = fi.Length;
                            if (res[0].get2<DateTime>("datetime_created") != fi.CreationTime)
                                sql["datetime_created"] = fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                            if (res[0].get2<DateTime>("datetime_modified") != fi.LastWriteTime)
                                sql["datetime_modified"] = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                            sql.executeUpdate(conn, "fi_file");
                        }
                        else
                            // Create new entry
                            insertFiles.Append("('").Append(dirid).Append("', '").Append(SQLUtils.escape(file)).Append("', '").Append(SQLUtils.escape(fi.Extension.Substring(1))).Append("', '").Append(fi.Length).Append("', '").Append(SQLUtils.escape(fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"))).Append("', '").Append(SQLUtils.escape(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"))).Append("'),");
                    }
                    // Update file flags
                    if (flagsFiles.Length > 0)
                    {
                        ps = new PreparedStatement("UPDATE fi_file SET flags = flags & ~?ne WHERE " + flagsFiles.Remove(flagsFiles.Length - 4, 4) + ";");
                        ps["ne"] = (int)Flags.NonExistent;
                        conn.queryExecute(ps);
                    }
                    // Insert new files
                    if (insertFiles.Length > 0)
                        conn.queryExecute("INSERT INTO fi_file (dirid, file, extension, size, datetime_created, datetime_modified) VALUES" + insertFiles.Remove(insertFiles.Length - 1, 1).ToString() + ";");
                }
                ps = null;
                // Recurse each sub-directory
                string sd;
                foreach (string t in System.IO.Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    sd = t.Replace('\\', '/');
                    rebuildThreadWorkerRecurse(conn, baseLength, sd, dirid);
                }

            }
#if !DEBUG
            catch {}
#else
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (UnauthorizedAccessException) { }
#endif
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The base-path of where the files are stored.
        /// </summary>
        public string PathFiles
        {
            get
            { // Note to developers: if you change this, also change the value in the Directory model under PhysicalPath property.
                return Core.BasePath + "/_files";
            }
        }
    }
}