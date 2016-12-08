using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TestForum.Models;

namespace TestForum.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            ForumInit init = new ForumInit(Request);
            ViewData["init"] = init;
            return View();
        }
    }
}
