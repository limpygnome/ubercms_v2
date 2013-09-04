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
 *      Path:           /App_Code/CMS/Base/UrlRewriting.cs
 * 
 *      Change-Log:
 *                      2013-08-08      Created initial class.
 *                      2013-09-04      UUID is now optional when loading a model.
 * 
 * *********************************************************************************************************************
 * Used for handling URL rewriting.
 * 
 * Note: this model has not been used in BaseUtils for inserting multiple URL rewriting paths.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Base
{
    /// <summary>
    /// Used for handling URL rewriting.
    /// 
    /// Note: this model has not been used in BaseUtils for inserting multiple URL rewriting paths.
    /// </summary>
    public class UrlRewriting
    {
        // Enums *******************************************************************************************************
        public enum PersistStatus
        {
            Success,
            InvalidPath,
            InUse,
            Error
        };
        // Fields ******************************************************************************************************
        private bool        modified,           // Indicates if the model has been modified.
                            persisted;          // Indicates if the model has been persisted to the database.
        private int         urlid;              // The unique identifier for the model.
        private UUID        pluginOwner;        // The identifier of the plugin which owns this model.
        private string      fullPath;           // The full URL rewriting path.
        private int         priority;           // The priority of this model, over models with a similar or the same fullpath.
        // Methods - Constructors **************************************************************************************
        public UrlRewriting()
        {
            this.modified = this.persisted = false;
            this.priority = 0;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Creates and persists a model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="owner">The plugin which owns the URL rewriting model.</param>
        /// <param name="fullPath">The full URL path of the model.</param>
        /// <param name="persistStatus">The status of persisting the model.</param>
        /// <returns>Model or null.</returns>
        public static UrlRewriting create(Connector conn, Plugin owner, string fullPath, out PersistStatus persistStatus)
        {
            UrlRewriting rw = new UrlRewriting();
            rw.PluginOwner = owner.UUID;
            rw.FullPath = fullPath;
            persistStatus = rw.save(conn);
            return persistStatus == PersistStatus.Success ? rw : null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="fullPath">The full-path of the model.</param>
        /// <returns>Model or null.</returns>
        public static UrlRewriting load(Connector conn, string fullPath)
        {
            fullPath = stripFullPath(fullPath);
            if (fullPath == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM cms_view_urlrewriting WHERE full_path=?full_path;");
            ps["full_path"] = fullPath;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="urlID">Identifier of URL rewriting entry.</param>
        /// <returns>Model or null.</returns>
        public static UrlRewriting load(Connector conn, int urlID)
        {
            Result r = conn.queryRead("SELECT * FROM cms_view_urlrewriting WHERE urlid='" + SQLUtils.escape(urlID.ToString()) + "';");
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="row">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static UrlRewriting load(ResultRow row)
        {
            UrlRewriting rw = new UrlRewriting();
            rw.urlid = row.get2<int>("urlid");
            rw.pluginOwner = row.contains("uuid") ? UUID.parse(row["uuid"]) : null;
            rw.fullPath = row.get2<string>("full_path");
            rw.priority = row.get2<int>("priority");
            return rw;
        }
        /// <summary>
        /// Unpersists the model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM cms_urlrewriting WHERE urlid=?urlid;");
            ps["urlid"] = urlid;
            conn.queryExecute(ps);
            persisted = false;
        }
        /// <summary>
        /// Persists the model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = not persisted (likely the full-path is already in-use).</returns>
        public PersistStatus save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return PersistStatus.Error;
                // Validate full-path
                if (fullPath == null || fullPath.Length == 0)
                    return PersistStatus.InvalidPath;
                // Split into groups and validate each one
                string[] g = fullPath.Split('/');
                foreach (string s in g)
                    if (g.Length == 0 || !Regex.IsMatch(s, @"^([a-zA-Z0-9_.\-]*)$"))
                        return PersistStatus.InvalidPath;
                // Compile and execute insert/update
                SQLCompiler sql = new SQLCompiler();
                sql["uuid"] = pluginOwner.Bytes;
                sql["full_path"] = fullPath;
                sql["priority"] = priority;
                try
                {
                    if (persisted)
                    {
                        sql.UpdateAttribute = "urlid";
                        sql.UpdateValue = urlid;
                        sql.executeUpdate(conn, "cms_urlrewriting");
                    }
                    else
                        urlid = int.Parse(sql.executeInsert(conn, "cms_urlrewriting", "urlid")[0]["urlid"]);
                }
                catch (DuplicateEntryException)
                {
                    return PersistStatus.InUse;
                }
                return PersistStatus.Success;
            }
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Strips a full-path of tailing forward slash's and any whitespace.
        /// </summary>
        /// <param name="fullPath">The full-path to be treated.</param>
        /// <returns>Either the full-path cleaned-up or null if invalid.</returns>
        public static string stripFullPath(string fullPath)
        {
            fullPath = fullPath.Trim();
            if (fullPath.StartsWith("/"))
            {
                if (fullPath.Length == 1)
                    return null;
                else
                    fullPath = fullPath.Substring(1);
            }
            if (fullPath.EndsWith("/"))
            {
                if (fullPath.Length == 1)
                    return null;
                else
                    fullPath = fullPath.Substring(0, fullPath.Length - 1);
            }
            return fullPath;
        }
        /// <summary>
        /// Finds the available plugins to serve a request. Returns an empty array if a consistency issue has occurred
        /// (a plugin cannot be found).
        /// </summary>
        /// <returns>The request handlers.</returns>
        /// <param name="pathInfo">Path info.</param>
        /// <param name="conn">Database connector.</param>
        public static Plugin[] findRequestHandlers(PathInfo pathInfo, Connector conn)
        {
            Result r = conn.queryRead("SELECT uuid FROM cms_view_request_handlers WHERE (full_path='" + SQLUtils.escape(pathInfo.FullPath) + "' OR full_path ='" + SQLUtils.escape(pathInfo.ModuleHandler) + "');");
            List<Plugin> result = new List<Plugin>(r.Count);
            Plugin p;
            foreach (ResultRow plugin in r)
            {
                p = Core.Plugins[plugin["uuid"]];
                if (p != null)
                    result.Add(p);
            }
            return result.ToArray();
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of this model.
        /// </summary>
        public int UrlID
        {
            get
            {
                return urlid;
            }
            private set
            {
                urlid = value;
            }
        }
        /// <summary>
        /// The owner of this model and entry-point for when this model is used for url rewriting.
        /// </summary>
        public UUID PluginOwner
        {
            get
            {
                return pluginOwner;
            }
            set
            {
                pluginOwner = value;
                modified = true;
            }
        }
        /// <summary>
        /// The full path used to be handled; this can also be the module-handler/first directory. If this path is
        /// the module-handler/first-directory of the URL and has a higher priority, it will handle a request over
        /// other URL rewriting models.
        /// 
        /// Note: this will never start or end with a forward-slash; this will be stripped, if set, when persisting.
        /// </summary>
        public string FullPath
        {
            get
            {
                return fullPath;
            }
            set
            {
                fullPath = stripFullPath(value);
                modified = true;
            }
        }
        /// <summary>
        /// The priority of the URL rewriting model over other models; the higher the value, the greater the priority/
        /// the more likely this model will come first before other models.
        /// </summary>
        public int Priority
        {
            get
            {
                return priority;
            }
            set
            {
                priority = value;
                modified = true;
            }
        }
    }
}