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
 *      Path:           /App_Code/CMS/Plugins/Files/models/File.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model for representing and handling files of the files plugin.
 * *********************************************************************************************************************
 */
using CMS.Base;
using System;
using UberLib.Connector;

namespace CMS.Plugins.Files
{
    /// <summary>
    /// A model for representing and handling files of the files plugin.
    /// </summary>
    public class File
    {
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            File = 1,
            Directory = 2,
            Flags = 4,
            Description = 8,
            Size = 16,
            DateTime_Created = 32,
            DateTime_Modified = 64
        };
        // Fields ******************************************************************************************************
        private bool            persisted;              // Indicates if the model has been persisted.
        private Fields          modified;               // The fields which have been modified.
        private int             fileID;                 // The identifier of this file.
        private string          file;                   // The name of this file.
        private int             dirID;                  // Directory identifier.
        private Main.Flags      flags;                  // The flags of the file.
        private string          description;            // A description of the file.
        private int             size;                   // The size of the file.
        private DateTime        dtCreated,              // The date and time of when the file was created.
                                dtModified;             // The date and time of when the file was modified.
        private Extension       extension;              // Extension data.
        // Methods - Constructors **************************************************************************************
        private File()
        {
            this.persisted = false;
            this.modified = Fields.None;
            this.flags = Main.Flags.None;
            this.file = this.description = null;
            this.size = this.fileID = this.dirID = 0;
            this.dtCreated = this.dtModified = DateTime.MinValue;
        }
        // Methods - Persistence ***************************************************************************************
        /// <summary>
        /// Loads a file from a specified relative path.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="path">Relative path of file.</param>
        /// <returns>Model or null.</returns>
        public static File load(Connector conn, string path)
        {
            // Adjust to begin with / (if it doesnt already)
            if(path.Length == 1)
                return null;
            else if (!path.StartsWith("/"))
                path = "/" + path;
            else if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);
            // Split root and name apart
            int rootindex = path.LastIndexOf('/');
            string root;
            string name;
            if (rootindex == 0)
            {
                root = "/";
                name = path.Substring(1);
            }
            else
            {
                root = path.Substring(0, rootindex);
                name = path.Substring(rootindex+1);
            }
            PreparedStatement ps = new PreparedStatement("SELECT f.* FROM view_fi_file AS f, fi_dir AS d WHERE d.dirid=f.dirid AND d.dir_path=?path_root AND file=?path_name;");
            ps["path_root"] = root;
            ps["path_name"] = name;
            Result res = conn.queryRead(ps);
            return res.Count != 1 ? null : load(res[0]);
        }
        /// <summary>
        /// Loads a model from database data.
        /// </summary>
        /// <param name="data">Database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static File load(ResultRow data)
        {
            File f = new File();
            f.persisted = true;
            f.fileID = data.get2<int>("fileid");
            f.file = data.get2<string>("file");
            f.dirID = data.get2<int>("dirid");
            f.flags = (Main.Flags)data.get2<int>("flags");
            f.description = data.get2<string>("description");
            f.size = data.get2<int>("size");
            f.dtCreated = data.get2<DateTime>("datetime_created");
            f.dtModified = data.get2<DateTime>("datetime_modified");
            f.extension = Extension.load(data);
            return f;
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
                if ((modified & Fields.File) == Fields.File)
                    sql["file"] = file;
                if ((modified & Fields.Directory) == Fields.Directory)
                    sql["dirid"] = dirID;
                if ((modified & Fields.Flags) == Fields.Flags)
                    sql["flags"] = flags;
                if ((modified & Fields.Description) == Fields.Description)
                    sql["description"] = description;
                if ((modified & Fields.Size) == Fields.Size)
                    sql["size"] = size;
                if ((modified & Fields.DateTime_Created) == Fields.DateTime_Created)
                    sql["datetime_created"] = dtCreated;
                if ((modified & Fields.DateTime_Modified) == Fields.DateTime_Modified)
                    sql["datetime_modified"] = dtModified;
                // Execute
                if (persisted)
                {
                    sql.UpdateAttribute = "fileid";
                    sql.UpdateValue = fileID;
                    sql.executeUpdate(conn, "fi_file");
                }
                else
                {
                    fileID = int.Parse(sql.executeInsert(conn, "fi_file", "fileid")[0]["fileid"]);
                    persisted = true;
                }
                modified = Fields.None;
                return true;
            }
        }
        // Methods *****************************************************************************************************
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
        /// The identifier of this file.
        /// </summary>
        public int FileID
        {
            get
            {
                return fileID;
            }
        }
        /// <summary>
        /// The name of this file.
        /// 
        /// Maximum of 256 characters; this may be less depending on the actual file-system used by the CMS.
        /// </summary>
        public string Filename
        {
            get
            {
                return file;
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
        /// The flags of the file.
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
        /// A description of the file.
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
        /// The size of the file.
        /// </summary>
        public int Size
        {
            get
            {
                return size;
            }
        }
        /// <summary>
        /// The date and time of when the file was created.
        /// </summary>
        public DateTime DateTime_Created
        {
            get
            {
                return dtCreated;
            }
        }
        /// <summary>
        /// The date and time of when the file was modified.
        /// </summary>
        public DateTime DateTime_Modified
        {
            get
            {
                return dtModified;
            }
        }
        /// <summary>
        /// Extension data.
        /// 
        /// Note: may be null.
        /// </summary>
        public Extension Extension
        {
            get
            {
                return extension;
            }
        }
    }
}