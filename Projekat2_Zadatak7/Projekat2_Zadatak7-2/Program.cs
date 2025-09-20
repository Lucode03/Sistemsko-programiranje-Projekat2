using System;
using System.IO;

namespace Projekat2_Zadatak7
{
    class Program
    {
        static async Task Main()
        {
            /*ThreadPool.GetMinThreads(out int worker, out int io);
            Console.WriteLine($"Min Threads: Worker={worker}, IO={io}");

            ThreadPool.GetMaxThreads(out worker, out io);
            Console.WriteLine($"Max Threads: Worker={worker}, IO={io}");*/

            //TestiranjeKesa.Testiraj();

            //ThreadPool.SetMinThreads(10, 5);

            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            string urlPrefix = "http://localhost:5050/";

            Console.WriteLine($"Root folder: {rootFolder}");
            Console.WriteLine("Podrzani tipovi slika: .png, .jpg, .jpeg, .gif, .bmp, .svg, .webp");

            Server server = new Server(rootFolder, urlPrefix);
            server.StartAsync();

            Console.WriteLine("Pritisnite ENTER kako bi ste zaustavili server...");
            Console.ReadLine();

            await server.StopAsync();
        }
    }
}