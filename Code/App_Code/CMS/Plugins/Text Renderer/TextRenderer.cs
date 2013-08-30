using System;
using CMS.Base;

namespace CMS.Plugins
{
    public class TextRenderer : Plugin
    {
        // Enums

        // Methods - CMS
        public override bool install(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return base.install(conn, ref messageOutput);
        }
        public override bool uninstall(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return base.uninstall(conn, ref messageOutput);
        }
        public override bool enable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return base.enable(conn, ref messageOutput);
        }
        public override bool disable(UberLib.Connector.Connector conn, ref System.Text.StringBuilder messageOutput)
        {
            return base.disable(conn, ref messageOutput);
        }
        public override bool handler_pluginStart(UberLib.Connector.Connector conn)
        {
            // Load provider's cache
            return base.handler_pluginStart(conn);
        }
        public override void handler_pluginStop(UberLib.Connector.Connector conn)
        {
            base.handler_pluginStop(conn);
        }
        // Methods - Providers
        public void providerAdd()
        {
        }
        public void providerRemove(UUID uuidPlugin)
        {
        }
        public void providerRemove()
        {
        }
        // Methods - Rendering
        public void render()
        {
        }
    }
}