using NetIntrinsics;
using System.Runtime.CompilerServices;

// TODO:
// _byteswap_uint64, _byteswap_ulong, _byteswap_ushort: http://msdn.microsoft.com/en-us/library/a3140177.aspx

namespace System
{
    public static class Intrinsic
    {
        public static readonly string Status;

        static Intrinsic()
        {
            Status = Patcher.PatchClass(typeof(Intrinsic));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int PopCnt(long value)
        {
            return PopCnt((ulong)value);
        }

        [ReplaceWith("F3 48 0F B8 C1" /* popcnt rax, rcx */, Cpiud1ECX = 1 << 23)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int PopCnt(ulong value)
        {
            var count = 0;
            for (; value != 0; count++)
                value &= value - 1uL;
            return count;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int PopCnt(int value)
        {
            return PopCnt((uint)value);
        }

        [ReplaceWith("F3 0F B8 C1 90" /* popcnt eax, ecx | nop */, Cpiud1ECX = 1 << 23)]
        [MethodImpl(MethodImplOptions.NoInlining)]
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

        /// <summary>Finds position of the least significant bit set</summary>
        /// <param name="value">bitboard, != 0</param>
        [ReplaceWith("48 0F BC C1 90" /* bsf rax, rcx | nop */)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BitScanForward(long value)
        {
            // see: http://chessprogramming.wikispaces.com/BitScan
            /**
             * bitScanForward
             * @author Kim Walisch (2012)
             * @param bb bitboard to scan
             * @precondition bb != 0
             * @return index (0..63) of least significant one bit
             */
            //assert(bb != 0);
            const ulong debruijn64 = 0x03F79D71B4CB0A89uL;
            return bsIndex64[((ulong)(value ^ (value - 1)) * debruijn64) >> 58];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BitScanForward(ulong value)
        {
            return BitScanForward((long)value);
        }

        /// <summary>Finds position of the most significant bit set</summary>
        /// <param name="value">bitboard, != 0</param>
        [ReplaceWith("48 0F BD C1 90" /* bsr rax, rcx | nop */)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BitScanReverse(long value)
        {
            /**
             * bitScanReverse
             * @authors Kim Walisch, Mark Dickinson
             * @param bb bitboard to scan
             * @precondition bb != 0
             * @return index (0..63) of most significant one bit
             */
            //assert (value != 0);
            const ulong debruijn64 = 0x03F79D71B4CB0A89uL;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return bsIndex64[((ulong)value * debruijn64) >> 58];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BitScanReverse(ulong value)
        {
            return BitScanReverse((long)value);
        }

        [ReplaceWith("48 91 48 0F C8" /* xchg rax, rcx | bswap rax */)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ulong BSwap(ulong value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }
    }
}
