using System;
using System.Numerics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var isHardwareAccelerated = Vector.IsHardwareAccelerated;
            Console.WriteLine(isHardwareAccelerated.ToString());
        }
    }
}
