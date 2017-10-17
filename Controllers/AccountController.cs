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
            try
            {
                ViewData["init"] = new ForumInit(Request, Response);
                ViewData["is_post"] = Request.Method.ToUpperInvariant() == "POST";
                return View();
            }
            catch (NoProcessException)
            {
                return new EmptyResult();
            }
        }

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

        public IActionResult GetTFA()
        {
            try
            {
                ViewData["init"] = new ForumInit(Request, Response);
                ViewData["is_post"] = Request.Method.ToUpperInvariant() == "POST";
                return View();
            }
            catch (NoProcessException)
            {
                return new EmptyResult();
            }
        }

        public IActionResult API()
        {
            try
            {
                ForumInit finit = new ForumInit(Request, Response);
                if (Request.Method.ToUpperInvariant() != "POST")
                {
                    return Content("ERROR=NOT_POST;");
                }
                String qtype = Request.Form["qtype"];
                if (qtype == null)
                {
                    return Content("ERROR=NO_QTYPE;");
                }
                qtype = qtype.ToLowerInvariant();
                // Log in to an account
                if (qtype == "login")
                {
                    // Username, Password, TFA
                    return Content("ERROR=NOT_IMPLEMENTED;");
                }
                // Log out of an account
                else if (qtype == "logout")
                {
                    // Username, Session
                    return Content("ERROR=NOT_IMPLEMENTED;");
                }
                // Gather a one-use key to log in to a server.
                else if (qtype == "one_use_key")
                {
                    // Username, Session, KeyTypeID
                    return Content("ERROR=NOT_IMPLEMENTED;");
                }
                // A server wants to check a one-use key for validity.
                else if (qtype == "check_key")
                {
                    // Username, Key, KeyTypeID
                    return Content("ERROR=NOT_IMPLEMENTED;");
                }
                // Gather a bit of information on a user's public profile data, if available.
                else if (qtype == "mini_profile")
                {
                    // Username
                    return Content("ERROR=NOT_IMPLEMENTED;");
                }
                else
                {
                    return Content("ERROR=UNKNOWN;");
                }
            }
            catch (NoProcessException)
            {
                return new EmptyResult();
            }
        }
    }
}
