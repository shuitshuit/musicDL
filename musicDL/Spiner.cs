using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace musicDL
{
    public class Spiner
    {
        public static void Spin(string startMsg)
        {
            Console.CursorVisible = false;
            char[] bars = ['/', '-', '|'];

            foreach (var item in bars)
            {
                Console.Write($"{startMsg} {item}");
                Console.SetCursorPosition(0, Console.CursorTop);
                Thread.Sleep(120);
            }
            Console.CursorVisible = true;
        }
    }
}
