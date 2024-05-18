using BenchmarkDotNet.Attributes;
using SimpleSimd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SimdTest.SimdBenchmark;

namespace SimdTest
{
    public class SimdBenchmark
    {
        private readonly float[] left;
        private readonly float[] right;
        private readonly float[] output;

        public SimdBenchmark()
        {
            const int len = 8 * 100_000;
            left = new float[len];
            right = new float[len];
            output = new float[len];
            Random r = new Random(0);
            for(int i = 0; i < left.Length; i++)
            {
                left[i] = r.Next(10);
                right[i] = r.Next(10);
            }
        }

        #region Naive
        [Benchmark(Baseline = true)]
        public void NaiveSimple()
        {
            for(int i = 0; i < left.Length; i++)
                output[i] = left[i] * right[i];
        }

        [Benchmark]
        public void NaiveOptimised()
        {
            var (l,r,o) = (left, right, output);
            for (int i = 0; i < l.Length; i++)
                o[i] = l[i] * r[i];
        }

        [Benchmark]
        public void NaiveSpan()
        {
            ReadOnlySpan<float> l = left.AsSpan();
            ReadOnlySpan<float> r = right.AsSpan();
            Span<float> o = output.AsSpan();
            for (int i = 0; i < l.Length; i++)
                o[i] = l[i] * r[i];
        }
        #endregion

        #region Unsafe
        [Benchmark]
        public unsafe void UnsafeArrPtr()
        {
            fixed(float* l = left, r = right, o = output)
            {
                int len = output.Length;
                for (int i = 0; i < len; i++)
                    o[i] = l[i] * r[i];
            }
        }

        [Benchmark]
        public unsafe void UnsafeElementPtr()
        {
            fixed (float* pl = &left[0], pr = &right[0], po = &output[0])
            {
                var o = po; var l = pl; var r = pr;
                int len = output.Length;
                for (int i = 0; i < len; i++, l++, r++, o++)
                    *o = *l * *r;
            }
        }
        #endregion

        #region SIMD
        [Benchmark]
        public void SimdSimple()
        {
            ReadOnlySpan<float> l = left.AsSpan();
            ReadOnlySpan<float> r = right.AsSpan();
            Span<float> o = output.AsSpan();
            int len = l.Length;

            for (int i = 0; i < len; i += Vector<float>.Count)
                (new Vector<float>(l[i..]) * new Vector<float>(r[i..])).CopyTo(o[i..]);
        }

        [Benchmark]
        public void SimdCast()
        {
            var leftVec = MemoryMarshal.Cast<float, Vector<float>>(left);
            var rightVec = MemoryMarshal.Cast<float, Vector<float>>(right);
            var outputVec = MemoryMarshal.Cast<float, Vector<float>>(output);
            int len = rightVec.Length;
            for(int i = 0; i < len; i++)
                outputVec[i] = leftVec[i] * rightVec[i];
        }

        [Benchmark]
        public unsafe void SimdPtrInc()
        {
            fixed(float* lp = left, rp = right, op = output)
            {
                Vector<float>* l = (Vector<float>*)lp;
                Vector<float>* r = (Vector<float>*)rp;
                Vector<float>* o = (Vector<float>*)op;
                int len = right.Length;
                for(int i = 0; i < len; i += Vector<float>.Count, o++, r++, l++)
                {
                    *o = *l * *r;
                }
            }
        }

        [Benchmark]
        public unsafe void SimdPtrIncSimd()
        {
            fixed (float* lp = left, rp = right, op = output)
            {
                InlineVars inlinePtr = new(lp, rp, op);
                int len = right.Length;
                var v = InlineVars.IncrementVector;
                while (inlinePtr.Ptr.Counter < len)
                {
                    *inlinePtr.Ptr.Output = *inlinePtr.Ptr.Left * *inlinePtr.Ptr.Right;
                    inlinePtr.Vector += v;//test struct ac speed
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe ref struct InlineVars
        {
            [FieldOffset(0)]
            public InlinePtr Ptr;
            [FieldOffset(0)]
            public Vector<nint> Vector;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public InlineVars(float* l, float* r, float* o) => Ptr = new InlinePtr((Vector<float>*)l, (Vector<float>*)r, (Vector<float>*)o);

            public static Vector<nint> IncrementVector = new Vector<nint>(sizeof(Vector<float>)).WithElement(0, Vector<float>.Count);

            internal unsafe ref struct InlinePtr
            {
                public nint Counter;
                public Vector<float>* Left;
                public Vector<float>* Right;
                public Vector<float>* Output;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public InlinePtr(Vector<float>* l, Vector<float>* r, Vector<float>* o)
                {
                    Counter = 0;
                    Left = l;
                    Right = r;
                    Output = o;
                }
            }
        }
        #endregion

        [Benchmark]
        public unsafe void Lib()
        {
            SimdOps.Multiply(left.AsSpan(), right.AsSpan(), output.AsSpan());
        }

        public SimdBenchmark Validate(Action? t = null)
        {
            Array.Fill(output, 0);
            t?.Invoke();
            for(int i = 0; i < right.Length; i++)
                if (right[i] * left[i] != output[i])
                    throw new Exception("Failed");
            return this;
        }
    }
}
