namespace PerfTest
{
    public static class FallBack
    {
        public static int PopCnt(long value)
        {
            return PopCnt((ulong)value);
        }

        public static int PopCnt(ulong value)
        {
            var count = 0;
            for (; value != 0; count++)
                value &= value - 1uL;
            return count;
        }

        public static int PopCnt(int value)
        {
            return PopCnt((uint)value);
        }

        public static int PopCnt(uint value)
        {
            var count = 0;
            for (; value != 0; count++)
                value &= value - 1u;
            return count;
        }

        static readonly int[] bsIndex64 = new[] {
             0, 47,  1, 56, 48, 27,  2, 60,
            57, 49, 41, 37, 28, 16,  3, 61,
            54, 58, 35, 52, 50, 42, 21, 44,
            38, 32, 29, 23, 17, 11,  4, 62,
            46, 55, 26, 59, 40, 36, 15, 53,
            34, 51, 20, 43, 31, 22, 10, 45,
            25, 39, 14, 33, 19, 30,  9, 24,
            13, 18,  8, 12,  7,  6,  5, 63
        };

        public static int BitScanForward(long value)
        {
            const ulong debruijn64 = 0x03F79D71B4CB0A89uL;
            return bsIndex64[((ulong)(value ^ (value - 1)) * debruijn64) >> 58];
        }

        public static int BitScanReverse(long value)
        {
            const ulong debruijn64 = 0x03F79D71B4CB0A89uL;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return bsIndex64[((ulong)value * debruijn64) >> 58];
        }
    }
}
