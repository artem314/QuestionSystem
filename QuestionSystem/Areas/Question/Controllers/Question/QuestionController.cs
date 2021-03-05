using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace QuestionSystem.Areas.Question.Controllers.Question
{
    public class QuestionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public string TestM()
        {
            return "asd";
        }
    }
}