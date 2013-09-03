using System;
using System.Collections.Generic;
using System.Text;
using UberLib.Connector;
using CMS.Base;
using CMS.BasicSiteAuth.Models;

namespace CMS.BasicArticles
{
    public class ArticleThreadPermissions
    {
        // Fields ******************************************************************************************************
        private bool                modified,           // Indicates if the model has been modified.
                                    persisted;          // Indicates if the model has been persisted.
        private UUID                uuidThread;         // The article thread these permissions apply to.
        private List<int>           groups;             // The user-groups currently able to view the thread.
        // Methods - Constructors **************************************************************************************
        public ArticleThreadPermissions()
        {
            this.modified = this.persisted = false;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads the permissions of an article thread.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">The identifier of the article thread.</param>
        /// <returns>Article thread permissions.</returns>
        public ArticleThreadPermissions load(Connector conn, UUID uuidThread)
        {
            ArticleThreadPermissions p = new ArticleThreadPermissions();
            p.uuidThread = uuidThread;
            p.persisted = true;
            PreparedStatement ps = new PreparedStatement("SELECT groupid FROM ba_article_thread_permissions;");
            ps["uuid_thread"] = uuidThread;
            foreach (ResultRow r in conn.queryRead(ps))
                p.groups.Add(r.get2<int>("groupid"));
            return p;
        }
        /// <summary>
        /// Persists the permissions to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = not persisted.</returns>
        public bool save(Connector conn)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("BEGIN;");
            // -- Drop existing permissions
            sb.Append("DELETE FROM ba_article_thread_permissions;");
            // -- Insert new ones
            if (groups.Count > 0)
            {
                sb.Append("INSERT INTO ba_article_thread permissions (uuid_thread, groupid) VALUES");
                foreach (int g in groups)
                    sb.Append("(").Append(uuidThread.NumericHexString).Append(", ").Append(g.ToString()).Append("),");
                sb.Remove(sb.Length - 1, 1).Append(";");
            }
            sb.Append("COMMIT;");
            try
            {
                conn.queryExecute(sb.ToString());
                persisted = true;
                modified = false;
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Tests if a user is able to access the current article based on the permissions.
        /// 
        /// Note: user's apart of a user-group with administrator or/and moderator flag(s) set to true will be able to
        /// view all articles.
        /// </summary>
        /// <param name="user">The user to be tested.</param>
        /// <returns>True = authorised, false = not authorised.</returns>
        public bool isAuthorisedView(User user)
        {
            return user.UserGroup.Administrator || user.UserGroup.Moderator || groups.Contains(user.UserGroup.GroupID);
        }
        // Methods - Mutators ******************************************************************************************
        /// <summary>
        /// Adds a user-group to view the article thread.
        /// </summary>
        /// <param name="g"></param>
        public void add(UserGroup g)
        {
            lock (this)
            {
                if (!groups.Contains(g.GroupID))
                {
                    groups.Add(g.GroupID);
                    modified = true;
                }
            }
        }
        /// <summary>
        /// Removes a user-group from viewing the article thread.
        /// </summary>
        /// <param name="g"></param>
        public void remove(UserGroup g)
        {
            lock (this)
            {
                if (groups.Contains(g.GroupID))
                {
                    groups.Remove(g.GroupID);
                    modified = true;
                }
            }
        }
        // Methods - Properties ****************************************************************************************
        public List<int> UserGroups
        {
            get
            {
                return groups;
            }
            set
            {
                lock (this)
                {
                    groups = value;
                    modified = true;
                }
            }
        }
    }
}