﻿using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SimdTest
{
    public class SimdBenchmark
    {
        private readonly float[] left;
        private readonly float[] right;
        private readonly float[] output;
        public const int len = 8 * 100_000;

        public SimdBenchmark()
        {
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
        public void SimdRef()
        {
            ref var leftPtr = ref MemoryMarshal.GetReference<float>(left);
            ref var rightPtr = ref MemoryMarshal.GetReference<float>(right);
            ref var outPtr = ref MemoryMarshal.GetReference<float>(output);
            int len = right.Length;

            for (int i = 0; i < len; i += Vector256<float>.Count)
            {
                ref Vector<float> l1 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref leftPtr, i));
                ref Vector<float> r1 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref rightPtr, i));
                ref Vector<float> o1 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref outPtr, i));
                o1 = r1 * l1;
            }
        }

        [Benchmark]
        public void SimdRefUnroll()
        {
            ref var leftPtr = ref MemoryMarshal.GetReference<float>(left);
            ref var rightPtr = ref MemoryMarshal.GetReference<float>(right);
            ref var outPtr = ref MemoryMarshal.GetReference<float>(output);
            int len = right.Length;

            for (int i = 0; i < len; i += Vector256<float>.Count * 4)
            {
                ref Vector<float> l1 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref leftPtr, i));
                ref Vector<float> r1 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref rightPtr, i));
                ref Vector<float> o1 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref outPtr, i));

                ref Vector<float> l2 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref leftPtr, i + Vector<float>.Count));
                ref Vector<float> r2 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref rightPtr, i + Vector<float>.Count));
                ref Vector<float> o2 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref outPtr, i + Vector<float>.Count));

                ref Vector<float> l3 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref leftPtr, i + 2 * Vector<float>.Count));
                ref Vector<float> r3 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref rightPtr, i + 2 * Vector<float>.Count));
                ref Vector<float> o3 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref outPtr, i + 2 * Vector<float>.Count));

                ref Vector<float> l4 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref leftPtr, i + 3 * Vector<float>.Count));
                ref Vector<float> r4 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref rightPtr, i + 3 * Vector<float>.Count));
                ref Vector<float> o4 = ref Unsafe.As<float, Vector<float>>(ref Unsafe.Add(ref outPtr, i + 3 * Vector<float>.Count));
                o1 = r1 * l1;
                o2 = r2 * l2;
                o3 = r3 * l3;
                o4 = r4 * l4;
            }
        }

        [Benchmark]
        public unsafe void SimdPtrInc()
        {
            fixed(float* lp = left, rp = right, op = output)
            {
                Vector<float>* l = (Vector<float>*)lp;
                Vector<float>* r = (Vector<float>*)rp;
                Vector<float>* o = (Vector<float>*)op;
                nint len = right.Length;
                nint i = 0;

                while(i < len)
                {
                    *o = *l * *r;

                    i += Vector<float>.Count;
                    o++;
                    r++;
                    l++;
                }
            }
        }

        [Benchmark]
        public unsafe void SimdPtrIncUnroll()
        {
            fixed (float* lp = left, rp = right, op = output)
            {
                Vector<float>* l = (Vector<float>*)lp;
                Vector<float>* r = (Vector<float>*)rp;
                Vector<float>* o = (Vector<float>*)op;
                nint len = right.Length;
                nint i = 0;

                while (i < len)
                {
                    *o = *l * *r;
                    o++; r++; l++;
                    *o = *l * *r;
                    o++; r++; l++;
                    *o = *l * *r;
                    o++; r++; l++;
                    *o = *l * *r;
                    o++; r++; l++;

                    i += Vector<byte>.Count;
                }
            }
        }
        [Benchmark]
        public unsafe void SimdPtrIncUnrollIndex()
        {
            fixed (float* lp = left, rp = right, op = output)
            {
                Vector<float>* l = (Vector<float>*)lp;
                Vector<float>* r = (Vector<float>*)rp;
                Vector<float>* o = (Vector<float>*)op;
                nint len = right.Length >> 3;
                nint i = 0;

                while (i < len)
                {
                    o[i] = l[i] * r[i];
                    o[i + 1] = l[i + 1] * r[i + 1];
                    o[i + 2] = l[i + 2] * r[i + 2];
                    o[i + 3] = l[i + 3] * r[i + 3];

                    i++;
                }
            }
        }

        [Benchmark]
        public unsafe void SimdPtrIncSimd()
        {
            fixed (float* lp = left, rp = right, op = output)
            {
                InlineVars inlinePtr = new()
                {
                    Ptr = new()
                    {
                        Counter = 0,
                        Left = (Vector<float>*)lp,
                        Right = (Vector<float>*)rp,
                        Output = (Vector<float>*)op,
                    }
                };

                var v = InlineVars.IncrementVector;
                for (nint l = len; inlinePtr.Ptr.Counter < l; inlinePtr.Vector += v)
                {
                    *inlinePtr.Ptr.Output = *inlinePtr.Ptr.Left * *inlinePtr.Ptr.Right;
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

            public static Vector<nint> IncrementVector = new Vector<nint>(new nint[] { 
                Vector<float>.Count, 
                sizeof(Vector<float>), 
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
                sizeof(Vector<float>),
            });

            internal unsafe ref struct InlinePtr
            {
                public nint Counter;
                public Vector<float>* Left;
                public Vector<float>* Right;
                public Vector<float>* Output;
            }
        }
        #endregion

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
