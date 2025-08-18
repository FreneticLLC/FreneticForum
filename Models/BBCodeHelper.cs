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
using FreneticUtilities.FreneticExtensions;

namespace FreneticForum.Models
{
    public class BBCodeHelper
    {
        public static List<BBCode> GetDefaultBBCodes()
        {
            List<BBCode> list =
            [
                new BBCode("b", "<b>", "</b>", "Bold Text"),
                new BBCode("i", "<i>", "</i>", "Italic Text"),
                new BBCode("hr", "<hr>", "", "Horizontal Line"),
                new BBCode("e", "<span class=\"emphasis\">", "</span>", "Emphasized Text"),
                new BBCode("c", "<code>", "</code>", "In-Line Code Block", "", true),
                new BBCode("s", "<span class=\"strike\">", "</span>", "Strike-Through Text"),
                new BBCode("u", "<span class=\"underline\">", "</span>", "Underlined Text"),
                new BBCode("*", ForumUtilities.BULLET.ToString(), "", "List Entry"),
                new BBCode("size", "<span style=\"font-size:{{VALUE}}px;\">", "</span>", "Resized Text", "VALUE{7-60}") { Low = 7, High = 60 },
                new BBCode("url", "<a href=\"{{VALUE}}\">", "</a>", "Web Link", "http(s)://LINK_HERE") { Validator = "^https?://.*$" },
                new BBCode("code", "<pre>", "</pre>", "Code Block", "", true) { NoPrecedingNewline = true },
            ];
            return list;
        }

        public static List<BBCode> BBCodesKnown = null;

        public static List<BBCode> ActualBBCodes()
        {
            // TODO: write to and read from database.
            return BBCodesKnown ??= GetDefaultBBCodes();
        }

        public static HtmlString BBCodeInfo()
        {
            List<BBCode> codes = ActualBBCodes();
            StringBuilder res = new();
            for (int i = 0; i < codes.Count; i++)
            {
                res.Append("<div class=\"blockify\"><code>[" + codes[i].BBC
                + (string.IsNullOrWhiteSpace(codes[i].ValueHelp) ? "" : "=" + codes[i].ValueHelp)
                + "]" + codes[i].Help + "[/" + codes[i].BBC + "]</code></div>");
                if (i + 1 < codes.Count)
                {
                    res.Append(", ");
                }
                else
                {
                    res.Append('.');
                }
            }
            return new HtmlString(res.ToString());
        }

        private static string BBC_Internal(List<BBCode> codes, string input, int depth = 0)
        {
            if (depth > 500)
            {
                return "(BBCode ignored, too deep...)";
            }
            StringBuilder res = new();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '[')
                {
                    StringBuilder tag = new();
                    int t = i + 1;
                    while (t < input.Length)
                    {
                        if (input[t] == ']')
                        {
                            break;
                        }
                        tag.Append(input[t++]);
                    }
                    string tag_str = tag.ToString();
                    int ind = tag_str.IndexOf('=');
                    string tag_id = tag_str[..(ind < 0 ? tag_str.Length : ind)].ToLowerFast();
                    string tag_value = ind < 0 ? "" : tag_str[(ind + 1)..];
                    t++;
                    StringBuilder content = new();
                    int selfs = 1;
                    bool gottem = false;
                    while (t < input.Length)
                    {
                        if (input[t] == '[')
                        {
                            StringBuilder subTag = new();
                            t++;
                            while (t < input.Length)
                            {
                                if (input[t] == ']')
                                {
                                    break;
                                }
                                subTag.Append(input[t++]);
                            }
                            string subtag_str = subTag.ToString();
                            //int subind = tag_str.IndexOf('=');
                            string subtag_id = subtag_str;
                            if (subtag_id == tag_id)
                            {
                                selfs++;
                            }
                            else if (subtag_id == "/" + tag_id)
                            {
                                selfs--;
                            }
                            if (selfs == 0)
                            {
                                string innerData = content.ToString();
                                foreach (BBCode code in codes)
                                {
                                    if (code.BBC == tag_id)
                                    {
                                        if (!code.Plainify)
                                        {
                                            innerData = BBC_Internal(codes, innerData, depth + 1);
                                        }
                                        else
                                        {
                                            innerData = innerData.Replace("\n", "<{NOVERT}>");
                                        }
                                        string repl = "";
                                        if (code.Low != -1 || code.High != -1)
                                        {
                                            if (!int.TryParse(tag_value, out int outp))
                                            {
                                                break;
                                            }
                                            if (outp < code.Low || outp > code.High)
                                            {
                                                outp = code.Low;
                                            }
                                            repl = outp.ToString();
                                        }
                                        else if (code.Validate != null)
                                        {
                                            if (!code.Validate.IsMatch(tag_value))
                                            {
                                                break;
                                            }
                                            repl = tag_value;
                                        }
                                        innerData = code.HTMLPrefix.Replace("{{VALUE}}", repl) + innerData + code.HTMLSuffix.Replace("{{VALUE}}", repl);
                                        break;
                                    }
                                }
                                res.Append(innerData);
                                i = t;
                                gottem = true;
                                break;
                            }
                            else
                            {
                                content.Append("[" + subtag_str);
                            }
                        }
                        content.Append(input[t++]);
                    }
                    if (!gottem)
                    {
                        res.Append('[');
                    }
                    continue;
                }
                res.Append(input[i]);
            }
            return res.ToString();
            
        }

        public static HtmlString ParseBBode(string input)
        {
            input = input.Replace("&", "&amp;").Replace(">", "&gt;").Replace("<", "&lt;");
            List<BBCode> codes = ActualBBCodes();
            try
            {
                string outp = BBC_Internal(codes, input).Replace("\r", "");
                while (outp.Contains("\n\n"))
                {
                    outp = outp.Replace("\n\n", "\n");
                }
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].NoPrecedingNewline)
                    {
                        while (outp.Contains("\n" + codes[i].HTMLPrefix))
                        {
                            outp = outp.Replace("\n" + codes[i].HTMLPrefix, codes[i].HTMLPrefix);
                        }
                    }
                }
                return new HtmlString(outp.Replace("\n", "\n<br>").Replace("<{NOVERT}>", "\n"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new HtmlString("Bad BBCode!");
            }
        }
    }

    public class BBCode(string _bbc, string _htmlpre, string _htmlsuf, string _help, string vhelp = "", bool _plainify = false)
    {
        public string BBC = _bbc;

        public string HTMLPrefix = _htmlpre;

        public string HTMLSuffix = _htmlsuf;

        public string Help = _help;

        public string ValueHelp = vhelp;

        public bool Plainify = _plainify;

        public int Low = -1;

        public int High = -1;

        public Regex Validate = null;

        public bool NoPrecedingNewline = false;

        public string Validator
        {
            set
            {
                Validate = new Regex(value, RegexOptions.Compiled);
            }
        }
    }
}
