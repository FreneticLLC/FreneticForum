using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Html;

namespace FreneticForum.Models
{
    public class BBCodeHelper
    {
        public static List<BBCode> GetDefaultBBCodes()
        {
            List<BBCode> list = new List<BBCode>();
            list.Add(new BBCode("[b]{{TEXT:1}}[/b]", "<b>{{1}}</b>", "[b]Bold Text[/b]"));
            list.Add(new BBCode("[i]{{TEXT:1}}[/i]", "<i>{{1}}</i>", "[i]Italic Text[/i]"));
            list.Add(new BBCode("[e]{{TEXT:1}}[/e]", "<span class=\"emphasis\">{{1}}</span>", "[e]Emphasized Text[/e]"));
            list.Add(new BBCode("[c]{{PLAIN_TEXT:1}}[/c]", "<code>{{1}}</code>", "[c]Code Snippet[/c}"));
            list.Add(new BBCode("[s]{{TEXT:1}}[/s]", "<span class=\"strike\">{{1}}</span>", "[s]Striked-Through Text[/s]"));
            list.Add(new BBCode("[u]{{TEXT:1}}[/u]", "<span class=\"underline\">{{1}}</span>", "[u]Underline Text[/u]"));
            list.Add(new BBCode("[size={{INTEGER(7,60):1}}]{{TEXT:2}}[/size]", "<span style=\"font-size:{{1}};\">{{2}}</span>", "[size=25]Resized Text[/size]"));
            return list;
        }

        public static List<BBCode> BBCodesKnown = null;

        public static List<BBCode> ActualBBCodes()
        {
            // TODO: write to and read from database.
            return BBCodesKnown ?? (BBCodesKnown = GetDefaultBBCodes());
        }

        private static string BBC_Internal(List<BBCode> codes, string input, int depth = 0)
        {
            // TODO: This probably needs a redesign.
            if (depth > 500)
            {
                return "(BBCode ignored, too deep...)";
            }
            int flb = input.IndexOf('[');
            int lrb = input.LastIndexOf(']');
            if (flb == -1 || lrb == -1)
            {
                return input;
            }
            string first = input.Substring(0, flb);
            string subst = input.Substring(flb, (lrb + 1) - flb);
            string last = input.Substring(lrb + 1);
            foreach (BBCode bbc in codes)
            {
                Match m = bbc.Tester.Match(subst);
                if (!m.Success)
                {
                    continue;
                }
                string before = input.Substring(0, m.Index);
                int aind = m.Index + m.Length + 1;
                string after = aind >= input.Length ? "" : input.Substring(aind);
                string res = bbc.HTML;
                foreach (KeyValuePair<string, BBCodeLimits> entry in bbc.AllLimits)
                {
                    string inner = m.Groups[entry.Key].ToString();
                    if (!entry.Value.Plainify)
                    {
                        inner = BBC_Internal(codes, input, depth + 1);
                    }
                    if (entry.Value.High != -1 && int.Parse(inner) > entry.Value.High)
                    {
                        inner = entry.Value.High.ToString();
                    }
                    if (entry.Value.Low != -1 && int.Parse(inner) < entry.Value.Low)
                    {
                        inner = entry.Value.Low.ToString();
                    }
                    inner = inner.Replace("{", "&#123;").Replace("}", "&#124;");
                    res = res.Replace("{{" + entry.Key + "}}", inner);
                }
                return first + res + last;
            }
            return input;
            
        }

        public static HtmlString ParseBBode(string input)
        {
            input = input.Replace("&", "&amp;").Replace(">", "&gt;").Replace("<", "&lt;");
            List<BBCode> codes = ActualBBCodes();
            try
            {
                return new HtmlString(BBC_Internal(codes, input));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new HtmlString("Bad BBCode!");
            }
        }
    }

    public class BBCodeLimits
    {
        public bool Plainify = false;

        public int Low = -1;

        public int High = -1;
    }

    public class BBCode
    {
        public static Regex RG_FINDER = new Regex("{{([A-Z]+)(\\((\\d+),(\\d+)\\))?:(\\d+)}}", RegexOptions.Compiled);

        public string BBC;

        public string HTML;

        public string Help;

        public Regex Tester;

        public Dictionary<string, BBCodeLimits> AllLimits = new Dictionary<string, BBCodeLimits>();

        public BBCode(string _bbc, string _html, string _help)
        {
            if (!_bbc.StartsWith("[") || !_bbc.EndsWith("]"))
            {
                throw new Exception("Invalid BBCode!");
            }
            BBC = _bbc;
            HTML = _html;
            Help = _help;
            // {{TEXT:1}} becomes (?<1>.*)
            StringBuilder sb = new StringBuilder();
            int start = 0;
            for (int i = 1; i < _bbc.Length; i++)
            {
                if (i <= start)
                {
                    continue;
                }
                string sub = _bbc.Substring(start, i - start);
                Match m = RG_FINDER.Match(sub);
                if (m.Success)
                {
                    sb.Append(Regex.Escape(sub.Substring(0, m.Index)));
                    start = i;
                    string id = m.Groups[5].ToString();
                    BBCodeLimits limits = new BBCodeLimits();
                    switch (m.Groups[1].ToString())
                    {
                        case "TEXT":
                            sb.Append("(?<" + id + ">[^\\[]*)");
                            break;
                        case "PLAIN_TEXT":
                            sb.Append("(?<" + id + ">^\\[]*)");
                            limits.Plainify = true;
                            break;
                        case "INTEGER":
                            int lowMax = -1;
                            int highMax = -1;
                            limits.Plainify = true;
                            if (m.Groups.Count > 3 && int.TryParse(m.Groups[3].ToString(), out lowMax) && int.TryParse(m.Groups[4].ToString(), out highMax))
                            {
                                limits.Low = lowMax;
                                limits.High = highMax;
                            }
                            sb.Append("(?<" + id + ">\\d+)");
                            break;
                        default:
                            throw new Exception("Invalid regex option: " + m.Groups[1]);
                    }
                    AllLimits[id] = limits;
                }
            }
            sb.Append(Regex.Escape(_bbc.Substring(start)));
            Tester = new Regex(sb.ToString(), RegexOptions.Compiled);
        }
    }
}
