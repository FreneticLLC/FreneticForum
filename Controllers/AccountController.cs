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
        public IActionResult Index()
        {
            ViewData["init"] = new ForumInit(Request);
            return View();
        }
    }
}
