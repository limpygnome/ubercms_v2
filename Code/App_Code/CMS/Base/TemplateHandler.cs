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
 *      Path:           /App_Code/CMS/Base/TemplateHandler.cs
 * 
 *      Change-Log:
 *                      2013-07-31      Created and finished initial class.
 * 
 * *********************************************************************************************************************
 * A model for template handler functions, apart of the templating system.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Base
{
    public class TemplateHandler
    {
        // Delegates ***************************************************************************************************
        public delegate string TemplateFunction(Data data, string[] args);
        // Fields ******************************************************************************************************
        private bool                persisted,      // Indicates if this model has been persisted.
                                    modified;       // Indicates if this model has been modified.
        private TemplateFunction    function;       // The underlying template function invoked by the template renderer.
        private UUID                uuid;           // The owner of the handler (plugin).
        private string              path,           // The path/name of the function when called by a template.
                                    classPath,      // The class-path to the template function handler.
                                    functionName;   // The name of the actual function (well method) for the handler.
        // Methods - Constructors **************************************************************************************
        public TemplateHandler()
        {
            this.persisted = this.modified = false;
        }
        // Methods - Database Persistence ******************************************************************************
        /// <summary>
        /// Loads a model using a database tuple/result-row.
        /// 
        /// Note: uuid attribute should be in hex format without hypthens.
        /// </summary>
        /// <param name="data">Database tuple/result-row.</param>
        /// <returns>Model or null.</returns>
        public static TemplateHandler load(ResultRow data)
        {
            TemplateHandler th = new TemplateHandler();
            th.uuid = UUID.createFromHex(data["uuid"]);
            th.path = data.get2<string>("path");
            th.classPath = data.get2<string>("classpath");
            th.functionName = data.get2<string>("function_name");
            th.generateDelegate();
            return th;
        }
        /// <summary>
        /// Persists the model to the database.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        public void save(Connector conn)
        {
            lock (this)
            {
                if (!modified)
                    return;
                SQLCompiler sql = new SQLCompiler();
                sql["uuid"] = uuid != null ? uuid.Bytes : null;
                sql["classpath"] = classPath;
                sql["function_name"] = functionName;
                if (persisted)
                {
                    sql.UpdateAttribute = "path";
                    sql.UpdateValue = path;
                    sql.executeUpdate(conn, "cms_template_handlers");
                }
                else
                {
                    sql["path"] = path;
                    sql.executeInsert(conn, "cms_template_handlers");
                    persisted = true;
                }
                modified = false;
            }
        }
        /// <summary>
        /// Unpersists the data from the database.
        /// </summary>
        public void remove(Connector conn)
        {
            PreparedStatement ps = new PreparedStatement("DELETE FROM cms_template_handlers WHERE path=?path;");
            ps["path"] = path;
            conn.queryExecute(ps);
            this.persisted = false;
            this.modified = true;
        }
        // Methods *****************************************************************************************************
        private void generateDelegate()
        {
            lock (this)
            {
                function = null;
                if (classPath != null && functionName != null)
                {
                    try
                    {
                        Type t = Assembly.GetExecutingAssembly().GetType(classPath, false);
                        // Check we found the type - ignore if we haven't, function calls will state it's not defined (informing the developer)
                        if (t != null)
                        {
                            // Fetch the information regarding the template handler function (method)
                            MethodInfo m = t.GetMethod(functionName);
                            if (m != null)
                                // Convert to delegate and set
                                function = new TemplateFunction(Delegate.CreateDelegate(typeof(TemplateFunction), m) as TemplateFunction);
                        }
                    }
                    catch { }
                }
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// The path/function-name when being called from a template.
        /// 
        /// Note: if this model has already been persisted, modifying this property will have no affect.
        /// </summary>
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                if (persisted)
                    return;
                path = value;
                modified = true;
            }
        }
        /// <summary>
        /// The UUID of the plugin which owns this template function handler; may be null.
        /// </summary>
        public UUID UUID
        {
            get
            {
                return uuid;
            }
            set
            {
                uuid = value;
                modified = true;
            }
        }
        /// <summary>
        /// The class-path of the underlying method for the template function handler.
        /// 
        /// Note: changing this property will cause the underlying delegate to be regenerated.
        /// </summary>
        public string ClassPath
        {
            get
            {
                return classPath;
            }
            set
            {
                classPath = value;
                modified = true;
                generateDelegate();
            }
        }
        /// <summary>
        /// The name of the method at the class-path for the template function handler.
        /// 
        /// Note: changing this property will cause the underlying delegate to be regenerated.
        /// </summary>
        public string FunctionName
        {
            get
            {
                return functionName;
            }
            set
            {
                functionName = value;
                modified = true;
                generateDelegate();
            }
        }
        /// <summary>
        /// The delegate for the underlying template function handler.
        /// </summary>
        public TemplateFunction FunctionDelegate
        {
            get
            {
                return function;
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