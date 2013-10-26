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
 *      Path:           /App_Code/CMS/Plugins/Text Renderer/providers/Navigation.cs
 * 
 *      Change-Log:
 *                      2013-09-23      Finished initial class.
 *						2013-10-25		Content text removed, general bug-fixes and improvements.
 * 
 * *********************************************************************************************************************
 * A text-renderer provider for navigation related features and additions.
 * *********************************************************************************************************************
 */
using CMS.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CMS.Plugins.TRProviders
{
    /// <summary>
    /// A text-renderer provider for navigation related features and additions.
    /// </summary>
    public class Navigation : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public Navigation(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder header, ref StringBuilder text, RenderType renderTypes)
        {
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                navigationBox(ref text, ref data);
            }
        }
        private static void navigationBox(ref StringBuilder text, ref Data data)
        {
            MatchCollection contentBoxes = Regex.Matches(text.ToString(), @"\[navigation\]", RegexOptions.Multiline);
            if (contentBoxes.Count != 0)
            {
                // We found content-boxes; hence we'll now format the headings and build the content box
                StringBuilder contentBox = new StringBuilder();
                string titleFormatted, anchor;
                int headingParent = 2; // Since h1 and h2 are already used, content will use h3 onwards
                int currentTreeLevel = headingParent; // 1 = parent/not within a tree; this corresponds with e.g. heading 2-6
                int treeParse, treeChangeDir;
                List<string> titlesReserved = new List<string>(); // This will be used to avoid title-clashes; this is highly likely in sub-trees
                int titleOffset;
                int matchOffset = 0; // Every-time we insert an anchor, the match index is offsetted by the length of the anchor - which we'll store in here
                int[] nodeCount = new int[4]; // This should be at the max heading count i.e. |3,4,5,6| = 4
                foreach (Match m in Regex.Matches(text.ToString(), @"\<h(3|4|5|6)\>(.*?)\<\/h(\1)\>", RegexOptions.Multiline))
                {
                    // Check the tree is valid and if it has changed
                    treeParse = int.Parse(m.Groups[1].Value);
                    if (currentTreeLevel != treeParse)
                    {
                        // Tree has changed; check what to do...
                        treeChangeDir = treeParse - currentTreeLevel;
                        if (treeChangeDir >= 1)
                        {
                            // We've gone in by a level
                            while (--treeChangeDir >= 0)
                            {
                                contentBox.Append("<ol>");
                                currentTreeLevel++;
                            }
                            // We only want to reset the count for the current node if we go back into a new node; hence this is not done when exiting a level
                            nodeCount[currentTreeLevel - (headingParent + 1)] = 0;
                        }
                        else if (treeChangeDir <= -1)
                        {
                            // We've came out by a level
                            while (++treeChangeDir <= 0)
                            {
                                contentBox.Append("</ol>");
                                currentTreeLevel--;
                            }
                        }
                    }
                    // Format the title
                    titleFormatted = HttpUtility.UrlEncode(m.Groups[2].Value.Replace(" ", "_"));
                    titleOffset = 1;
                    if (titlesReserved.Contains(titleFormatted))
                    {
                        // Increment the counter until we find a free title
                        while (titlesReserved.Contains(titleFormatted + "_" + titleOffset) && titleOffset < int.MaxValue)
                            titleOffset++;
                        titleFormatted += "_" + titleOffset;
                    }
                    // Reserve the title
                    titlesReserved.Add(titleFormatted);
                    // Insert a hyper-link at the position of the heading
                    anchor = "<a id=\"" + titleFormatted + "\"></a>\n";
                    text.Insert(m.Index + matchOffset, anchor);
                    matchOffset += anchor.Length;
                    // Increment node count
                    nodeCount[currentTreeLevel - (headingParent + 1)]++;
                    // Add title to content box
                    contentBox.Append("<li><a href=\"#").Append(titleFormatted).Append("\">").Append(navigationBox_nodeStr(nodeCount, currentTreeLevel - headingParent)).Append(" ").Append(m.Groups[2]).Append("</a></li>");
                }
                // Check if we ever added anything; if so we'll need closing tags for each level
                for (int i = headingParent; i <= currentTreeLevel; i++)
                    contentBox.Append("</ol>");
                // Add content-box wrapper
                contentBox.Insert(0, "<div class=\"navigation\">").Append("</div>");
                // Add the content boxes
                string contentBoxFinalized = contentBox.ToString();
                foreach (Match m in contentBoxes)
                    text.Replace(m.Value, contentBoxFinalized);
            }
        }
        private static string navigationBox_nodeStr(int[] nodeCount, int currentNodeSubractParentOffset)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < currentNodeSubractParentOffset; i++)
                sb.Append(nodeCount[i]).Append(".");
            return sb.ToString();
        }
    }
}