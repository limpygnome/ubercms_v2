using System;
using System.Text;
using System.Text.RegularExpressions;
using CMS.Base;

namespace CMS.Plugins.TRProviders
{
    public class Text : CMS.Plugins.RenderProvider
    {
        // Methods - Constructors **************************************************************************************
        public Text(UUID uuid, UUID uuidPlugin, string title, string description, bool enabled, int priority)
            : base(uuid, uuidPlugin, title, description, enabled, priority) { }
        // Methods - Overrides *****************************************************************************************
        public override void render(Data data, ref StringBuilder text, RenderType renderTypes)
        {
            // Text formatting
            if ((renderTypes & RenderType.TextFormatting) == RenderType.TextFormatting)
            {
                // Face
                foreach (Match m in Regex.Matches(text.ToString(), @"\[font=([a-zA-Z\s]+)\](.*?)\[\/font\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<span style=\"font-family: " + m.Groups[1].Value + "\">" + m.Groups[2].Value + "</span>");
                // Size
                foreach (Match m in Regex.Matches(text.ToString(), @"\[size=([1-9]{1}|1[0-9]{1}|2[0-9]{1}|30{1}|[1-9]{1}.[1-9]{1}|1[0-9]{1}.[1-9]{1}|2[0-9]{1}.[1-9]{1})(em|pt)\](.*?)\[\/size\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<span style=\"font-size: " + m.Groups[1].Value + m.Groups[2].Value + "\">" + m.Groups[3].Value + "</span>");
                // Colour
                foreach (Match m in Regex.Matches(text.ToString(), @"\[(?:colour|color)=#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})\](.*?)\[\/(?:colour|color)\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<span style=\"color: #" + m.Groups[1].Value + "\">" + m.Groups[2].Value + "</span>");
                // Bold
                foreach (Match m in Regex.Matches(text.ToString(), @"\[b\](.*?)\[\/b\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<strong>" + m.Groups[1].Value + "</strong>");
                // Italics
                foreach (Match m in Regex.Matches(text.ToString(), @"\[i\](.*?)\[\/i\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<em>" + m.Groups[1].Value + "</em>");
                // Underline
                foreach (Match m in Regex.Matches(text.ToString(), @"\[u\](.*?)\[\/u\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<u>" + m.Groups[1].Value + "</u>");
                // Cross-through
                foreach (Match m in Regex.Matches(text.ToString(), @"\[s\](.*?)\[\/s\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<del>" + m.Groups[1].Value + "</del>");
                // High-lighting
                foreach (Match m in Regex.Matches(text.ToString(), @"\[highlight=#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})\](.*?)\[\/highlight\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<span style=\"background: #" + m.Groups[1].Value + "\">" + m.Groups[2].Value + "</span>");
                // Text shadow
                foreach (Match m in Regex.Matches(text.ToString(), @"\[shadow=([0-9]{1}|[0-9]{1}.[0-9]{1}|[0-9]{2}|[0-9]{2}.[0-9]{1}),([0-9]{1}|[0-9]{1}.[0-9]{1}|[0-9]{2}|[0-9]{2}.[0-9]{1}),([0-9]{1}|[0-9]{1}.[0-9]{1}|[0-9]{2}|[0-9]{2}.[0-9]{1}),#([a-fA-F0-9]{3}|[a-fA-F0-9]{6})\](.*?)\[\/shadow\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<span style=\"text-shadow: " + m.Groups[1].Value + "em " + m.Groups[2].Value + "em " + m.Groups[3].Value + "em #" + m.Groups[4].Value + ";\">" + m.Groups[5].Value + "</span>");
            }
            // Objects/layout
            if ((renderTypes & RenderType.Objects) == RenderType.Objects)
            {
                // Text align left
                foreach (Match m in Regex.Matches(text.ToString(), @"\[left\](.*?)\[\/left\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<div class=\"tal\">" + m.Groups[1].Value + "</div>");
                // Text align right
                foreach (Match m in Regex.Matches(text.ToString(), @"\[right\](.*?)\[\/right\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<div class=\"tar\">" + m.Groups[1].Value + "</div>");
                // Text align center
                foreach (Match m in Regex.Matches(text.ToString(), @"\[center\](.*?)\[\/center\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<div class=\"tac\">" + m.Groups[1].Value + "</div>");
                // Hyper-link
                foreach (Match m in Regex.Matches(text.ToString(), @"\[url=([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-\=\?]+)\](.*?)\[\/url\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<a href=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\">" + m.Groups[3].Value + "</a>");
                foreach (Match m in Regex.Matches(text.ToString(), @"\[url\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-\=\?]+)\[\/url\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<a href=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\">" + m.Groups[1].Value + "://" + m.Groups[2].Value + "</a>");
                // E-mail hyper-link
                foreach (Match m in Regex.Matches(text.ToString(), @"\[email\]([a-zA-Z0-9\.@_\-]+)\[\/email\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<a href=\"mailto://" + m.Groups[1].Value + "\">" + m.Groups[1].Value + "</a>");
                // Lists
                lists(ref text);
                // Float left
                foreach (Match m in Regex.Matches(text.ToString(), @"\[fl\](.*?)\[\/fl\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<div class=\"fl\">" + m.Groups[1].Value + "</div>");
                // Float right
                foreach (Match m in Regex.Matches(text.ToString(), @"\[fr\](.*?)\[\/fr\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<div class=\"fr\">" + m.Groups[1].Value + "</div>");
                // Padding/margin
                foreach (Match m in Regex.Matches(text.ToString(), @"\[(padding|margin)=([0-9]{1}|[0-9]{2}|[0-9]{1}.[0-9]{1}|[0-9]{2}.[0-9]{1})\](.*?)\[\/\1]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<div style=\"padding: " + m.Groups[2].Value + "em\">" + m.Groups[3].Value + "</div>");
                // Blockquote
                blockquote(ref text);
                // Image
                foreach (Match m in Regex.Matches(text.ToString(), @"\[img\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-\+]+)\[\/img\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<a title=\"Click to open the image...\" href=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\"><img src=\"" + m.Groups[1].Value + "://" + m.Groups[2].Value + "\" /></a>");
                foreach (Match m in Regex.Matches(text.ToString(), @"\[img=([0-9]{4}px|[0-9]{3}px|[0-9]{2}px|[0-9]{1}px|[0-9]{4}em|[0-9]{3}em|[0-9]{2}em|[0-9]{1}em)\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-\+]+)\[\/img\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<a title=\"Click to open the image...\" href=\"" + m.Groups[2].Value + "://" + m.Groups[3].Value + "\"><img style=\"width: " + m.Groups[1].Value + "; height: " + m.Groups[1].Value + ";\" src=\"" + m.Groups[2].Value + "://" + m.Groups[3].Value + "\" /></a>");
                foreach (Match m in Regex.Matches(text.ToString(), @"\[img=([0-9]{4}px|[0-9]{3}px|[0-9]{2}px|[0-9]{1}px|[0-9]{4}em|[0-9]{3}em|[0-9]{2}em|[0-9]{1}em),([0-9]{4}px|[0-9]{3}px|[0-9]{2}px|[0-9]{1}px|[0-9]{4}em|[0-9]{3}em|[0-9]{2}em|[0-9]{1}em)\]([a-zA-Z0-9]+)\:\/\/([a-zA-Z0-9\/\._\-\+]+)\[\/img\]", RegexOptions.Multiline))
                    text.Replace(m.Value, "<a title=\"Click to open the image...\" href=\"" + m.Groups[3].Value + "://" + m.Groups[4].Value + "\"><img style=\"width: " + m.Groups[1].Value + "; height: " + m.Groups[2].Value + ";\" src=\"" + m.Groups[3].Value + "://" + m.Groups[4].Value + "\" /></a>");
            }
        }
        private static void blockquote(ref StringBuilder text)
        {
            string stext;
            foreach (Match m in Regex.Matches(text.ToString(), @"\[blockquote\](.*?)\[\/blockquote\]", RegexOptions.Multiline))
            {
                stext = m.Groups[2].Value;
                if (stext.StartsWith("<br />") && stext.Length > 6)
                    stext = stext.Substring(6); // Remove starting <br />
                if (stext.EndsWith("<br />") && stext.Length > 6)
                    stext = stext.Remove(stext.Length - 6, 6); // Remove ending <br />
                text.Replace(m.Value, "<blockquote>" + stext + "</blockquote>");
            }
        }
        private static void lists(ref StringBuilder text)
        {
            StringBuilder list;
            foreach (Match m in Regex.Matches(text.ToString(), @"\[(nlist|list)\](.*?)\[\/(\1)\]", RegexOptions.Multiline))
            {
                list = new StringBuilder();
                int currentTree = 1, parsedTree, treeDiff;
                bool lineIsBp;
                char c;
                // Get the tag of the list
                string tag = m.Groups[1].Value == "nlist" ? "ol" : "ul";
                // Render elements
                foreach (string line in m.Groups[2].Value.Split(new string[] { "\n", "<br />" }, StringSplitOptions.None))
                {
                    lineIsBp = true;
                    // Check if the line is a bullet point, if so get the index
                    parsedTree = 0;
                    if (line.Length > 0 && line[0] == '[')
                    {
                        while (lineIsBp && parsedTree < line.Length - 1)
                        {
                            c = line[parsedTree + 1];
                            if (c == '*')
                                parsedTree++;
                            else if (c == ']' && parsedTree > 0)
                                break; // Valid bullet-pointer
                            else
                                lineIsBp = false; // Invalid bullet-point; invalid char or no *
                        }
                        // Check the line isn't just e.g. [***** or [****]
                        if (lineIsBp && parsedTree >= line.Length - 1)
                            lineIsBp = false;
                    }
                    else
                        lineIsBp = false;
                    // If the line is not a bp, append the line - else handle tree change, adding element etc
                    if (lineIsBp)
                    {
                        treeDiff = parsedTree - currentTree;
                        if (treeDiff > 0)
                        {
                            // We've gone in a level/levels - add the required tags
                            while (--treeDiff >= 0)
                            {
                                list.Append("<").Append(tag).Append(">");
                            }
                            currentTree = parsedTree;
                        }
                        else if (treeDiff < 0)
                        {
                            // Append closing item tag
                            list.Append("</li>");
                            // We've came out of a level/levels
                            while (++treeDiff <= 0)
                            {
                                list.Append("</").Append(tag).Append(">");
                            }
                            currentTree = parsedTree;
                        }
                        else
                            // Close the last element for the new element
                            list.Append("</li>");

                        // Append element
                        list.Append("<li>").Append(line.Substring(parsedTree + 2)); // 2 due to [ ] chars

                    }
                    else if (list.Length > 0) // Protection against empty lists
                        list.Append("<br />").Append(line);
                }
                if (list.Length > 0)
                {
                    // Append starting tag
                    list.Insert(0, "<" + tag + ">");
                    // Close the list
                    for (int i = 0; i < currentTree; i++)
                        list.Append("</").Append(tag).Append(">");
                    // Check for tailing <br />
                    if (list[list.Length - 6] == '<' && list[list.Length - 5] == 'b' && list[list.Length - 4] == 'r' && list[list.Length - 3] == ' ' && list[list.Length - 2] == '/' && list[list.Length - 1] == '>')
                        list.Remove(list.Length - 6, 6);
                    // Add closing tag
                    list.Append("</li>");
                    // Replace source text
                    text.Replace(m.Value, list.ToString());
                }
            }
        }
    }
}