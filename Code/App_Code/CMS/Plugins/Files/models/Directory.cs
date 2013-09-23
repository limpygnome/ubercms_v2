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
 *      Path:           /App_Code/CMS/Plugins/Files/models/Directory.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model for representing and handling directories of the files plugin.
 * *********************************************************************************************************************
 */
using CMS.Base;
using System;
using System.Collections.Generic;
using UberLib.Connector;

namespace CMS.Plugins.Files
{
    /// <summary>
    /// A model for representing and handling directories of the files plugin.
    /// </summary>
    public class Directory
    {
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            Directory = 1,
            Flags = 2,
            Description = 4
        };
        // Fields ******************************************************************************************************
        private bool            persisted;      // Indicates if the model has been persisted.
        private Fields          modified;       // The fields which have been modified.
        private int             dirID;          // Directory identifier.
        private string          directoryName;  // The name of the directory.
        private string          directoryPath;  // The relative path of the directory.
        private Main.Flags      flags;          // The flags of the directory.
        private string          description;    // Description.
        // Methods - Constructors **************************************************************************************
        private Directory()
        {
            this.persisted = false;
            this.modified = Fields.None;
            this.dirID = -1;
            this.directoryName = this.directoryPath = null;
            this.flags = Main.Flags.None;
        }
        // Methods - Persistence ***************************************************************************************
        /// <summary>
        /// Fetches the sub-directories.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>Array of directories, possibly empty.</returns>
        public Directory[] getSubDirs(Connector conn)
        {
            List<Directory> buffer = new List<Directory>();
            PreparedStatement ps = new PreparedStatement("SELECT * FROM view_fi_dir WHERE parent_dirid=?parent;");
            ps["parent"] = dirID;
            Result dirs = conn.queryRead(ps);
            Directory d;
            foreach (ResultRow t in dirs)
            {
                if ((d = Directory.load(t)) != null)
                    buffer.Add(d);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// Fetches the files within this directory.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>Array of files, possibly empty.</returns>
        public File[] getFiles(Connector conn)
        {
            List<File> buffer = new List<File>();
            PreparedStatement ps = new PreparedStatement("SELECT * FROM view_fi_file WHERE dirid=?dir;");
            ps["dir"] = dirID;
            Result files = conn.queryRead(ps);
            File f;
            foreach (ResultRow t in files)
            {
                if ((f = File.load(t)) != null)
                    buffer.Add(f);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// Loads a model from the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="relativePath">Relative path of directory.</param>
        /// <returns>Model or null.</returns>
        public static Directory load(Connector conn, string relativePath)
        {
            // Get the relative path to the correct format
            if (relativePath == null || relativePath.Length == 0)
                return null;
            else if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;
            else if (relativePath.Length > 1 && relativePath.EndsWith("/"))
                relativePath = relativePath.Substring(0, relativePath.Length - 1);
            // Search and load
            PreparedStatement ps = new PreparedStatement("SELECT * FROM view_fi_dir WHERE dir_path=?path;");
            ps["path"] = relativePath;
            Result res = conn.queryRead(ps);
            return res.Count == 1 ? load(res[0]) : null;
        }
        /// <summary>
        /// Loads a model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="dirid">Identifier of the directory.</param>
        /// <returns>Model or null.</returns>
        public static Directory load(Connector conn, int dirid)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM view_fi_dir WHERE dirid=?dirid;");
            ps["dirid"] = dirid;
            Result res = conn.queryRead(ps);
            return res.Count == 1 ? load(res[0]) : null;
        }
        /// <summary>
        /// Loads a model from database data.
        /// </summary>
        /// <param name="data">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static Directory load(ResultRow data)
        {
            Directory dir = new Directory();
            dir.persisted = true;
            dir.dirID = data.get2<int>("dirid");
            dir.directoryName = data.get2<string>("dir_name");
            dir.directoryPath = data.get2<string>("dir_path");
            dir.flags = (Main.Flags)data.get2<int>("flags");
            dir.description = data.get2<string>("description");
            return dir;
        }
        /// <summary>
        /// Persists the model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = no changes.</returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (modified == Fields.None)
                    return false;
                // Compile SQL
                SQLCompiler sql = new SQLCompiler();
                if ((modified & Fields.Directory) == Fields.Directory)
                {
                    sql["dir_name"] = directoryName;
                    sql["dir_path"] = directoryPath;
                }
                if ((modified & Fields.Flags) == Fields.Flags)
                    sql["flags"] = flags;
                if ((modified & Fields.Description) == Fields.Description)
                    sql["description"] = description;
                // Execute
                if (persisted)
                {
                    sql.UpdateAttribute = "dirid";
                    sql.UpdateValue = dirID;
                    sql.executeUpdate(conn, "fi_dir");
                }
                else
                {
                    dirID = int.Parse(sql.executeInsert(conn, "fi_dir", "dirid")[0]["dirid"]);
                    persisted = true;
                }
                modified = Fields.None;
                return true;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Indicates if the model has been modified.
        /// </summary>
        public bool IsModified
        {
            get
            {
                return modified == Fields.None;
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
        /// Directory identifier.
        /// </summary>
        public int DirectoryID
        {
            get
            {
                return dirID;
            }
        }
        /// <summary>
        /// The name of the directory.
        /// 
        /// Note: limited to 256 characters.
        /// </summary>
        public string DirectoryName
        {
            get
            {
                return directoryName;
            }
            set
            {
                lock (this)
                {
                    directoryName = value;
                    modified |= Fields.Directory;
                }
            }
        }
        /// <summary>
        /// The relative path of the directory, including the directory-name.
        /// 
        /// Note: the top-level directory will be '/' (without quotations).
        /// Note 2: limited to 512 characters by default.
        /// </summary>
        public string DirectoryPath
        {
            get
            {
                return directoryPath;
            }
            set
            {
                lock (this)
                {
                    directoryPath = value;
                    modified |= Fields.Directory;
                }
            }
        }
        /// <summary>
        /// The flags of the directory.
        /// </summary>
        public Main.Flags Flags
        {
            get
            {
                return flags;
            }
            set
            {
                lock (this)
                {
                    flags = value;
                    modified |= Fields.Flags;
                }
            }
        }
        /// <summary>
        /// A description of the directory.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                lock (this)
                {
                    description = value;
                    modified |= Fields.Description;
                }
            }
        }
        /// <summary>
        /// The physical path to the directory.
        /// </summary>
        public string PhysicalPath
        {
            get
            {
                return Core.BasePath + "/_files" + directoryPath;
            }
        }
    }
}