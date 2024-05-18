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
        static unsafe void Main(string[] args)
        {
            var m = new SimdBenchmark();
            foreach (var methodInfo in typeof(SimdBenchmark).GetMethods().Where(m => m.GetCustomAttribute<BenchmarkAttribute>() != null))
                m.Validate(() => methodInfo.Invoke(m, null));
            
            var sum = BenchmarkRunner.Run<SimdBenchmark>();
            Console.ReadLine();
        }
    }
}
