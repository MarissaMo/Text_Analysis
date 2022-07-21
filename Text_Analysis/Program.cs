using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace CountTripleText
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int numtr = 6; //количество потоков
            string text;
            StreamReader sr;
            string filename;
            Console.WriteLine("Введите путь к файлу");
            while (true)
            {
                filename = Console.ReadLine();
                if(File.Exists(filename))
                {
                    sr = new StreamReader(filename);
                    break;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Вы ввели неверный путь к файлу, попробуйте еще раз");
                }
            }

            Console.WriteLine();


            int[] flp = new int[numtr];//для хранения границ текста для каждого потока
            TriplTextAnalyser[] triplTextCount = new TriplTextAnalyser[numtr];//Для каждого потока используем отдельный экземпляр класса TriplTextAnalyser
            Dictionary<ulong, int[]> dtr = new Dictionary<ulong, int[]>(); //словарь для объединения словарей со всех потоков

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            text = sr.ReadToEnd();
            sr.Close();

            //определяем границы текста для каждого потока. Делим текст примерно на одинаковые части для каждого потока
            int tlen = text.Length;
            int pos;
            for (int i = 0; i < numtr - 1; i++)
            {
                pos = text.IndexOf(" ", tlen / numtr * (i + 1));
                if (pos > 0)
                {
                    flp[i] = pos;
                }
                else
                {
                    flp[i] = tlen - 1;
                }
            }
            flp[numtr - 1] = tlen - 1;

            //запускаем потоки. результат по каждому потоку сохраняем в собственном словаре
            for (int i = 0; i < numtr; i++)
            {
                if (i == 0)
                {
                    triplTextCount[i] = new TriplTextAnalyser(text, 0, flp[0]);
                }
                else
                {
                    triplTextCount[i] = new TriplTextAnalyser(text, flp[i - 1], flp[i]);
                }
                if (i == numtr - 1)
                {
                    triplTextCount[i].TriplCount();
                }
                else
                {
                    triplTextCount[i].StartThreadForTriplCount();
                }
            }

            //Ожидаем завершения потоков
            for (int i = 0; i < numtr - 1; i++)
            {
                triplTextCount[i].Thrd.Join();
            }

            //объединяем словари из каждого потока
            for (int i = 0; i < numtr; i++)
            {
                foreach (var eldic in triplTextCount[i].dtr)
                {
                    if (i != 0 && dtr.TryGetValue(eldic.Key, out int[] icont))
                    {
                        icont[0] += eldic.Value[0];
                    }
                    else
                    {
                        dtr.Add(eldic.Key, new int[] { eldic.Value[0] });
                    }
                }
            }

            //Печатаем результат
            var sortedDict = dtr.OrderByDescending(entry => entry.Value[0]).Take(10).ToDictionary(pair => pair.Key, pair => pair.Value[0]);
            string triplp;
            foreach (var el in sortedDict)
            {
                triplp = "" + (char)int.Parse(el.Key.ToString().Substring(1, 5)) + (char)int.Parse(el.Key.ToString().Substring(6, 5)) + (char)int.Parse(el.Key.ToString().Substring(11, 5));
                Console.WriteLine($"Трипл '{triplp}' встречается {el.Value} раз");
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00} часов, {1:00} минут, {2:00} секунд, {3:000} миллисекунд",
                        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            Console.WriteLine("Время выполнения программы: " + elapsedTime);

            Console.ReadLine();

        }
    }

    class TriplTextAnalyser
    {
        public Dictionary<ulong, int[]> dtr = new Dictionary<ulong, int[]>();
        public Thread Thrd;

        private string source;
        private int bp;
        private int lp;

        public TriplTextAnalyser(string source, int bp, int lp)
        {
            this.source = source;
            this.bp = bp;
            this.lp = lp;
        }

        public void StartThreadForTriplCount()
        {
            Thrd = new Thread(this.TriplCount);
            Thrd.Start();
        }

        public void TriplCount()
        {
            ulong ulkey;

            for (int i = bp; i <= lp - 2; i++)
            {
                if (char.IsLetter(source[i]) && char.IsLetter(source[i + 1]) && char.IsLetter(source[i + 2]))
                {
                    ulkey = (((ulong)source[i] + 100000) * 100000 + (ulong)source[i + 1]) * 100000 + (ulong)source[i + 2];

                    if (dtr.TryGetValue(ulkey, out int[] icont))
                    {
                        icont[0] += 1;
                    }
                    else
                    {
                        dtr.Add(ulkey, new int[] { 1 });
                    }

                }
            }

        }
    }
}
