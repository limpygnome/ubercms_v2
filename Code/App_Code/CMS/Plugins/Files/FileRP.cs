using System;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;

namespace CMS.Plugins.Files
{
    public class FileRP : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public FileRP(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            // Objects
            if ((renderTypes & RenderType.TextFormatting) == RenderType.TextFormatting)
            {
            }
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
            }
        }
    }
}