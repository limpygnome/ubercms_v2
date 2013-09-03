using System;
using System.Text;
using System.Reflection;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Plugins
{
    public abstract class RenderProvider
    {
        // Enums *******************************************************************************************************
        /// <summary>
        /// The type of rendering to perform.
        /// </summary>
        public enum RenderType
        {
            /// <summary>
            /// Renders any text markup such as colouring, size and font-face.
            /// </summary>
            TextFormatting,
            /// <summary>
            /// Rneders any object markup such as imagery.
            /// </summary>
            Objects
        }
        private enum Fields
        {
            None = 0,
            UUID_Plugin = 1,
            Title = 2,
            Description = 4,
            Enabled = 8,
            Priority = 16
        };
        // Fields ******************************************************************************************************
        private bool    persisted;      // Indicates if this model has been persisted.
        private Fields  modified;       // Indicates which fields of the model have been modified.
        private UUID    uuid,           // The identifier of this model.
                        uuidPlugin;     // The identifier of the plugin which owns this model; can be null.
        private string  title,          // The title of the model; can be null.
                        description;    // A description of the model; can be null.
        private bool    enabled;        // Indicates if this provider is enabled.
        private int     priority;       // The priority of rendering; highest values are served first.
        // Methods - Constructors **************************************************************************************
        public RenderProvider()
        {
            this.persisted = false;
            this.modified = Fields.None;
            this.uuid = uuidPlugin = null;
            this.title = this.description;
            this.enabled = false;
        }
        public RenderProvider(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
        {
            this.persisted = false;
            this.modified = Fields.UUID_Plugin | Fields.Title | Fields.Description | Fields.Enabled | Fields.Priority;
            this.uuid = uuid;
            this.uuidPlugin = uuidPlugin;
            this.title = title;
            this.description = description;
            this.enabled = enabled;
            this.priority = priority;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads a render provider model.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuid">The identifier of the model.</param>
        /// <returns>Model or null.</returns>
        public static RenderProvider load(Connector conn, UUID uuid)
        {
            if (uuid == null)
                return null;
            PreparedStatement ps = new PreparedStatement("SELECT * FROM view_textrendering_providers WHERE uuid=?uuid;");
            ps["uuid"] = uuid.Bytes;
            Result r = conn.queryRead(ps);
            return r.Count == 1 ? load(Assembly.GetExecutingAssembly(), r[0]) : null;
        }
        /// <summary>
        /// Loads a render provider model.
        /// </summary>
        /// <param name="ass">The assembly to load the model from (use Assembly.GetExecutingAssembly() if unsure).</param>
        /// <param name="row">The database tuple/row.</param>
        /// <returns>Model or null.</returns>
        public static RenderProvider load(Assembly ass, ResultRow row)
        {
            // Create instance of class
            RenderProvider p;
            try
            {
                p = (RenderProvider)ass.CreateInstance(row["classpath"], false, BindingFlags.CreateInstance, null, new object[] { }, null, null);
            }
            catch
            {
                return null;
            }
            p.persisted = true;
            // Load model data
            p.uuid = UUID.parse(row["uuid"]);
            p.uuidPlugin = UUID.parse(row["uuid_plugin"]);
            p.title = row.get2<string>("title");
            p.description = row.get2<string>("description");
            p.enabled = row["enabled"].Equals("1");
            p.priority = row.get2<int>("priority");
            return p;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <returns>True = persisted, false = not persisted.</returns>
        public bool save(TextRenderer tr, Connector conn)
        {
            // Check the model has been modified and the UUID is not null
            if (modified == Fields.None || uuid == null)
                return false;
            // Compile SQL
            SQLCompiler sql = new SQLCompiler();
            sql["uuid"] = uuid.Bytes;
            if((modified & Fields.UUID_Plugin) == Fields.UUID_Plugin)
                sql["uuid_plugin"] = uuidPlugin.Bytes;
            if ((modified & Fields.Title) == Fields.Title)
                sql["title"] = title;
            if ((modified & Fields.Description) == Fields.Description)
                sql["description"] = description;
            if ((modified & Fields.Enabled) == Fields.Enabled)
                sql["enabled"] = enabled ? "1" : "0";
            if ((modified & Fields.Priority) == Fields.Priority)
                sql["priority"] = priority;
            try
            {
                // Execute SQL
                if (persisted)
                {
                    sql.UpdateAttribute = "uuid";
                    sql.UpdateValue = uuid.Bytes;
                    sql.executeUpdate(conn, "textrendering_providers");
                }
                else
                {
                    sql["classpath"] = GetType().FullName;
                    sql["uuid"] = uuid.Bytes;
                    sql.executeInsert(conn, "textrendering_providers");
                }
                // Update textrenderer version of this model by readding it
                tr.providerAdd(this);
            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Unpersists the model from the database and providers cache.
        /// </summary>
        /// <param name="conn"></param>
        public void remove(TextRenderer rendering, Connector conn)
        {
            lock (this)
            {
                PreparedStatement ps = new PreparedStatement("DELETE FROM textrendering_providers WHERE uuid=?uuid;");
                ps["uuid"] = uuid.Bytes;
                conn.queryExecute(ps);
                rendering.providerRemove(this);
                persisted = false;
            }
        }
        // Methods - Virtual *******************************************************************************************
        /// <summary>
        /// Renders a piece of text.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="text">The text to be rendered.</param>
        /// <param name="renderTypes">The type of rendering to perform.</param>
        public virtual void render(Data data, ref StringBuilder text, RenderProvider.RenderType renderTypes)
        {
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The identifier of this model.
        /// </summary>
        public UUID UUID
        {
            get
            {
                return uuid;
            }
        }
        /// <summary>
        /// The identifier of the plugin which owns this model; can be null.
        /// </summary>
        public UUID UUID_Plugin
        {
            get
            {
                return uuidPlugin;
            }
            set
            {
                uuidPlugin = value;
                modified |= Fields.UUID_Plugin;
            }
        }
        /// <summary>
        /// The title of the model; can be null.
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
                modified |= Fields.Title;
            }
        }
        /// <summary>
        /// A description of the model; can be null.
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
        /// Indicates if this provider is enabled.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                lock (this)
                {
                    enabled = value;
                    modified |= Fields.Enabled;
                }
            }
        }
        /// <summary>
        /// The priority of the provider. Providers with higher priorities are served first; recommended default value
        /// of zero, unless there is a particular reason.
        /// </summary>
        public int Priority
        {
            get
            {
                return priority;
            }
            set
            {
                lock (this)
                {
                    priority = value;
                    modified |= Fields.Priority;
                }
            }
        }
    }
}