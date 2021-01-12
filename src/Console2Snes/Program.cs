using System;
using System.Threading.Tasks;
using Dotnet2Snes;

namespace Console2Snes
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Out.WriteLine("Welcome.");
            Task.Run(MainAsync).Wait();
            Console.In.ReadLine();
        }

        static async void MainAsync()
        {
            Console.Out.WriteLine("In Main Async");
            Dotnet2Snes.Dotnet2Snes connection = new Dotnet2Snes.Dotnet2Snes(8080);
            string version = await connection.Attach("COM3");
            Console.Out.WriteLine(version);
            Console.In.ReadLine();
        }
    }
}
