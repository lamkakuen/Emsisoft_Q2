using System;

namespace Q2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to exit...");
            var hashProcessor = new HashProcessor();
            hashProcessor.ProcessHashes();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
