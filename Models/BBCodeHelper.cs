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
        public static List<KeyValuePair<string, string>> GetDefaultBBCodes()
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            list.Add(new KeyValuePair<string, string>("[b]{{TEXT:1}}[/b]", "<b>{{1}}</b>"));
            list.Add(new KeyValuePair<string, string>("[i]{{TEXT:1}}[/i]", "<i>{{1}}</i>"));
            list.Add(new KeyValuePair<string, string>("[e]{{TEXT:1}}[/e]", "<span class=\"emphasis\">{{1}}</span>"));
            list.Add(new KeyValuePair<string, string>("[c]{{TEXT:1}}[/c]", "<code>{{1}}</code>"));
            return list;
        }
    }
}
