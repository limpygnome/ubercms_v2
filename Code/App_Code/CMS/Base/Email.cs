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
 *      Path:           /App_Code/CMS/Base/Email.cs
 * 
 *      Change-Log:
 *                      2013-08-26      Created and finished initial class.
 * 
 * *********************************************************************************************************************
 * A model for representing an e-mail handled by the e-mail queue.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using UberLib.Connector;

namespace CMS.Base
{
    public class Email
    {
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            EmailDestination = 1,
            Subject = 2,
            Content = 4,
            IsHtml = 8
        };
        // Fields ******************************************************************************************************
        private bool        persisted;              // Indicates if the model has been persisted.
        private Fields      modified;               // Indicates if the model has been modified.
        private int         emailid;                // The identifier of the e-mail when persisted.
        private string      emailDestination,       // The destination e-mail address.
                            subject,                // The subject of the e-mail.
                            content;                // The content of the e-mail.
        private bool        isHtml;                 // Indicates if the content is HTML (else plain-text).
        // Methods - Constructors **************************************************************************************
        public Email()
        {
            this.modified = Fields.None;
            this.emailid = -1;
            this.emailDestination = this.subject = this.content = null;
            this.isHtml = false;
        }
        public Email(string emailDestination, string subject, string content, bool isHtml)
        {
            this.modified = Fields.EmailDestination | Fields.Subject | Fields.Content | Fields.IsHtml;
            this.emailDestination = emailDestination;
            this.subject = subject;
            this.content = content;
            this.isHtml = isHtml;
        }
        // Methods - Persistence ***************************************************************************************
        /// <summary>
        /// Loads an e-mail model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="emailid">E-mail identifier.</param>
        /// <returns>Model or null.</returns>
        public Email load(Connector conn, int emailid)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM cms_email_queue WHERE emailid=?emailid;");
            ps["emailid"] = emailid;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(r[0]) : null;
        }
        /// <summary>
        /// Loads an e-mail model from a database row/tuple.
        /// </summary>
        /// <param name="data">Database row/tuple.</param>
        /// <returns>Model or null.</returns>
        public Email load(ResultRow data)
        {
            Email e = new Email();
            e.persisted = true;
            // Set model data
            e.emailid = data.get2<int>("emailid");
            e.emailDestination = data.get2<string>("email");
            e.subject = data.get2<string>("subject");
            e.content = data.get2<string>("content");
            e.isHtml = data["html"] == "1";
            return e;
        }
        /// <summary>
        /// Loads a specified amount of e-mails, with an offset.
        /// 
        /// Note: this may exclude e-mails with sending errors; refer to EmailQueue and SQL file for rule-set (as well
        /// as rules on sorting).
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="amount">The maximum number of models to load.</param>
        /// <param name="page">The offset (page * amount) of items starting at 1.</param>
        /// <returns>An array (may be empty but never null).</returns>
        public Email[] load(Connector conn, int amount, int page)
        {
            List<Email> buffer = new List<Email>();
            Result r = conn.queryRead("SELECT * FROM cms_view_email_queue LIMIT " + amount + " OFFSET " + (amount * page));
            Email t;
            foreach (ResultRow row in r)
            {
                t = load(row);
                if (t != null)
                    buffer.Add(t);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// Persists the e-mail model.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public bool save(Connector conn)
        {
            lock (this)
            {
                if (modified == Fields.None)
                    return false;
                // Compile SQL
                SQLCompiler sql = new SQLCompiler();
                sql["email"] = emailDestination;
                sql["subject"] = subject;
                sql["body"] = content;
                sql["html"] = isHtml ? "1" : "0";
                if (persisted)
                {
                    sql.UpdateAttribute = "emailid";
                    sql.UpdateValue = emailid;
                    sql.executeUpdate(conn, "cms_email_queue");
                }
                else
                    emailid = int.Parse(sql.executeInsert(conn, "cms_email_queue", "emailid")[0]["emailid"]);
                return true;
            }
        }
        /// <summary>
        /// Unpersists this-email model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void remove(Connector conn)
        {
            lock (this)
            {
                if (!persisted)
                    return;
                PreparedStatement ps = new PreparedStatement("DELETE FROM cms_email_queue WHERE emailid=?emailid;");
                ps["emailid"] = emailid;
                conn.queryExecute(ps);
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of the model when persisted.
        /// </summary>
        public int EmailID
        {
            get
            {
                return emailid;
            }
        }
        /// <summary>
        /// The destination of the e-mail.
        /// </summary>
        public string EmailDestination
        {
            get
            {
                return emailDestination;
            }
            set
            {
                emailDestination = value;
                modified |= Fields.EmailDestination;
            }
        }
        /// <summary>
        /// The subject of the e-mail.
        /// </summary>
        public string Subject
        {
            get
            {
                return subject;
            }
            set
            {
                subject = value;
                modified |= Fields.Subject;
            }
        }
        /// <summary>
        /// The content of the e-mail.
        /// </summary>
        public string Content
        {
            get
            {
                return content;
            }
            set
            {
                content = value;
                modified |= Fields.Content;
            }
        }
        /// <summary>
        /// Indicates if the content of the e-mail is HTML (else plain-text if false).
        /// </summary>
        public bool IsHtml
        {
            get
            {
                return isHtml;
            }
            set
            {
                isHtml = value;
                modified |= Fields.IsHtml;
            }
        }

        public bool IsModified
        {
            get
            {
                return modified != Fields.None;
            }
        }
        public bool IsPersisted
        {
            get
            {
                return persisted;
            }
        }
    }
}