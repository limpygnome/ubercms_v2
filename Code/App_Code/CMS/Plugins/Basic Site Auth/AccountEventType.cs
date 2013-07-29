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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/AccountEventType.cs
 * 
 *      Change-Log:
 *                      2013-07-24      Created initial class.
 * 
 * *********************************************************************************************************************
 * A model for account event types.
 * *********************************************************************************************************************
 */
using System;
using System.Reflection;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    /// <summary>
    /// A model for account event types.
    /// </summary>
    public class AccountEventType
    {
        // Fields ******************************************************************************************************
        bool        persisted,              // Indicates if the model has been persisted to the database.
                    modified;               // Indicates if the model has been modified.
        int         eventTypeID;            // The identifier of the event-type in the database.
        string      title,                  // The title of the event-type.
                    description,            // A description of the event-type.
                    renderClasspath,        // The class-path of the function for rendering an event of this type.
                    renderFunction;         // The function name of the function for rendering an event of this type.
        MethodInfo  renderMethod;           // The function for rendering.
        // Methods - Constructors **************************************************************************************
        public AccountEventType()
        {
            this.persisted = this.modified = false;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads an instance of an account event type using its identifier.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="eventTypeID">Account event type identifier.</param>
        /// <returns>Either an instance or null.</returns>
        public static AccountEventType load(Connector conn, int eventTypeID)
        {
            Result result = conn.queryRead("SELECT * FROM bsa_account_event_types WHERE eventtypeid='" + SQLUtils.escape(eventTypeID.ToString()) + "';");
            if (result.Count == 1)
                return load(result[0]);
            else
                return null;
        }
        /// <summary>
        /// Loads an instance of an account event type using a database result-row/tuple.
        /// </summary>
        /// <param name="data">Database result-row/tuple.</param>
        /// <returns>Either an instance or null.</returns>
        public static AccountEventType load(ResultRow data)
        {
            AccountEventType a = new AccountEventType();
            a.persisted = true;
            a.eventTypeID = data.get2<int>("eventtypeid");
            a.title = data.get2<string>("title");
            a.description = data.get2<string>("description");
            a.renderClasspath = data.get2<string>("render_classpath");
            a.renderFunction = data.get2<string>("render_function");
            try
            {
                a.renderMethod = Assembly.GetExecutingAssembly().GetType(a.renderClasspath).GetMethod(a.renderFunction);
            }
            catch
            {
                a.renderMethod = null;
            }
            return a;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="bsa">Basic site authentication plugin.</param>
        /// <param name="conn">Database connector.</param>
        public void save(BasicSiteAuth bsa, Connector conn)
        {
            if (!modified)
                return;
            SQLCompiler c = new SQLCompiler();
            c["title"] = title;
            c["description"] = description;
            c["render_classpath"] = renderClasspath;
            c["render_function"] = renderFunction;
            if (persisted)
            {
                c.UpdateAttribute = "eventtypeid";
                c.UpdateValue = eventTypeID;
                c.executeUpdate(conn, "bsa_account_event_types");
            }
            else
            {
                eventTypeID = c.executeInsert(conn, "bsa_account_event_types")[0].get2<int>("eventtypeid");
                persisted = true;
            }
            modified = false;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of the event-type in the database.
        /// </summary>
        public int EventTypeID
        {
            get
            {
                return eventTypeID;
            }
        }
        /// <summary>
        /// The title of the event-type.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                modified = true;
            }
        }
        /// <summary>
        /// A description of the event-type.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
                modified = true;
            }
        }
        /// <summary>
        /// The class-path of the function for rendering an event of this type.
        /// </summary>
        public string RenderClassPath
        {
            get
            {
                return renderClasspath;
            }
            set
            {
                renderClasspath = value;
                modified = true;
            }
        }
        /// <summary>
        /// The function name of the function for rendering an event of this type.
        /// </summary>
        public string RenderClassFunction
        {
            get
            {
                return renderFunction;
            }
            set
            {
                renderFunction = value;
                modified = true;
            }
        }
        /// <summary>
        /// The function for rendering.
        /// </summary>
        public MethodInfo RenderMethod
        {
            get
            {
                return renderMethod;
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
    }
}