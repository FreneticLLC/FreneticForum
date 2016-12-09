using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TestForum.Models;

namespace TestForum.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult ErrorInternal()
        {
            ViewData["init"] = new ForumInit(Request);
            return View();
        }

        public IActionResult Error404()
        {
            ViewData["init"] = new ForumInit(Request);
            return View();
        }

        public IActionResult Index()
        {
            // Somewhat special case: Redirect to install page if not installed!
            try
            {
                ViewData["init"] = new ForumInit(Request);
                return View();
            }
            catch (InitFailedException)
            {
                Response.Redirect("/Home/Install");
                return new EmptyResult();
            }
        }

        public IActionResult Install()
        {
            // SUPER SPECIAL CASE: Install page is only valid IF regular pages ARE NOT!
            try
            {
                ForumInit init = new ForumInit(Request);
                Response.Redirect("/");
                return new EmptyResult();
            }
            catch (InitFailedException)
            {
                // Do Nothing
            }
            bool isPost = Request.Method.ToUpperInvariant() == "POST";
            ViewData["is_post"] = isPost;
            if (isPost)
            {
                ViewData["tf_form_admin_pw"] = Request.Form["admin_password"].ToString();
                ViewData["tf_form_title"] = Request.Form["forum_name"].ToString();
                ViewData["tf_form_dburl"] = Request.Form["forum_dburl"].ToString();
                ViewData["tf_form_dbname"] = Request.Form["forum_dbname"].ToString();
            }
            ViewData["response"] = Response;
            return View();
        }
    }
}
