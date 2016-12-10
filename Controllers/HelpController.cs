using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FreneticForum.Models;

namespace FreneticForum.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult Index()
        {
            ViewData["init"] = new ForumInit(Request);
            return View();
        }

        public IActionResult BBCodeTester()
        {
            bool isPost = Request.Method.ToUpperInvariant() == "POST";
            ViewData["is_post"] = isPost;
            ViewData["init"] = new ForumInit(Request);
            return View();
        }
    }
}
