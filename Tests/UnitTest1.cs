using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestPopCnt()
        {
            TestPopCnt32(0, 0);
            TestPopCnt64(0, 0);

            TestPopCnt32(-1, 32);
            TestPopCnt64(-1, 64);

            for (var i = 0; i < 32; i++)
                TestPopCnt32(1 << i, 1);
            for (var i = 0; i < 64; i++)
                TestPopCnt64(1L << i, 1);

            for (var i = 1; i < 32; i++)
                TestPopCnt32(1 | (1 << i), 2);
            for (var i = 1; i < 64; i++)
                TestPopCnt64(1L | (1L << i), 2);

            var test32 = new[]{
                Tuple.Create(-1982907, 21),
                Tuple.Create(26879365, 11),
                Tuple.Create(27991186, 12),
                Tuple.Create(86976929, 13),
            };
            foreach (var t in test32) {
                TestPopCnt32(t.Item1, t.Item2);
                TestPopCnt64((long)(uint)t.Item1, t.Item2);
                TestPopCnt64(((long)(uint)t.Item1) << 32, t.Item2);
            }
        }

        static void TestPopCnt64(long p1, int p2)
        {
            Assert.AreEqual(p2, Intrinsic.PopCnt(p1));
        }

        static void TestPopCnt32(int p1, int p2)
        {
            Assert.AreEqual(p2, Intrinsic.PopCnt(p1));
        }

        [TestMethod]
        public void TestBsf()
        {
            for (var i = 0; i < 64; i++)
                Assert.AreEqual(i, Intrinsic.BitScanForward(1L << i));

            for (var i = 1; i < 64; i++)
                Assert.AreEqual(0, Intrinsic.BitScanForward(1L | (1L << i)));

            for (var i = 0; i < 63; i++)
                Assert.AreEqual(i, Intrinsic.BitScanForward((1L << 63) | (1L << i)));
        }

        [TestMethod]
        public void TestBsr()
        {
            for (var i = 0; i < 64; i++)
                Assert.AreEqual(i, Intrinsic.BitScanReverse(1L << i));

            for (var i = 1; i < 64; i++)
                Assert.AreEqual(i, Intrinsic.BitScanReverse(1L | (1L << i)));

            for (var i = 0; i < 63; i++)
                Assert.AreEqual(63, Intrinsic.BitScanReverse((1L << 63) | (1L << i)));
        }

        [TestMethod]
        public void TestPatchStatus()
        {
            if (IntPtr.Size != 8) {
                Assert.Inconclusive("Not patched: please choose X64 as default architecture in Test Settings");
            } else {
                if (!Intrinsic.Status.StartsWith("Ok")) {
                    Assert.Fail(Intrinsic.Status);
                } else {
                    var extraStatus = Intrinsic.Status.Substring(3).Trim();
                    if (extraStatus.Length > 0) {
                        Assert.Inconclusive(extraStatus);
                    }
                }
            }
        }
    }
}
