using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DeepMorphy;
using Microsoft.AspNetCore.Mvc;
using QuestionSystem.Models;

namespace QuestionSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var morph = new MorphAnalyzer();
            var results = morph.Parse(new string[]
{
    "королёвские",
    "тысячу",
    "миллионных",
    "красотка",
    "1-ый"
}).ToArray();

            var morphInfo = results[0];
            ViewData["morphInfo"] = morphInfo;
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
