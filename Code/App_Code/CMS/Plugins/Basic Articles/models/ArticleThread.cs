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
 *      Path:           /App_Code/CMS/Plugins/Basic Articles/models/ArticleThread.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model to represent a thread containing multiple articles/revisions.
 * *********************************************************************************************************************
 */
using System;
using System.Drawing;
using System.IO;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicArticles
{
    /// <summary>
    /// A model to represent a thread containing multiple articles/revisions.
    /// </summary>
    public class ArticleThread
    {
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            Url = 1,
            Thumbnail = 2,
            ArticleCurrent = 4,
            Description = 8
        };
        public enum CreateThread
        {
            Error,
            UrlInvalid,
            UrlUsed,
            Success
        };
        public enum UpdateThumbnail
        {
            InvalidSize,
            InvalidData,
            Error,
            Success
        }
        // Fields ******************************************************************************************************
        private Fields          modified;                   // Indicates if the model has been modified.
        private bool            persisted;                  // Indicates if the model has been persisted.
        private UUID            uuidThread;                 // The UUID/identifier of this article thread.
        private UrlRewriting    url;                        // The URL rewriting model.
        private UUID            uuidArticleCurrent;         // The UUID of the article currently displayed for the thread.
        private bool            thumbnail;                  // Indicates if the article has a thumbnail uploaded.
        private string          description;                // A description of the thread.
        // Methods - Constructors **************************************************************************************
        public ArticleThread()
        {
            this.modified = Fields.None;
            this.persisted = false;
            this.uuidThread = null;
            this.url = null;
            this.uuidArticleCurrent = null;
            this.thumbnail = false;
            this.description = null;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Either creates or fetches an article thread based on a full-path.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="ba">Basic Articles.</param>
        /// <param name="fullPath">The full path of the desired thread.</param>
        /// <param name="at">The article thread model output parameter; set to null if unsuccessful.</param>
        /// <returns>True = successfully fetched article thread model, false = failed to fetch article thread model.</returns>
        public static CreateThread createFetch(Connector conn, BasicArticles ba, string fullPath, out ArticleThread at)
        {
            bool newThread = true;
            ArticleThread temp;
            if (fullPath != null)
            {
                // Ensure the URL is formatted correctly
                fullPath = UrlRewriting.stripFullPath(fullPath);
                // Lookup for an existing article at the URL
                PreparedStatement ps = new PreparedStatement("SELECT uuid_thread FROM ba_article_thread_createfetch WHERE full_path=?full_path;");
                ps["full_path"] = fullPath;
                Result r = conn.queryRead(ps);
                // Load, else create, the model for the article
                if (r.Count == 1)
                {
                    if ((temp = ArticleThread.load(conn, UUID.parse(r[0].get2<string>("uuid_thread")))) == null)
                    {
                        at = null;
                        return CreateThread.Error;
                    }
                    newThread = true;
                }
                else if (r.Count > 1)
                    throw new Exception("Multiple article threads exist for the full-path - critical exception!");
                else
                {
                    // A thread does not exist at the specified full-path - create it!
                    // -- Create URL
                    UrlRewriting rw = new UrlRewriting();
                    rw.FullPath = fullPath;
                    rw.PluginOwner = ba.UUID;
                    UrlRewriting.PersistStatus s = rw.save(conn);
                    if (s != UrlRewriting.PersistStatus.Success)
                    {
                        at = null;
                        switch (s)
                        {
                            case UrlRewriting.PersistStatus.Error:
                                return CreateThread.Error;
                            case UrlRewriting.PersistStatus.InvalidPath:
                                return CreateThread.UrlInvalid;
                            case UrlRewriting.PersistStatus.InUse:
                                return CreateThread.UrlUsed;
                        }
                    }
                    // -- Create new model
                    temp = new ArticleThread();
                    temp.Url = rw;
                }
            }
            else
                temp = new ArticleThread();
            // Persist the thread
            if (!temp.save(conn))
            {
                at = null;
                return CreateThread.Error;
            }
            // Check if this is a new thread
            if (newThread)
            {
                // -- Add the anonymous group by default to thread permissions for viewing
                ArticleThreadPermissions perms = new ArticleThreadPermissions(temp.UUIDThread);
                perms.add(ArticleThreadPermissions.getAnonymousGroup());
                if (!perms.save(conn))
                {
                    // Rollback...
                    temp.remove(conn);
                    at = null;
                    return CreateThread.Error;
                }
            }
            // Success
            at = temp;
            return CreateThread.Success;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">Identifier of thread.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(Connector conn, UUID uuidThread)
        {
            if (uuidThread == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_thread WHERE uuid_thread_raw=?uuid_thread_raw;");
            ps["uuid_thread_raw"] = uuidThread.Bytes;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads the article at the specified url.
        /// </summary>
        /// <param name="url">The URL of the article.</param>
        /// <param name="conn">Database connector.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(Connector conn, string url)
        {
            if (url == null || url.Length == 0)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM ba_view_load_article_thread WHERE full_path=?full_path;");
            ps["full_path"] = UrlRewriting.stripFullPath(url);
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a model from a database tuple/row.
        /// </summary>
        /// <param name="data">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static ArticleThread load(ResultRow data)
        {
            ArticleThread th = new ArticleThread();
            th.persisted = true;
            th.uuidThread = UUID.parse(data["uuid_thread"]);
            th.url = UrlRewriting.load(data);
            th.uuidArticleCurrent = UUID.parse(data["uuid_article_current"]);
            th.thumbnail = data["thumbnail"].Equals("1");
            th.description = data.isNull("description") || data["description"].Length == 0 ? null : data["description"];
            return th;
        }
        /// <summary>
        /// Persists the model to the database.
        /// 
        /// Note: if a UUID for the thread has not been specified and the model has not been persisted, a version 4 UUID
        /// will be generated.
        /// </summary>
        /// <param name="conn">Database connecotor.</param>
        /// <returns>True = persisted, false = no change.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (modified == Fields.None)
                    return false;
                // Compile SQL
                SQLCompiler sql = new SQLCompiler();
                if ((modified & Fields.Url) == Fields.Url)
                {
                    if (url == null)
                        sql["urlid"] = null;
                    else
                        sql["urlid"] = url.UrlID;
                }
                if((modified & Fields.ArticleCurrent) == Fields.ArticleCurrent)
                    sql["uuid_article_current"] = uuidArticleCurrent != null ? uuidArticleCurrent.Bytes : null;
                if((modified & Fields.Thumbnail) == Fields.Thumbnail)
                    sql["thumbnail"] = thumbnail ? "1" : "0";
                if ((modified & Fields.Description) == Fields.Description)
                    sql["description"] = description;
                // Execute SQL
                try
                {
                    if (persisted)
                    {
                        sql.UpdateAttribute = "uuid_thread";
                        sql.UpdateValue = uuidThread.Bytes;
                        sql.executeUpdate(conn, "ba_article_thread");
                    }
                    else
                    {
                        if(uuidThread == null)
                            uuidThread = UUID.generateVersion4();
                        sql["uuid_thread"] = uuidThread.Bytes;
                        sql.executeInsert(conn, "ba_article_thread");
                        persisted = true;
                    }
                    modified = Fields.None;
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// Unpersists the model from the database, but only if the article has no articles.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            lock (this)
            {
                conn.queryExecute("BEGIN;");
                bool delete = conn.queryCount("SELECT COUNT('') FROM ba_article WHERE uuid_thread=" + uuidThread.NumericHexString) == 0;
                if (delete)
                {
                    try
                    {
                        // Delete the article
                        conn.queryExecute("DELETE FROM ba_article_thread WHERE uuid_thread=" + uuidThread.NumericHexString);
                        // Delete the URL
                        if (url != null)
                            url.remove(conn);
                        persisted = false;
                    }
                    catch { }
                }
                conn.queryExecute("COMMIT;");
            }
        }
        /// <summary>
        /// Unpersists the model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void removeForce(Connector conn)
        {
            lock (this)
            {
                PreparedStatement ps = new PreparedStatement("DELETE FROM ba_article_thread WHERE uuid_thread=?uuid_thread;");
                ps["uuid_thread"] = uuidThread.Bytes;
                try
                {
                    conn.queryExecute(ps);
                    url.remove(conn);
                }
                catch { }
                persisted = false;
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Updates the thumbnail for the thread.
        /// </summary>
        /// <param name="stream">Image data.</param>
        /// <param name="length">The size/length of the image data.</param>
        /// <param name="extension">The extension of the image.</param>
        /// <returns>Status of updating the thumbnail.</returns>
        public UpdateThumbnail thumbnailUpdate(Stream stream, int length, string extension, BaseUtils.ResizeAction action)
        {
            lock (this)
            {
                // Check the file-size
                if (length < Core.Settings[Settings.SETTINGS__THREAD_IMAGE_LENGTH_MIN].get<int>() || length > Core.Settings[Settings.SETTINGS__THREAD_IMAGE_LENGTH_MAX].get<int>())
                    return UpdateThumbnail.InvalidSize;
                // Check extension
                if (extension.StartsWith("."))
                {
                    if (extension.Length == 1)
                        return UpdateThumbnail.InvalidData;
                    else
                        extension = extension.Substring(1);
                }
                if (!Core.Settings[Settings.SETTINGS__THREAD_IMAGE_ALLOWED_EXTENSIONS].get<string>().Contains(extension))
                    return UpdateThumbnail.InvalidData;
                // Convert to byte array
                byte[] data = new byte[length];
                try
                {
                    stream.Read(data, 0, length);
                    stream.Close();
                }
                catch
                {
                    return UpdateThumbnail.InvalidData;
                }
                return thumbnailUpdate(data, action);
            }
        }
        /// <summary>
        /// Updates the thumbnail for the thread.
        /// </summary>
        /// <param name="data">Image data.</param>
        /// <returns>Status of updating the thumbnail.</returns>
        public UpdateThumbnail thumbnailUpdate(byte[] data, BaseUtils.ResizeAction action)
        {
            lock (this)
            {
                // Parse the byte data into an image
                try
                {
                    MemoryStream ms = new MemoryStream(data);
                    UpdateThumbnail ut = thumbnailUpdate(Image.FromStream(ms), action);
                    ms.Close();
                    ms.Dispose();
                    return ut;
                }
                catch
                {
                    return UpdateThumbnail.InvalidData;
                }
            }
        }
        /// <summary>
        /// Updates the thumbnail for the thread.
        /// </summary>
        /// <param name="img">Image data.</param>
        /// <returns>Status of updating the thumbnail.</returns>
        public UpdateThumbnail thumbnailUpdate(Image img, BaseUtils.ResizeAction action)
        {
            lock (this)
            {
                // Resize image
                img = BaseUtils.resizeImage(img, action, Core.Settings[Settings.SETTINGS__THREAD_IMAGE_WIDTH].get<int>(), Core.Settings[Settings.SETTINGS__THREAD_IMAGE_HEIGHT].get<int>());
                // Save the image to disk
                try
                {
                    if (!Directory.Exists(PathThumbnails))
                        Directory.CreateDirectory(PathThumbnails);
                    img.Save(PathThumbnail, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch
                {
                    return UpdateThumbnail.Error;
                }
                // Update modification flags
                if (!thumbnail)
                {
                    thumbnail = true;
                    modified |= Fields.Thumbnail;
                }
                return UpdateThumbnail.Success;
            }
        }
        /// <summary>
        /// Resets the thumbnail.
        /// </summary>
        public void thumbnailReset()
        {
            lock (this)
            {
                if (thumbnail)
                {
                    File.Delete(PathThumbnail);
                    thumbnail = false;
                    modified |= Fields.Thumbnail;
                }
            }
        }
        /// <summary>
        /// Moves the thread to a new URL.
        /// 
        /// Note: you will need to persist the thread model to save changes!
        /// </summary>
        /// <param name="ba">Basic Articles plugin model.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="urlNew">The new URL/destination of the thread.</param>
        /// <returns>The status of the operation.</returns>
        public UrlRewriting.PersistStatus move(BasicArticles ba, Connector conn, string urlNew)
        {
            lock (this)
            {
                // Save the old for unpersistence later
                UrlRewriting urOld = url;
                // Create new URL
                UrlRewriting urNew = new UrlRewriting();
                urNew.PluginOwner = ba.UUID;
                urNew.FullPath = urlNew;
                UrlRewriting.PersistStatus ps = urNew.save(conn);
                if (ps == UrlRewriting.PersistStatus.Success)
                {
                    urOld.remove(conn);
                    url = urNew;
                    modified |= Fields.Url;
                }
                return ps;
            }
        }
        /// <summary>
        /// Updates the description of the thread.
        /// </summary>
        /// <param name="value">The new description.</param>
        /// <returns>True = updated, false = invalid length.</returns>
        public bool descriptionUpdate(string value)
        {
            lock (this)
            {
                if (value.Length < Core.Settings[Settings.SETTINGS__THREAD_DESCRIPTION_LENGTH_MIN].get<int>() || value.Length > Core.Settings[Settings.SETTINGS__THREAD_DESCRIPTION_LENGTH_MAX].get<int>())
                    return false;
                description = value == null || value.Length == 0 ? null : value;
                modified |= Fields.Description;
                return true;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The UUID of this article thread.
        /// 
        /// This can only be set interally if the model has yet to be persisted!
        /// </summary>
        public UUID UUIDThread
        {
            get
            {
                return uuidThread;
            }
            internal set
            {
                lock (this)
                {
                    if (!persisted)
                        uuidThread = value;
                }
            }
        }
        /// <summary>
        /// The URL rewriting model associated with this thread; can be null.
        /// </summary>
        public UrlRewriting Url
        {
            get
            {
                return url;
            }
            set
            {
                lock (this)
                {
                    url = value;
                    modified |= Fields.Url;
                }
            }
        }
        /// <summary>
        /// The UUID of the current article; can be null.
        /// </summary>
        public UUID UUIDArticleCurrent
        {
            get
            {
                return uuidArticleCurrent;
            }
            set
            {
                lock (this)
                {
                    uuidArticleCurrent = value;
                    modified |= Fields.ArticleCurrent;
                }
            }
        }
        /// <summary>
        /// Indicates if the thread has a thumbnail.
        /// </summary>
        public bool Thumbnail
        {
            get
            {
                return thumbnail;
            }
            set
            {
                lock (this)
                {
                    thumbnail = value;
                    modified |= Fields.Thumbnail;
                }
            }
        }
        /// <summary>
        /// The description of the thread.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
        }
        /// <summary>
        /// The URL of the thumbnail for this article thread; if no thumbnail is available, the URL will point to a
        /// default thumbnail.
        /// </summary>
        public string UrlThumbnail
        {
            get
            {
                return thumbnail ? ("/content/basic_articles/thumbnails/" + uuidThread.Hex + ".png") : "/content/images/basic_articles/default.png";
            }
        }
        /// <summary>
        /// The physical path for the thumbnail of this article.
        /// </summary>
        public string PathThumbnail
        {
            get
            {
                return PathThumbnails + "/" + uuidThread.Hex + ".png";
            }
        }
        /// <summary>
        /// The physical path of thumbnails
        /// </summary>
        public static string PathThumbnails
        {
            get
            {
                return Core.PathContent + "/basic_articles/thumbnails";
            }
        }
        /// <summary>
        /// Indicates if the model has been persisted.
        /// </summary>
        public bool IsPersisted
        {
            get
            {
                return persisted;
            }
        }
        /// <summary>
        /// Indicates if the model has been modified.
        /// </summary>
        public bool IsModified
        {
            get
            {
                return modified != Fields.None;
            }
        }
    }
}