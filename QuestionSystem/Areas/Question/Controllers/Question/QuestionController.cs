﻿using System;
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

        ///TODO вынести весь этот дегенаративный бред в отдельный класс

        ///TODO вынести в какой нибудь отдельный класс
        private static Dictionary<string, string> CaseToquestion = new Dictionary<string, string>()
        {
            { "им", "Что" },//на самом деле его быть не должно, но программа не отличит винительный от именительного, а вопросы одинаковые, так что все ок
            { "рд", "Чего"},
            { "дт", "Чему"},
            { "вн", "Что" },
            { "тв", "Чем"},
            { "пр", "О чем" }
        };

        private static string[] subordinatingConjunctions =
        { "потому что", "оттого что","так как", "в виду того что", "благодаря тому что","вследствие того что", "в связи с тем что",
            "чтобы", "для того чтобы", "с тем чтобы","несмотря на то что"
        };

        private static char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

        /// <summary>
        /// вовзращает вопрос к ПРОСТОМУ предложению, 
        /// так как сложные предложения состоят из набора простых, 
        /// то необходимо вызвать несколько раз эту функцию для сложного предоженя
        /// </summary>
        /// <param name="morph"></param>
        /// <param name="words"></param>
        /// <returns>string Вопрос к ПРОСТОМУ предложению</returns>
        private List<string> processSimpleSentence(MorphAnalyzer morph, string[] words)
        {

            var morphResult = morph.Parse(words).ToArray();
            string wordCase = string.Empty;

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

            int wordIndex = 0;
            foreach (DeepMorphy.Model.MorphInfo morphInfo in morphResult)
            {

                if (morphInfo.BestTag.Has("сущ"))
                {
                    //первое существительное - подлежащее
                    if (subject == 0)
                    {
                        subject++;
                        //проверка на словосочетание
                        ///TODO сделать проверку на выход за границы, но в теории она не нужна.
                        if (morphResult[wordIndex + 1].BestTag.Has("сущ"))
                        {
                            result[2] = morphInfo.Text + " " + morphResult[wordIndex + 1].Text;
                        }
                        else
                        {
                            result[2] = morphInfo.Text;
                        }
                    }

                    //следующее после глагола существительное
                    if (subject == 1 && predicate != 0 && !String.IsNullOrEmpty(wordCase))
                    {
                        wordCase = morphInfo.BestTag["падеж"];
                        result[0] = CaseToquestion[wordCase];
                    }

                }

                if (morphInfo.BestTag.Has("гл") && predicate < 1)
                {
                    result[1] = morphInfo.Text;
                    predicate++;
                }

                wordIndex++;
            }

            return result;
        }

        /// <summary>
        ///НЕОЖИДАННО Генерирует вопрос  к конкретному предложению
        /// </summary>
        /// <param name="morph">Обьект MorphAnalyzer</param>
        /// <param name="sourceSentence">Исходное предложение</param>
        /// <returns>string вопрос</returns>

        /// TODO вынести MorphAnalyzer в singleton
        private string generateQuestion(MorphAnalyzer morph, string sourceSentence)
        {

            //разбиение предложения на слова
            string[] words = sourceSentence.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

            int dash = Array.FindIndex(words, d => d.Equals("-"));

            // > 0 чтобы не первой позицией
            if (dash > 0)
            {
                //string res = string.Join(" ", words, 0, dash) +" " + string.Join(" ", words, dash + 1, words.Length - 1 - dash);//это ответ
                ///TODO Вынести "Что такое" в языковые константы
                string res = "Что такое " + string.Join(" ", words, 0, dash) + " ?";
                return res;
            }

            List<string> result = processSimpleSentence(morph, words);

            //string[] words1 = sourceSentence.Split(subordinatingConjunctions, StringSplitOptions.RemoveEmptyEntries);//разбиение по подчинительным союзам

            //TODO разбиение по сочинительным союзом и работа с каждой частью, как с не зависимым предложением

            foreach (string word in result)
            {
                if (word == "")
                {
                    return string.Empty;
                }
            }

            return string.Join(" ", result) + '?';

        }

        [HttpPost]
        public IActionResult Generate(string text)
        {
            //CaseToquestion.Add("им", "Что"); //на самом деле его быть не должно, но программа не отличит винительный от именительного, а вопросы одинаковые, так что все ок
            //CaseToquestion.Add("рд", "Чего");
            //CaseToquestion.Add("дт", "Чему");
            //CaseToquestion.Add("вн", "Что");
            //CaseToquestion.Add("тв", "Чем");
            //CaseToquestion.Add("пр", "О чем");

            var morph = new MorphAnalyzer();

            //Инициализация 1 - 4 шаг.
            Dictionary<int, GenQuestion> suggestions = splittingText(text);

            foreach (KeyValuePair<int, GenQuestion> sug in suggestions)
            {
                string question = generateQuestion(morph, sug.Value.Suggestion);
                if (question.Length != 0)
                {
                    sug.Value.QuestionText = question;
                }
            }


            // return Content(test);

            ViewData["GenQuestion"] = suggestions;
            ViewBag.Title = "Вывод вопросов";
            return View("ViewGenQuestion");
        }

        //TODO исправить неточность при разбитии на предложения, когда после точки нет пробела - считает как одно предложение
        public Dictionary<int, GenQuestion> splittingText(String text)
        {
            string[] massSuggestions = Regex.Split(text, @"(?<=[\.!\?])\s+");

            Dictionary<int, GenQuestion> suggestions = new Dictionary<int, GenQuestion>();
            char[] stopChar = new char[] { '*' };

            int index = 1;
            foreach (string suggestion in massSuggestions)
            {
                bool isNormalSuggestion = true;
                string[] words = suggestion.Split(' ');

                if (words.Length <= 3) isNormalSuggestion = false;
                if (words[words.Length - 1].Contains("?")) isNormalSuggestion = false;
                if (words[words.Length - 1].Contains("!")) isNormalSuggestion = false;


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