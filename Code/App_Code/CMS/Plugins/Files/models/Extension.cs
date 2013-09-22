using CMS.Base;
using System;
using System.Reflection;
using UberLib.Connector;

namespace CMS.Plugins.Files
{
    public class Extension
    {
        // Enums *******************************************************************************************************
        private enum Fields
        {
            None = 0,
            Title = 1,
            UrlIcon = 2,
            RenderClassPath = 4,
            RenderMethod = 8
        };
        // Fields ******************************************************************************************************
        private bool        persisted;              // Indicates if the model has been persisted.
        private Fields      modified;               // Indicates if the model has been modified.
        private string      extension,              // The extension of the extension, without the dot.
                            title,                  // The title of the extension.
                            urlIcon,                // The URL of the extension's icon.
                            renderClasspath,        // The class-path for rendering the media-type.
                            renderMethod;           // The method for rendering the media-type.
        private MethodInfo  miRendering;            // The object used for invoking the renderer method.
        // Methods - Constructors **************************************************************************************
        public Extension()
        {
            this.persisted = false;
            this.modified = Fields.None;
            this.extension = this.title = this.urlIcon = this.renderClasspath = this.renderMethod = null;
            this.miRendering = null;
        }
        // Methods - Persistence ***************************************************************************************
        /// <summary>
        /// Loads a model from database data.
        /// </summary>
        /// <param name="data">The database tuple/data.</param>
        /// <returns>Model or null.</returns>
        public static Extension load(ResultRow data)
        {
            Extension ext = new Extension();
            ext.persisted = true;
            ext.extension = data.contains("extension") ? data["extension"] : null;
            ext.title = data.contains("title") ? data["title"] : null;
            ext.urlIcon = data.contains("url_icon") ? data["url_icon"] : null;
            ext.renderClasspath = data.contains("render_classpath") ? data["render_classpath"] : null;
            ext.renderMethod = data.contains("render_method") ? data["render_method"] : null;
            ext.rebuildRenderer();
            return ext;
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
                if ((modified & Fields.Title) == Fields.Title)
                    sql["title"] = title;
                if ((modified & Fields.UrlIcon) == Fields.UrlIcon)
                    sql["url_icon"] = urlIcon;
                if ((modified & Fields.RenderClassPath) == Fields.RenderClassPath)
                    sql["render_classpath"] = renderClasspath;
                if ((modified & Fields.RenderMethod) == Fields.RenderMethod)
                    sql["render_method"] = renderMethod;
                // Execute
                if (persisted)
                {
                    sql.UpdateAttribute = "extension";
                    sql.UpdateValue = extension;
                    sql.executeUpdate(conn, "fi_extensions");
                }
                else
                {
                    sql["extension"] = extension;
                    sql.executeInsert(conn, "fi_extensions");
                    persisted = true;
                }
                modified = Fields.None;
                return true;
            }
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Rebuilds the renderer method.
        /// </summary>
        public void rebuildRenderer()
        {
            try
            {
                miRendering = Assembly.GetExecutingAssembly().GetType(renderClasspath).GetMethod(renderMethod);
            }
            catch
            {
                miRendering = null;
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The extension of the extension, without the dot.
        /// </summary>
        public string Ext
        {
            get
            {
                return extension;
            }
            set
            {
                lock (this)
                {
                    if (!persisted)
                        extension = value;
                }
            }
        }
        /// <summary>
        /// The title of the extension.
        /// 
        /// Note: can be null.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                lock (this)
                {
                    title = value;
                    modified |= Fields.Title;
                }
            }
        }
        /// <summary>
        /// The URL of the extension's icon.
        /// 
        /// Note: can be null.
        /// </summary>
        public string Url_Icon
        {
            get
            {
                return urlIcon;
            }
            set
            {
                lock (this)
                {
                    urlIcon = value;
                    modified |= Fields.UrlIcon;
                }
            }
        }
        /// <summary>
        /// The class-path for rendering the media-type.
        /// 
        /// Note: can be null.
        /// </summary>
        public string Render_ClassPath
        {
            get
            {
                return renderClasspath;
            }
            set
            {
                lock (this)
                {
                    renderClasspath = value;
                    modified |= Fields.RenderClassPath;
                }
            }
        }
        /// <summary>
        /// The method for rendering the media-type.
        /// 
        /// Note: can be null.
        /// </summary>
        public string Render_Method
        {
            get
            {
                return renderMethod;
            }
            set
            {
                lock (this)
                {
                    renderMethod = value;
                    modified |= Fields.RenderMethod;
                }
            }
        }
        /// <summary>
        /// The object used for invoking the renderer method.
        /// 
        /// Note: can be null.
        /// </summary>
        public MethodInfo Renderer
        {
            get
            {
                return miRendering;
            }
        }
    }
}