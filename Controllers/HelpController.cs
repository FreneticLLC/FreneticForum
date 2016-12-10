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
            ViewData["init"] = new ForumInit(Request, Response);
            return View();
        }

        public IActionResult BBCodeTester()
        {
            ViewData["is_post"] = Request.Method.ToUpperInvariant() == "POST";
            ViewData["init"] = new ForumInit(Request, Response);
            return View();
        }
    }
}
