using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

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
            list.Add(new BBCode("[c]{{TEXT:1}}[/c]", "<code>{{1}}</code>", "[c]Code Snippet[/c}"));
            list.Add(new BBCode("[s]{{TEXT:1}}[/s]", "<span class=\"strike\">{{1}}</span>", "[s]Striked-Through Text[/s]"));
            list.Add(new BBCode("[u]{{TEXT:1}}[/u]", "<span class=\"underline\">{{1}}</span>", "[u]Underline Text[/u]"));
            list.Add(new BBCode("[size={{INTEGER(7,60):1}}]{{TEXT:2}}[/size]", "<span style=\"font-size:{{1}};\">{{2}}</span>", "[size=25]Resized Text[/size]"));
            return list;
        }
    }

    public class BBCode
    {
        public string BBC;

        public string HTML;

        public string Help;

        public BBCode(string _bbc, string _html, string _help)
        {
            BBC = _bbc;
            HTML = _html;
            Help = _help;
        }
    }
}
