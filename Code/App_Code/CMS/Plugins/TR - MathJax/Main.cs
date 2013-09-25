using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CMS.Base;
using UberLib.Connector;

namespace CMS.Plugins
{
    public class MathJax : Plugin
    {
        // Methods - Constructors **************************************************************************************
        public MathJax(UUID uuid, string title, string directory, PluginState state, PluginHandlerInfo handlerInfo, Base.Version version, int priority, string classPath)
            : base(uuid, title, directory, state, handlerInfo, version, priority, classPath)
        { }
        // Methods - CMS ***********************************************************************************************
        public override bool install(Connector conn, ref StringBuilder messageOutput)
        {
            // Get TR instance
            TextRenderer tr = (TextRenderer)Core.Plugins[UUID.parse(TextRenderer.TR_UUID)];
            if (tr == null)
            {
                messageOutput.AppendLine("Could not get text-renderer instance! Cannot continue...");
                return false;
            }
            // Add providers
            RenderProvider temp;
            // -- MathJax Local Installation
            temp = new TRProviders.MathJax(UUID.parse("00C3438F-A0F9-4C9D-BCF7-A76104DE693C"), this.UUID, "MathJax", "Renders mathematical syntax.", true, 0);
            if (!temp.save(tr, conn))
            {
                messageOutput.AppendLine("Failed to create 'Escaping' text renderer provider!");
                return false;
            }
            return true;
        }
        public override bool uninstall(Connector conn, ref StringBuilder messageOutput)
        {
            // Remove provider
            TextRenderer tr = (TextRenderer)Core.Plugins[UUID.parse(TextRenderer.TR_UUID)];
            if (tr != null)
                tr.providersRemove(conn, UUID);
            return true;
        }
        public override bool enable(Connector conn, ref StringBuilder messageOutput)
        {
            // Install content
            //if (!BaseUtils.contentInstall(PathContent, Core.PathContent, false, ref messageOutput))
            //    return false;
            return true;
        }
        public override bool disable(Connector conn, ref StringBuilder messageOutput)
        {
            // Remove content
            //if (!BaseUtils.contentUninstall(PathContent, Core.PathContent, ref messageOutput))
            //    return false;
            return true;
        }
    }
}