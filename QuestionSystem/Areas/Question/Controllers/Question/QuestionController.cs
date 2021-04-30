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

        private static string[] coordinateConjunction = { "а", "но", "однако", "а то", "не то", "а не то", "либо" };

        private static char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

        /// <summary>
        /// вовзращает вопрос к ПРОСТОМУ предложению, 
        /// так как сложные предложения состоят из набора простых, 
        /// то необходимо вызвать несколько раз эту функцию для сложного предоженя
        /// </summary>
        /// <param name="morph"></param>
        /// <param name="words"></param>
        /// <returns>string Вопрос к ПРОСТОМУ предложению</returns>
        private string processSimpleSentence(MorphAnalyzer morph, string sourceSentence)
        {
            //разбиение предложения на слова
            string[] words = sourceSentence.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

            int dash = Array.FindIndex(words, d => d.Equals("-"));

            // > 0 чтобы не первой позицией
            // пусть пока будет по самому факту наличия тире, потом надо добавить еще проверки, на частицы и формы слов
            if (dash > 0 && (dash + 1 <= words.Length - 1))
            {
                //string res = string.Join(" ", words, 0, dash) +" " + string.Join(" ", words, dash + 1, words.Length - 1 - dash);//это ответ
                ///TODO Вынести "Что такое" в языковые константы
                string res = "Что такое " + string.Join(" ", words, 0, dash) + " ?";
                return res;
            }

            var morphResult = morph.Parse(words).ToArray();
            string wordCase = string.Empty;

            short subject = 0; // подлежащее
            short predicate = 0;// сказуемое
                                /// 0 -  вопросительное слово
                                /// 1 - глагол
                                /// 2 - существительное
                                
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
                    if (subject == 1 && predicate != 0 && String.IsNullOrEmpty(wordCase))
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

            foreach (string word in result)
            {
                if (word == "")
                {
                    return string.Empty;
                }
            }

            return string.Join(" ", result) + '?';
        }

        /// <summary>
        /// Подготавливает сложносочиненное предложение к разделению, заменяет сочинительные союзы на тег [REMOVE], дальше по этому тегу идет разбиение на части 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string prepareSentenceForCoordinateConjunction(string source)
        {
            string[] words = source.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

            int wordIndex = 0;
            foreach (string word in words)
            {
                int index = Array.FindIndex(words, d => d.Equals(Array.Find(coordinateConjunction, x => x.Equals(words[wordIndex]))));

                if (wordIndex == index)
                {
                    words[index] = "[REMOVE]";
                }
                wordIndex++;
            }

            return string.Join(" ", words);
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

            //string[] words1 = sourceSentence.Split(subordinatingConjunctions, StringSplitOptions.RemoveEmptyEntries);//разбиение по подчинительным союзам

            //разбиение по сочинительным союзам и работа с каждой частью, как с независимым предложением

            string coordinateSentence = prepareSentenceForCoordinateConjunction(sourceSentence);
            if (coordinateSentence.Contains("[REMOVE]"))
            {
                string[] coordinateSentences = coordinateSentence.Split("[REMOVE]", StringSplitOptions.RemoveEmptyEntries);

                if (coordinateSentences.Length >= 2)
                {
                    string result = string.Empty;//пока что несколько вопросов к одному предложению будут возвращаться как одна строка

                    foreach (string coordSentence in coordinateSentences)
                    {
                        result += processSimpleSentence(morph, coordSentence);
                    }

                    if (result.Length != 0)
                    {
                        return result;
                    }
                    else
                    {
                        return "";
                    }
                }
            }

            return processSimpleSentence(morph, sourceSentence);
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
            Dictionary<int, GenQuestion> offers = splittingText(text);

            foreach (KeyValuePair<int, GenQuestion> sug in offers)
            {
                string question = generateQuestion(morph, sug.Value.Sentence);
                if (question.Length != 0)
                {
                    sug.Value.QuestionText = question;
                }
            }

            ViewData["GenQuestion"] = offers;
            ViewBag.Title = "Вывод вопросов";
            return View("ViewGenQuestion");
        }

        public Dictionary<int, GenQuestion> splittingText(String text)
        {
            string[] massSuggestions = Regex.Split(text, @"(?<=[\.!\?])\s+");

            Dictionary<int, GenQuestion> offers = new Dictionary<int, GenQuestion>();
            char[] stopChar = new char[] { '*', '\r','\n' };

            int index = 1;
            foreach (string suggestion in massSuggestions)
            {
                bool isNormalSentence = true;
                string[] words = suggestion.Split(' ');

                if (words.Length <= 3) isNormalSentence = false;
                if (words[words.Length - 1].Contains("?")) isNormalSentence = false;
                if (words[words.Length - 1].Contains("!")) isNormalSentence = false;

                if (isNormalSentence)
                {
                    //Удаление стоп-символов
                    string buf = "";
                    foreach (char c in stopChar)
                    {
                        buf = suggestion.Replace(c.ToString(), "");
                    }

                    GenQuestion q = new GenQuestion
                    {
                        Sentence = buf
                    };

                    offers[index] = q;
                }

                index++;
            }

            return offers;

        }
    }
}