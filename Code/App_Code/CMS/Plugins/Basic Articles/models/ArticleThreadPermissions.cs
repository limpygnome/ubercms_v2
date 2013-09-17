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
        // Enums *******************************************************************************************************
        public enum Action
        {
            View,
            Create,
            Edit,
            Rebuild,
            EditPermissions,
            EditThreadInfo,
            Publish,
            Delete,
            DeleteThread,
            MoveThread
        };
        // Fields ******************************************************************************************************
        private bool                modified,           // Indicates if the model has been modified.
                                    persisted;          // Indicates if the model has been persisted.
        private UUID                uuidThread;         // The article thread these permissions apply to.
        private List<int>           groups;             // The user-groups currently able to view the thread.
        private UserGroup           ugAnonymous;        // The cached model of the anonymous user-group for the current request.
        // Methods - Constructors **************************************************************************************
        private ArticleThreadPermissions()
        {
            this.modified = this.persisted = false;
            this.groups = new List<int>();
            this.ugAnonymous = null;
        }
        public ArticleThreadPermissions(UUID uuidThread)
        {
            this.modified = this.persisted = false;
            this.groups = new List<int>();
            this.uuidThread = uuidThread;
            this.ugAnonymous = null;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads the permissions of an article thread.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidThread">The identifier of the article thread.</param>
        /// <returns>Article thread permissions.</returns>
        public static ArticleThreadPermissions load(Connector conn, UUID uuidThread)
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
            if (!modified)
                return false;
            // Compile query
            StringBuilder sb = new StringBuilder();
            sb.Append("BEGIN;");
            // -- Drop existing permissions
            sb.Append("DELETE FROM ba_article_thread_permissions WHERE uuid_thread=" + uuidThread.NumericHexString + ";");
            // -- Insert new ones
            if (groups.Count > 0)
            {
                sb.Append("INSERT INTO ba_article_thread_permissions (uuid_thread, groupid) VALUES");
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
                conn.queryExecute("ROLLBACK;");
                return false;
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Tests if a user is authorised to perform an action.
        /// 
        /// Note: user's apart of a user-group with administrator or/and moderator flag(s) set to true will be able to
        /// view all articles.
        /// 
        /// Warning: if this code is modified, you should be absolutely sure your logic is correct! This section of code
        /// is responsible for nearly, if not all, permissions for this plugin.
        /// </summary>
        /// <param name="user">The user to be tested.</param>
        /// <param name="permissions">The permissions for a thread; can be null.</param>
        /// <param name="article">The article needed for context for some actions; can be null.</param>
        /// <returns>True = authorised, false = not authorised.</returns>
        public static bool isAuthorised(User user, Action action, ArticleThreadPermissions permissions, Article article)
        {
            // Fetch the anonymous user-group for non-authenticated users; this group's permissions may override user permissions if higher than a user
            // -- Else the user could just logout and have higher permissions, which makes no sense.
            UserGroup anon;
            if (permissions != null)
            {
                if (permissions.ugAnonymous != null)
                    anon = permissions.ugAnonymous;
                else
                    anon = permissions.ugAnonymous = getAnonymousGroup();
            }
            else
                anon = getAnonymousGroup();
            // Check which permission is being tested
            switch (action)
            {
                case Action.View:
                    return (anon != null && (anon.Moderator || anon.Administrator || (permissions != null && permissions.groups.Contains(anon.GroupID)))) ||
                        (user != null &&
                                        (user.UserGroup.Administrator || user.UserGroup.Moderator || permissions.groups.Contains(user.UserGroup.GroupID))
                        );
                case Action.Create:
                    return (anon != null && (anon.Pages_Create || anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Create || user.UserGroup.Moderator || user.UserGroup.Administrator));
                case Action.Edit:
                case Action.Rebuild:
                    return (anon != null && (anon.Pages_Modify || anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Modify || user.UserGroup.Moderator || user.UserGroup.Administrator || (article != null && article.UserIdAuthor == user.UserID && user.UserGroup.Pages_ModifyOwn)));
                case Action.Delete:
                    return (anon != null && (anon.Pages_Delete || anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Delete || user.UserGroup.Moderator || user.UserGroup.Administrator || (article != null && article.UserIdAuthor == user.UserID && user.UserGroup.Pages_DeleteOwn)));
                case Action.DeleteThread:
                    return (anon != null && (anon.Pages_Modify || anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Modify || user.UserGroup.Moderator || user.UserGroup.Administrator));
                case Action.MoveThread:
                    return (anon != null && (anon.Pages_Delete || anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Delete || user.UserGroup.Moderator || user.UserGroup.Administrator));
                case Action.EditPermissions:
                    return (anon != null && (anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Moderator || user.UserGroup.Administrator));
                case Action.EditThreadInfo:
                    return (anon != null && (anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Create || user.UserGroup.Moderator || user.UserGroup.Administrator));
                case Action.Publish:
                    return (anon != null && (anon.Pages_Publish || anon.Moderator || anon.Administrator)) || (user != null && (user.UserGroup.Pages_Publish || user.UserGroup.Moderator || user.UserGroup.Administrator));
                default:
                    return false;
            }
        }
        public static UserGroup getAnonymousGroup()
        {
            BasicSiteAuth.BasicSiteAuth bsa = BasicSiteAuth.BasicSiteAuth.getCurrentInstance();
            return bsa == null ? null : bsa.UserGroups[Core.Settings[BasicSiteAuth.BasicSiteAuth.SETTINGS_GROUP_ANONYMOUS_GROUPID].get<int>()];
        }
        // Methods - Mutators ******************************************************************************************
        /// <summary>
        /// Adds a user-group to view the article thread.
        /// </summary>
        /// <param name="g">The user-group.</param>
        public void add(UserGroup g)
        {
            lock (this)
            {
                if (g != null && !groups.Contains(g.GroupID))
                {
                    groups.Add(g.GroupID);
                    modified = true;
                }
            }
        }
        /// <summary>
        /// Adds a user-group to view the article thread.
        /// </summary>
        /// <param name="groupID">The identifier of the user-group.</param>
        public void add(int groupID)
        {
            lock (this)
            {
                if (!groups.Contains(groupID))
                {
                    groups.Add(groupID);
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
        /// <summary>
        /// Indicates if the collection contains the user-group.
        /// </summary>
        /// <param name="g"The user-group.></param>
        /// <returns>True = exists, false = not exists.</returns>
        public bool contains(UserGroup g)
        {
            return contains(g.GroupID);
        }
        /// <summary>
        /// Indicates if the collection contains the user-group.
        /// </summary>
        /// <param name="groupID"The user-group identifier.></param>
        /// <returns>True = exists, false = not exists.</returns>
        public bool contains(int groupID)
        {
            return groups.Contains(groupID);
        }
        /// <summary>
        /// Clears all of the user-groups added.
        /// </summary>
        public void clear()
        {
            groups.Clear();
            modified = true;
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