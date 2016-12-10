using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FreneticForum.Models;

namespace FreneticForum.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            ViewData["init"] = new ForumInit(Request, Response);
            ViewData["is_post"] = Request.Method.ToUpperInvariant() == "POST";
            return View();
        }

        public IActionResult Index()
        {
            ViewData["init"] = new ForumInit(Request, Response);
            return View();
        }
    }
}
