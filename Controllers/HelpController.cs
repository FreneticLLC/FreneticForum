using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FreneticForum.Models;
using FreneticUtilities.FreneticExtensions;

namespace FreneticForum.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult Index()
        {
            try
            {
                ViewData["init"] = new ForumInit(Request, Response);
                return View();
            }
            catch (NoProcessException)
            {
                return new EmptyResult();
            }
        }

        public IActionResult BBCodeTester()
        {
            try
            {
                ViewData["is_post"] = Request.Method.ToUpperFast() == "POST";
                ViewData["init"] = new ForumInit(Request, Response);
                return View();
            }
            catch (NoProcessException)
            {
                return new EmptyResult();
            }
        }
    }
}
