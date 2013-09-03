using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Plugins
{
    public class TextRenderer : Plugin
    {
        // Fields ******************************************************************************************************
        private List<RenderProvider> cacheProviders;
        // Methods - Constructors **************************************************************************************
        public TextRenderer(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        { }
        // Methods - CMS ***********************************************************************************************
        public override bool install(Connector conn, ref StringBuilder messageOutput)
        {
            // Set handler hooks
            this.HandlerInfo.PluginStart = true;
            this.HandlerInfo.PluginStop = true;
            this.HandlerInfo.save(conn);
            // Install SQL
            if (!BaseUtils.executeSQL(PathSQL + "/install.sql", conn, ref messageOutput))
                return false;
            // Add default providers
            RenderProvider temp;
            // -- Escaping
            temp = new TRProviders.Escaping(UUID.parse("D7BF0796-632C-4C1E-8E8D-A7244B83BDCC"), this.UUID, "Escaping", "Stops text from being formatted by changing characters to HTML entities, including HTML.", true, int.MaxValue / 2);
            if (!temp.save(this, conn))
            {
                messageOutput.AppendLine("Failed to create 'Escaping' text renderer provider!");
                return false;
            }
            // -- Audio/video
            temp = new TRProviders.AudioVideo(UUID.parse("CCC84D75-FC9C-44E7-942C-5AE20293E9D0"), this.UUID, "Embedding: Audio & Video", "Allows embedding of audio and video objects.", true, 0);
            if (!temp.save(this, conn))
            {
                messageOutput.AppendLine("Failed to create 'Embedding: Audio & Video' text renderer provider!");
                return false;
            }
            // -- Code
            temp = new TRProviders.Code(UUID.parse("72E7D8A8-F9F2-40A7-9DF2-9CF09EFF3BFE"), this.UUID, "Embedding: Code", "Allows sections of code to be embedded and syntactically highlighted.", true, int.MaxValue / 8);
            if (!temp.save(this, conn))
            {
                messageOutput.AppendLine("Failed to create 'Embedding: Code' text renderer provider!");
                return false;
            }
            // -- Text
            temp = new TRProviders.Text(UUID.parse("C42FAE9D-3E0C-4D4D-8D4F-F2CAF72FB1C6"), this.UUID, "Text Formatting", "General BBCode text-formatting.", true, 0);
            if (!temp.save(this, conn))
            {
                messageOutput.AppendLine("Failed to create 'Text Formatting' text renderer provider!");
                return false;
            }
            return true;
        }
        public override bool uninstall(Connector conn, ref StringBuilder messageOutput)
        {
            // Uninstall SQL
            if (!BaseUtils.executeSQL(PathSQL + "/uninstall.sql", conn, ref messageOutput))
                return false;
            return true;
        }
        public override bool enable(Connector conn, ref StringBuilder messageOutput)
        {
            // Add pre-processing directives
            if (!BaseUtils.preprocessorDirective_Add("TextRendering", ref messageOutput))
                return false;
            return true;
        }
        public override bool disable(Connector conn, ref StringBuilder messageOutput)
        {
            // Remove pre-processing directives
            if (!BaseUtils.preprocessorDirective_Remove("TextRendering", ref messageOutput))
                return false;
            return true;
        }
        public override bool handler_pluginStart(Connector conn)
        {
            this.cacheProviders = new List<RenderProvider>();
            // Load provider's cache
            if (!reload(conn))
                return false;
            return true;
        }
        public override void handler_pluginStop(Connector conn)
        {
            this.cacheProviders = null;
        }
        // Methods - Providers *****************************************************************************************
        /// <summary>
        /// Adds a provider to the provider's cache. If a provider with the same UUID already exists, it will be
        /// replaced; it's recommended you invoke this method after updating the priority of a model to update it
        /// efficiently without reloading the entire cache.
        /// </summary>
        /// <param name="provider">The render provider to add.</param>
        public void providerAdd(RenderProvider provider)
        {
            lock (this)
            {
                if (cacheProviders == null)
                    return;
                // Remove any providers with the same UUID
                List<RenderProvider> buffer = new List<RenderProvider>();
                foreach (RenderProvider p in cacheProviders)
                    if (p.UUID == provider.UUID)
                        buffer.Add(p);
                foreach (RenderProvider p in buffer)
                    cacheProviders.Remove(p);
                // Add to the end of the list and sift down
                cacheProviders.Add(provider);
                int index = cacheProviders.Count - 1;
                RenderProvider t;
                while (index-- > 0 && cacheProviders[index].Priority > cacheProviders[index - 1].Priority)
                {
                    t = cacheProviders[index];
                    // Swap
                    cacheProviders[index] = cacheProviders[index - 1];
                    cacheProviders[index - 1] = t;
                }
            }
        }
        /// <summary>
        /// Removes a provider from the provider's cache.
        /// </summary>
        /// <param name="provider">The render provider to be removed.</param>
        public void providerRemove(RenderProvider provider)
        {
            lock (this)
            {
                cacheProviders.Remove(provider);
            }
        }
        /// <summary>
        /// Reloads the internal cache of providers.
        /// </summary>
        public bool reload(Connector conn)
        {
            lock (this)
            {
                // Wipe the cache
                cacheProviders.Clear();
                try
                {
                    // Load all the tuples and parse them from the database, and then add them to the cache
                    // -- Since we're reading them from the database in an order, no need to order the list
                    RenderProvider provider;
                    Assembly ass = Assembly.GetExecutingAssembly();
                    foreach (ResultRow row in conn.queryRead("SELECT * FROM view_textrendering_providers WHERE enabled='1' ORDER BY priority DESC;"))
                    {
                        if ((provider = RenderProvider.load(ass, row)) != null)
                            cacheProviders.Add(provider);
                    }
                    return true;
                }
                catch
                {
                    cacheProviders.Clear();
                    return false;
                }
            }
        }
        /// <summary>
        /// Removes all providers owned by a plugin from both the database and provider's cache.
        /// </summary>
        /// <param name="conn">Database connector.</param>
        /// <param name="uuidPlugin">The UUID of the plugin which owns the provider(s).</param>
        public void providersRemove(Connector conn, UUID uuidPlugin)
        {
            lock (this)
            {
                List<RenderProvider> buffer = new List<RenderProvider>();
                // Find all the providers owned by the plugin
                foreach (RenderProvider provider in cacheProviders)
                {
                    if (provider.UUID_Plugin == uuidPlugin)
                    {
                        provider.remove(this, conn);
                        buffer.Add(provider);
                    }
                }
                // Remove all those providers from the cache
                foreach (RenderProvider u in buffer)
                    cacheProviders.Remove(u);
            }
        }
        // Methods - Rendering *****************************************************************************************
        /// <summary>
        /// Renders a piece of text.
        /// </summary>
        /// <param name="data">The data for the current request.</param>
        /// <param name="text">The text to be rendered.</param>
        /// <param name="renderTypes">The type of rendering to perform.</param>
        public void render(Data data, ref StringBuilder text, RenderProvider.RenderType renderTypes)
        {
            lock (this)
            {
                foreach (RenderProvider provider in cacheProviders)
                    provider.render(data, ref text, renderTypes);
            }
        }
        // Methods - Properties ****************************************************************************************
        /// <summary>
        /// A cached array of ordered and enabled providers currently loaded in the runtime used for all rendering
        /// operations.
        /// 
        /// Note: this property is expensive and should be stored in a variable.
        /// </summary>
        public RenderProvider[] CacheProviders
        {
            get
            {
                lock (this)
                {
                    return cacheProviders.ToArray();
                }
            }
        }
    }
}