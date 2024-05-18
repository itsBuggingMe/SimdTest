using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Numerics;

namespace SimdTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var m = new SimdBenchmark();

            Console.WriteLine(Vector.IsHardwareAccelerated ? "HA: Yes!" : "HA: No!");
            Console.WriteLine(Vector<float>.IsSupported ? "Supported!" : "Not Supported!");
            Console.WriteLine(Vector<float>.Count);

            var sum = BenchmarkRunner.Run<SimdBenchmark>();
        }
    }
}
