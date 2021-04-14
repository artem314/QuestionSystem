using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DeepMorphy;
using Microsoft.AspNetCore.Mvc;

namespace QuestionSystem.Areas.Question.Controllers.Question
{
    [Area("question")]
    public class QuestionController : Controller
    {
        public IActionResult Index()
        {
            return View("Index");
        }

        //[HttpPost]
        public IActionResult Generate()
        {
            Dictionary<string, string> CaseToquestion = new Dictionary<string, string>();
            CaseToquestion.Add("им", "Что?"); //на самом деле его быть не должно, но программа не отличит винительный от именительного, а вопросы одинаковые, так что все ок
            CaseToquestion.Add("рд", "Чего?");
            CaseToquestion.Add("дт", "Чему?");
            CaseToquestion.Add("вн", "Что?");
            CaseToquestion.Add("тв", "Чем?");
            CaseToquestion.Add("пр", "О чем?");

            string text = "Программа для ЭВМ представляет собой описание алгоритма и данных на некотором языке программирования";
            var morph = new MorphAnalyzer();
            char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

            //разбиение текста на предложения
            string[] sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");

            foreach (string sentence in sentences)
            {
                short subject = 0; // подлежащее
                short predicate = 0;// сказуемое
                /// 0 -  вопросительное слово
                /// 1 - глагол
                /// 2 - существительное
                /// 
                List<string> result = new List<string>(3);
                result.Add("");
                result.Add("");
                result.Add("");
                result.Add("");
                //разбиение предложения на слова
                string[] words = sentence.Split(delimiterChars);

                var morphResult = morph.Parse(words).ToArray();
                string wordCase = string.Empty;


                foreach (DeepMorphy.Model.MorphInfo morphInfo in morphResult)
                {
                    
                    if (morphInfo.BestTag.Has("сущ"))
                    {
                       
                        if (subject == 0)
                        {
                            wordCase = morphInfo.BestTag["падеж"];
                            subject++;
                            result[2] = morphInfo.Text;
                        }

                        //следующее после глагола существительное
                        if (subject == 1 && predicate != 0 && !String.IsNullOrEmpty(wordCase))
                        {
                            result[0]= CaseToquestion[wordCase];
                        }

                    }

                    if (morphInfo.BestTag.Has("гл") && predicate < 1)
                    {
                        result[1] = morphInfo.Text;

                        predicate++;
                        //string wordCase = morphInfo.BestTag["падеж"];
                    }
                }

                var res = result.ToString();


            }


            return Content("123");
        }
    }
}