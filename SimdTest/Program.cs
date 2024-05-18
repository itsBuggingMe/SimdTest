using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace SimdTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var m = new SimdBenchmark();

            var sum = BenchmarkRunner.Run<SimdBenchmark>();
        }
    }
}
