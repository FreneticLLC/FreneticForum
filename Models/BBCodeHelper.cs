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
            list.Add(new BBCode("b", "<b>", "</b>", "Bold Text"));
            list.Add(new BBCode("i", "<i>", "</i>", "Italic Text"));
            list.Add(new BBCode("e", "<span class=\"emphasis\">", "</span>", "Emphasized Text"));
            list.Add(new BBCode("c", "<code>", "</code>", "Code Block", true));
            list.Add(new BBCode("s", "<span class=\"strike\">", "</span>", "Strike-Through Text"));
            list.Add(new BBCode("u", "<span class=\"underline\">", "</span>", "Underlined Text"));
            list.Add(new BBCode("size", "<span style=\"font-size:{{VALUE}}px;\">", "</span>", "Resized Text (value from 7 to 60)") { Low = 7, High = 60 });
            return list;
        }

        public static List<BBCode> BBCodesKnown = null; // TODO: Dictionary?

        public static List<BBCode> ActualBBCodes()
        {
            // TODO: write to and read from database.
            return BBCodesKnown ?? (BBCodesKnown = GetDefaultBBCodes());
        }

        private static string BBC_Internal(List<BBCode> codes, string input, int depth = 0)
        {
            if (depth > 500)
            {
                return "(BBCode ignored, too deep...)";
            }
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '[')
                {
                    StringBuilder tag = new StringBuilder();
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
                    string tag_id = tag_str.Substring(0, ind < 0 ? tag_str.Length : ind).ToLowerInvariant();
                    string tag_value = ind < 0 ? "" : tag_str.Substring(ind + 1);
                    t++;
                    StringBuilder content = new StringBuilder();
                    int selfs = 1;
                    bool gottem = false;
                    while (t < input.Length)
                    {
                        if (input[t] == '[')
                        {
                            StringBuilder subTag = new StringBuilder();
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
                            int subind = tag_str.IndexOf('=');
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
                                Console.WriteLine("Tag " + tag_id + " with innards: " + innerData);
                                foreach (BBCode code in codes)
                                {
                                    if (code.BBC == tag_id)
                                    {
                                        if (!code.Plainify)
                                        {
                                            innerData = BBC_Internal(codes, innerData, depth + 1);
                                        }
                                        string repl = "";
                                        if (code.Low != -1 || code.High != -1)
                                        {
                                            int outp;
                                            if (!int.TryParse(tag_value, out outp))
                                            {
                                                break;
                                            }
                                            if (outp < code.Low || outp > code.High)
                                            {
                                                outp = code.Low;
                                            }
                                            repl = outp.ToString();
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
                        res.Append("[");
                    }
                    continue;
                }
                res.Append(input[i]);
            }
            return res.ToString().Replace("\n", "\n<br>");
            
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

    public class BBCode
    {
        public string BBC;

        public string HTMLPrefix;

        public string HTMLSuffix;

        public string Help;

        public bool Plainify;

        public int Low = -1;

        public int High = -1;

        public BBCode(string _bbc, string _htmlpre, string _htmlsuf, string _help, bool _plainify = false)
        {
            BBC = _bbc;
            HTMLPrefix = _htmlpre;
            HTMLSuffix = _htmlsuf;
            Help = _help;
            Plainify = _plainify;
        }
    }
}
