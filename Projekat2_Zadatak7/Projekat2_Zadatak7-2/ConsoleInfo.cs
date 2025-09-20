using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat2_Zadatak7
{
    class ConsoleInfo
    {
        private static readonly object _lockObj = new object();
        public static void Server(string msg)
        {
            lock (_lockObj)
            {
                Console.WriteLine($"[SERVER] {msg}");
            }
        }
        public static void Print(String msg)
        {
            lock (_lockObj)
            {
                Console.WriteLine(msg);
            }
        }
        public static void Greska(String msg)
        {
            lock (_lockObj)
            {
                Console.WriteLine($"[GRESKA] {msg}");
            }
        }
        public static void Greska(String msg,Exception ex)
        {
            lock (_lockObj)
            {
                Console.WriteLine($"[GRESKA] {msg}: {ex.Message}");
            }
        }
        public static void Log(String msg,String nazivFajla)
        {
            lock (_lockObj)
            {
                Console.WriteLine("---------------------------------------------------------------------------------");
                Console.WriteLine($"[LOG] {msg}: {nazivFajla} | Vreme: {DateTime.Now}");
            }
        }
        public static void Info(String msg,String nazivFajla)
        {
            lock (_lockObj)
            {
                Console.WriteLine($"[INFO] {msg}: {nazivFajla}");
            }
        }

    }
}
