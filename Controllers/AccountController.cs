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

        public IActionResult APIv1()
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
                    // Username, Password, TFA, SType
                    // Return session
                    string username = Request.Form["username"];
                    string password = Request.Form["password"];
                    string tfa_code = Request.Form["tfa_code"];
                    string stype = Request.Form["SType"];
                    if (username == null || password == null || tfa_code == null || stype == null)
                    {
                        return Content("ERROR=BAD_INPUT;");
                    }
                    Account acc = finit.Database.GetAccount(username);
                    if (acc == null)
                    {
                        return Content("ERROR=ACCOUNT_MISSING;");
                    }
                    LoginResult res = acc.CanLogin(password, tfa_code);
                    if (res != LoginResult.ALLOWED)
                    {
                        return Content("ERROR=ACCOUNT_FAIL_CODE_" + res.ToString() + ";");
                    }
                    string genned = acc.GenerateSessMaster(stype);
                    if (genned == null)
                    {
                        return Content("ERROR=SESSION_GENERATION_FAILURE;");
                    }
                    return Content("ACCEPT=SESSION/" + genned + ",UID/" + acc.UserID + ";");
                }
                // Log out of an account
                else if (qtype == "logout")
                {
                    // Username, Session, SType
                    // Return always successs if Username and SType are valid, otherwise error (no error for wrong sesscode)
                    return Content("ERROR=NOT_IMPLEMENTED;");
                }
                // Gather a one-use key to log in to a server.
                else if (qtype == "one_use_key")
                {
                    // Username, Session, SType
                    // Return Key
                    string username = Request.Form["username"];
                    string sess_key = Request.Form["sess_key"];
                    string stype = Request.Form["SType"];
                    if (username == null || sess_key == null || stype == null)
                    {
                        return Content("ERROR=BAD_INPUT;");
                    }
                    Account acc = finit.Database.GetAccount(username);
                    if (acc == null)
                    {
                        return Content("ERROR=ACCOUNT_MISSING;");
                    }
                    if (!acc.TrySessMaster(stype, sess_key))
                    {
                        return Content("ERROR=BAD_SESSION;");
                    }
                    string key = acc.GenerateOneUseSess(stype);
                    if (key == null)
                    {
                        return Content("ERROR=KEY_GENERATION_FAILURE;");
                    }
                    return Content("ACCEPT=KEY/" + key + ";");
                }
                // A server wants to check a one-use key for validity.
                else if (qtype == "check_key")
                {
                    // Username, Key, SType
                    // Return boolean
                    string username = Request.Form["username"];
                    string ou_key = Request.Form["ou_key"];
                    string stype = Request.Form["SType"];
                    if (username == null || ou_key == null || stype == null)
                    {
                        return Content("ERROR=BAD_INPUT;");
                    }
                    Account acc = finit.Database.GetAccount(username);
                    if (acc == null)
                    {
                        return Content("ERROR=ACCOUNT_MISSING;");
                    }
                    if (!acc.CheckOneUseSess(stype, ou_key))
                    {
                        return Content("ERROR=BAD_KEY;");
                    }
                    if (!acc.CheckOneUseSess(stype, ou_key))
                    {
                        return Content("ERROR=BAD_KEY;");
                    }
                    // TODO: Check IP address somehow
                    return Content("ACCEPT=UID/" + acc.UserID + ";");
                }
                // Gather a bit of information on a user's public profile data, if available.
                else if (qtype == "mini_profile")
                {
                    // Username
                    // Return profile data
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
