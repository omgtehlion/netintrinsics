using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PerfTest
{
    static class Program
    {
        const int runs = 100000000;
        const int testDataLen = 1 << 14;
        const int testDataMask = testDataLen - 1;
        static ulong xorshift128plus_s0 = 123456789;
        static ulong xorshift128plus_s1 = ~1234567890uL;
        static ulong[] defaultData = Enumerable.Range(0, testDataLen).Select(_ => Xorshift128plus()).ToArray();
        static ulong[] sparseData = Enumerable.Range(0, testDataLen).Select(_ => GetSparseBits()).ToArray();

        static void Main(string[] args)
        {
            // warmup
            Console.WriteLine("Patch status: " + Intrinsic.Status, Intrinsic.PopCnt(1L), Intrinsic.BitScanForward(1L));

            TestRun("Rng", sw => {
                var sum = 0uL;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += defaultData[i & testDataMask];
                return (int)sum;
            });

            TestRun("BitScanForward", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += Intrinsic.BitScanForward((long)defaultData[i & testDataMask]);
                return sum;
            });
            TestRun("FallBack.BitScanForward", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += FallBack.BitScanForward((long)defaultData[i & testDataMask]);
                return sum;
            });

            TestRun("PopCnt64", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += Intrinsic.PopCnt(defaultData[i & testDataMask]);
                return sum;
            });
            TestRun("FallBack.PopCnt64", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += FallBack.PopCnt(defaultData[i & testDataMask]);
                return sum;
            });

            TestRun("PopCnt32", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += Intrinsic.PopCnt((uint)defaultData[i & testDataMask]);
                return sum;
            });
            TestRun("FallBack.PopCnt32", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += FallBack.PopCnt((uint)defaultData[i & testDataMask]);
                return sum;
            });

            TestRun("PopCnt64 on sparse data", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += Intrinsic.PopCnt(sparseData[i & testDataMask]);
                return sum;
            });
            TestRun("FallBack.PopCnt64 on sparse data", sw => {
                var sum = 0;
                sw.Start();
                for (var i = 0; i < runs; i++)
                    sum += FallBack.PopCnt(sparseData[i & testDataMask]);
                return sum;
            });

            Console.WriteLine("done.");
            Console.ReadKey();
        }

        static void TestRun(string message, Func<Stopwatch, int> func)
        {
            //reset random
            xorshift128plus_s0 = 123456789;
            xorshift128plus_s1 = ~1234567890uL;

            Console.WriteLine("Testing " + message + "...");
            var sw = new Stopwatch();
            var sum = func(sw);
            sw.Stop();
            Console.WriteLine("{0}ms, sum={1}", sw.ElapsedMilliseconds, sum);
            Console.WriteLine();
        }

        static ulong GetSparseBits()
        {
            var result = 0uL;
            for (var i = 0; i < 8; i++) {
                result |= 1uL << (int)(Xorshift128plus() & 63);
            }
            return result;
        }

        static ulong Xorshift128plus()
        {
            var s1 = xorshift128plus_s0;
            var s0 = xorshift128plus_s1;
            xorshift128plus_s0 = s0;
            s1 ^= s1 << 23;
            return (xorshift128plus_s1 = (s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26))) + s0;
        }
    }
}
