using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DeepMorphy;
using Microsoft.AspNetCore.Mvc;
using QuestionSystem.Areas.Question.Models;

namespace QuestionSystem.Areas.Question.Controllers.Question
{
    [Area("Question")]
    public class QuestionController : Controller
    {
        public IActionResult Index()
        {
            return View("Index");
        }

        //[HttpPost]
        public IActionResult Generate(string text)
        {
            Dictionary<string, string> CaseToquestion = new Dictionary<string, string>();
            CaseToquestion.Add("им", "Что?"); //на самом деле его быть не должно, но программа не отличит винительный от именительного, а вопросы одинаковые, так что все ок
            CaseToquestion.Add("рд", "Чего?");
            CaseToquestion.Add("дт", "Чему?");
            CaseToquestion.Add("вн", "Что?");
            CaseToquestion.Add("тв", "Чем?");
            CaseToquestion.Add("пр", "О чем?");

         //   /question/question

            //string text = "Программа для ЭВМ представляет собой описание алгоритма и данных на некотором языке программирования";
            var morph = new MorphAnalyzer();
            char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

            //Инициализация 1 - 4 шаг.
            Dictionary<int, GenQuestion> suggestions = splittingText(text);
            /*

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

            */

            string test = "";

            foreach (KeyValuePair<int, GenQuestion> sug in suggestions)
            {
                test += sug.Value.Suggestion + " ";
            }


            // return Content(test);

            ViewData["GenQuestion"] = suggestions;
            ViewBag.Title = "Вывод вопросов";
            return View("ViewGenQuestion");
        }

        public Dictionary<int, GenQuestion> splittingText(String text)
        {
            string[] massSuggestions = Regex.Split(text, @"(?<=[\.!\?])\s+");

            Dictionary<int, GenQuestion> suggestions =  new Dictionary<int, GenQuestion>();
            char[] stopChar = new char[] {'*'};

            int index = 1;
            foreach (string suggestion in massSuggestions)
            {
                bool isNormalSuggestion = true;
                string[] words = suggestion.Split(' ');

                if (words.Length <= 3) isNormalSuggestion = false;
                if(words[words.Length-1].Contains("?")) isNormalSuggestion = false;
                if(words[words.Length-1].Contains("!")) isNormalSuggestion = false;


                if (isNormalSuggestion)
                {
                    //Удаление стоп-символов
                    string buf = "";
                    foreach (char c in stopChar)
                    {
                        buf = suggestion.Replace(c.ToString(), "");
                    }

                    GenQuestion q = new GenQuestion
                    {
                        Suggestion = buf
                    };

                    suggestions[index] = q;
                }


                index++;
            }

            return suggestions;


        }
    }
}