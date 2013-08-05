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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/UserGroup.cs
 * 
 *      Change-Log:
 *                      2013-07-06      Created initial class.
 *                      2013-07-07      Added login field/property.
 * 
 * *********************************************************************************************************************
 * A model to represent a user-group of the basic site authentication plugin.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using CMS.BasicSiteAuth;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth.Models
{
    /// <summary>
    /// A model to represent a user-group of the basic site authentication plugin.
    /// </summary>
    public class UserGroup
    {
        // Fields
        int     groupID;            // The identifier of the user-group
        string  title,              // The title of the user-group.
                description;        // A description of the user-group.
        bool    pagesCreate,        // Create pages.
                pagesModify,        // Modify/update pages.
                pagesModifyOwn,     // Modify/update pages created by the own user.
                pagesDelete,        // Delete pages.
                pagesDeleteOwn,     // Delete pages created by the own user.
                pagesPublish,       // Publish pages (become public).

                commentsCreate,     // Create comments.
                commentsModifyOwn,  // Modify own comments.
                commentsDelete,     // Delete any comments.
                commentsDeleteOwn,  // Delete own comments.
                commentsPublish,    // Publish own comments.

                mediaCreate,        // Create media.
                mediaModify,        // Modify media.
                mediaModifyOwn,     // Modify media created by the own user.
                mediaDelete,        // Delete media (any).
                mediaDeleteOwn,     // Delete media created by the own user.

                moderator,          // Indicates moderator permissions.
                administrator,      // Indicates administrator/root permissions.
                login;              // Indicates if the user is allowed to login.

        bool    persisted,          // Indicates if this model has been persisted to the database.
                modified;           // Indicates if anything has been modified.
        // Methods - Constructors
        public UserGroup()
        {
            persisted = modified = false;
        }
        // Methods - Database
        /// <summary>
        /// Loads a user-group from the database based on its identifier.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="groupID">The group's identifier.</param>
        /// <returns>The group object or null if it cannot be found/invalid.</returns>
        public static UserGroup load(Connector conn, int groupID)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_user_groups WHERE groupid=?groupid;");
            ps["groupid"] = groupID;
            Result r = conn.queryRead(ps);
            if (r.Count == 1)
                return load(r[0]);
            else
                return null;
        }
        /// <summary>
        /// Loads a user-group from database data.
        /// </summary>
        /// <param name="data">Database data.</param>
        /// <returns>The group object or null if it cannot be found/invalid.</returns>
        public static UserGroup load(ResultRow data)
        {
            UserGroup g = new UserGroup();
            g.persisted = true;
            g.groupID = data.get2<int>("groupid");
            g.title = data.get2<string>("title");
            g.description = data.isNull("description") ? string.Empty : data.get2<string>("description");

            g.pagesCreate = data.get2<string>("pages_create").Equals("1");
            g.pagesModify = data.get2<string>("pages_modify").Equals("1");
            g.pagesModifyOwn = data.get2<string>("pages_modify_own").Equals("1");
            g.pagesDelete = data.get2<string>("pages_delete").Equals("1");
            g.pagesDeleteOwn = data.get2<string>("pages_delete_own").Equals("1");
            g.pagesPublish = data.get2<string>("pages_publish").Equals("1");

            g.commentsCreate = data.get2<string>("comments_create").Equals("1");
            g.commentsModifyOwn = data.get2<string>("comments_modify_own").Equals("1");
            g.commentsDelete = data.get2<string>("comments_delete").Equals("1");
            g.commentsDeleteOwn = data.get2<string>("comments_delete_own").Equals("1");
            g.commentsPublish = data.get2<string>("comments_publish").Equals("1");

            g.mediaCreate = data.get2<string>("media_create").Equals("1");
            g.mediaModify = data.get2<string>("media_modify").Equals("1");
            g.mediaModifyOwn = data.get2<string>("media_modify_own").Equals("1");
            g.mediaDelete = data.get2<string>("media_delete").Equals("1");
            g.mediaDeleteOwn = data.get2<string>("media_delete_own").Equals("1");

            g.moderator = data.get2<string>("moderator").Equals("1");
            g.administrator = data.get2<string>("administrator").Equals("1");
            g.login = data.get2<string>("login").Equals("1");
            return g;
        }
        /// <summary>
        /// Persists the data to the database.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        public void save(BasicSiteAuth bsa, Connector conn)
        {
            save(bsa, conn, false);
        }
        /// <summary>
        /// Persists the data to the database. If the user-group is new, it will be automatically added to the
        /// user-groups model in the BSA plugin.
        /// </summary>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="conn">Database connector.</param>
        /// <param name="forceInsert">Indicates if the data should be inserted, regardless of the data already existing.</param>
        public void save(BasicSiteAuth bsa, Connector conn, bool forceInsert)
        {
            lock(this)
            {
                SQLCompiler sql = new SQLCompiler();
                sql["title"] = title;
                sql["description"] = description.Length == 0 ? null : description;

                sql["pages_create"] = pagesCreate ? "1" : "0";
                sql["pages_modify"] = pagesModify ? "1" : "0";
                sql["pages_modify_own"] = pagesModifyOwn ? "1" : "0";
                sql["pages_delete"] = pagesDelete ? "1" : "0";
                sql["pages_delete_own"] = pagesDeleteOwn ? "1" : "0";
                sql["pages_publish"] = pagesPublish ? "1" : "0";

                sql["comments_create"] = commentsCreate ? "1" : "0";
                sql["comments_modify_own"] = commentsModifyOwn ? "1" : "0";
                sql["comments_delete"] = commentsDelete ? "1" : "0";
                sql["comments_delete_own"] = commentsDeleteOwn ? "1" : "0";
                sql["comments_publish"] = commentsPublish ? "1" : "0";

                sql["media_create"] = mediaCreate ? "1" : "0";
                sql["media_modify"] = mediaModify ? "1" : "0";
                sql["media_modify_own"] = mediaModifyOwn ? "1" : "0";
                sql["media_delete"] = mediaDelete ? "1" : "0";
                sql["media_delete_own"] = mediaDeleteOwn ? "1" : "0";

                sql["moderator"] = moderator ? "1" : "0";
                sql["administrator"] = administrator ? "1" : "0";
                sql["login"] = login ? "1" : "0";

                if (!forceInsert && persisted)
                {
                    sql.UpdateAttribute = "groupid";
                    sql.UpdateValue = groupID;
                    sql.executeUpdate(conn, "bsa_user_groups");
                }
                else
                {
                    groupID = (int)sql.executeInsert(conn, "bsa_user_groups", "groupid")[0].get2<long>("groupid");
                    persisted = true;
                }
                modified = false;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// Group identifier.
        /// </summary>
        public int GroupID
        {
            get
            {
                return groupID;
            }
        }
        /// <summary>
        /// The title of the group.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                modified = true;
                title = value;
            }
        }
        /// <summary>
        /// The description of the group.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                modified = true;
                description = value;
            }
        }
        // Methods - Properties - Pages
        /// <summary>
        /// Permission to create pages.
        /// </summary>
        public bool Pages_Create
        {
            get
            {
                return pagesCreate;
            }
            set
            {
                modified = true;
                pagesCreate = value;
            }
        }
        /// <summary>
        /// Permission to modify any page.
        /// </summary>
        public bool Pages_Modify
        {
            get
            {
                return pagesModify;
            }
            set
            {
                modified = true;
                pagesModify = value;
            }
        }
        /// <summary>
        /// Permission to modify pages created by the user.
        /// </summary>
        public bool Pages_ModifyOwn
        {
            get
            {
                return pagesModifyOwn;
            }
            set
            {
                modified = true;
                pagesModifyOwn = value;
            }
        }
        /// <summary>
        /// Permission to delete any page.
        /// </summary>
        public bool Pages_Delete
        {
            get
            {
                return pagesDelete;
            }
            set
            {
                modified = true;
                pagesDelete = value;
            }
        }
        /// <summary>
        /// Permission to delete a page created by the user.
        /// </summary>
        public bool Pages_DeleteOwn
        {
            get
            {
                return pagesDeleteOwn;
            }
            set
            {
                modified = true;
                pagesDeleteOwn = value;
            }
        }
        /// <summary>
        /// Permission to publish any page.
        /// </summary>
        public bool Pages_Publish
        {
            get
            {
                return pagesPublish;
            }
            set
            {
                modified = true;
                pagesPublish = value;
            }
        }
        // Methods - Properties - Comments
        /// <summary>
        /// Permission to create comments.
        /// </summary>
        public bool Comments_Create
        {
            get
            {
                return commentsCreate;
            }
            set
            {
                modified = true;
                commentsCreate = value;
            }
        }
        /// <summary>
        /// Permission to modify comments created by the user.
        /// </summary>
        public bool Comments_ModifyOwn
        {
            get
            {
                return commentsModifyOwn;
            }
            set
            {
                modified = true;
                commentsModifyOwn = value;
            }
        }
        /// <summary>
        /// Permission to delete any comment.
        /// </summary>
        public bool Comments_Delete
        {
            get
            {
                return commentsDelete;
            }
            set
            {
                modified = true;
                commentsDelete = value;
            }
        }
        /// <summary>
        /// Permission to delete a comment created by the user.
        /// </summary>
        public bool Comments_DeleteOwn
        {
            get
            {
                return commentsDeleteOwn;
            }
            set
            {
                modified = true;
                commentsDeleteOwn = value;
            }
        }
        /// <summary>
        /// Permission to publish any comment.
        /// </summary>
        public bool Comments_Publish
        {
            get
            {
                return commentsPublish;
            }
            set
            {
                modified = true;
                commentsPublish = value;
            }
        }
        // Methods - Properties - Media
        /// <summary>
        /// Permission to create media.
        /// </summary>
        public bool Media_Create
        {
            get
            {
                return mediaCreate;
            }
            set
            {
                modified = true;
                mediaCreate = value;
            }
        }
        /// <summary>
        /// Permission to modify media.
        /// </summary>
        public bool Media_Modify
        {
            get
            {
                return mediaModify;
            }
            set
            {
                modified = true;
                mediaModify = value;
            }
        }
        /// <summary>
        /// Permission to modify media created by the user.
        /// </summary>
        public bool Media_ModifyOwn
        {
            get
            {
                return mediaModifyOwn;
            }
            set
            {
                modified = true;
                mediaModifyOwn = value;
            }
        }
        /// <summary>
        /// Permission to delete any media.
        /// </summary>
        public bool Media_Delete
        {
            get
            {
                return mediaDelete;
            }
            set
            {
                modified = true;
                mediaDelete = value;
            }
        }
        /// <summary>
        /// Permission to delete media created by the user.
        /// </summary>
        public bool Media_DeleteOwn
        {
            get
            {
                return mediaDeleteOwn;
            }
            set
            {
                modified = true;
                mediaDeleteOwn = value;
            }
        }
        /// <summary>
        /// Indicates if the user is a moderator.
        /// </summary>
        public bool Moderator
        {
            get
            {
                return moderator;
            }
            set
            {
                modified = true;
                moderator = value;
            }
        }
        /// <summary>
        /// Indicates if the user is an administrator.
        /// </summary>
        public bool Administrator
        {
            get
            {
                return administrator;
            }
            set
            {
                modified = true;
                administrator = value;
            }
        }
        /// <summary>
        /// Indicates if the user is able to login.
        /// </summary>
        public bool Login
        {
            get
            {
                return login;
            }
            set
            {
                modified = true;
                login = value;
            }
        }
        /// <summary>
        /// Indicates if this model has been persisted to the database.
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
                return modified;
            }
        }
    }
}