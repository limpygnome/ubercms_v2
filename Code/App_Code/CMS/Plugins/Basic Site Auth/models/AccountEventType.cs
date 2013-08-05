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
 *      Path:           /App_Code/CMS/Plugins/Basic Site Auth/models/AccountEventType.cs
 * 
 *      Change-Log:
 *                      2013-07-24      Created initial class.
 *                      2013-08-01      Finished initial class.
 * 
 * *********************************************************************************************************************
 * A model for account event types.
 * *********************************************************************************************************************
 */
using System;
using System.Text;
using System.Reflection;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth.Models
{
    /// <summary>
    /// A model for account event types.
    /// </summary>
    public class AccountEventType
    {
        // Fields ******************************************************************************************************
        bool        persisted,              // Indicates if the model has been persisted to the database.
                    modified;               // Indicates if the model has been modified.
        UUID        typeUUID;               // The identifier of the event-type in the database.
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
        /// Creates and persists a new account event type model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="bsa">BSA plugin.</param>
        /// <param name="typeUUID">The identifier for the account event type.</param>
        /// <param name="title">Title.</param>
        /// <param name="description">Description.</param>
        /// <param name="renderClassPath">The class-path to the function for rendering the type of event.</param>
        /// <param name="renderFunction">The function at the class-path for rendering the type of event.</param>
        /// <param name="messageOutput">Message output.</param>
        /// <returns>Either the new model or null if an error occurred (check messageOutput for information).</returns>
        public static AccountEventType create(Connector conn, BasicSiteAuth bsa, UUID typeUUID, string title, string description, string renderClassPath, string renderFunction, ref StringBuilder messageOutput)
        {
            try
            {
                AccountEventType at = new AccountEventType();
                at.TypeUUID = typeUUID;
                at.Title = title;
                at.Description = description;
                at.RenderClassPath = renderClassPath;
                at.RenderFunction = renderFunction;
                if (at.save(bsa, conn))
                    return at;
                else
                {
                    messageOutput.Append("Failed to persist account event type model '").Append(title).Append("' (UUID: ").Append(typeUUID.HexHyphens).AppendLine(")!");
                    return null;
                }
            }
            catch (Exception ex)
            {
                messageOutput.Append("Failed to create account event type model due to exception: '").Append(ex.Message).AppendLine("'!");
                return null;
            }
        }
        /// <summary>
        /// Loads an instance of an account event type using its identifier.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="typeUUID">Account event type identifier.</param>
        /// <returns>Either an instance or null.</returns>
        public static AccountEventType load(Connector conn, UUID typeUUID)
        {
            PreparedStatement ps = new PreparedStatement("SELECT * FROM bsa_view_aet WHERE type_uuid=?typeid;");
            ps["typeid"] = typeUUID.Bytes;
            Result result = conn.queryRead(ps);
            return result.Count == 1 ? load(result[0]) : null;
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
            a.typeUUID = UUID.createFromHex(data.get2<string>("type_uuid"));
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
        /// <returns>True if persisted, false if failed.</returns>
        public bool save(BasicSiteAuth bsa, Connector conn)
        {
            if (!modified)
                return false;
            // Persist the data
            SQLCompiler c = new SQLCompiler();
            c["title"] = title;
            c["description"] = description;
            c["render_classpath"] = renderClasspath;
            c["render_function"] = renderFunction;
            if (persisted)
            {
                c.UpdateAttribute = "type_uuid";
                c.UpdateValue = typeUUID.Bytes;
                c.executeUpdate(conn, "bsa_account_event_types");
            }
            else
            {
                c["type_uuid"] = typeUUID.Bytes;
                c.executeInsert(conn, "bsa_account_event_types");
                persisted = true;
            }
            modified = false;
            return true;
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of the event-type in the database.
        /// 
        /// If the model has already been persisted, setting this property will have no effect.
        /// </summary>
        public UUID TypeUUID
        {
            get
            {
                return typeUUID;
            }
            set
            {
                if (persisted)
                    return;
                this.typeUUID = value;
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
        public string RenderFunction
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