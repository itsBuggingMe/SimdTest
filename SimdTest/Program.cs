using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;

namespace SimdTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var m = new SimdBenchmark();

            Console.WriteLine(Stopwatch.IsHighResolution);

            var sum = BenchmarkRunner.Run<SimdBenchmark>();

            foreach(var methodInfo in typeof(SimdBenchmark).GetMethods().Where(m => m.GetCustomAttribute<BenchmarkAttribute>() != null))
            {
                RunTest(methodInfo.ToString(), s => methodInfo.Invoke(s, null));
            }
        }

        public static void RunTest(string name, Action<SimdBenchmark> method)
        {
            const int trials = 1000;


            var m = new SimdBenchmark();
            var sw = Stopwatch.StartNew();

            for(int i = 0; i < trials; i++)
                method(m);

            sw.Stop();
            Console.WriteLine($"{name}: {sw.ElapsedTicks / trials}");
        }
    }
}
