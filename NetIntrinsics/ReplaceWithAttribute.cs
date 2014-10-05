using System;
using System.Globalization;

namespace NetIntrinsics
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ReplaceWithAttribute : Attribute
    {
        public readonly string Code;

        public int Cpiud1EDX, Cpiud1ECX;
        public int Cpiud7EBX, Cpiud7ECX;
        public int Cpiud80000001EDX, Cpiud80000001ECX;

        public ReplaceWithAttribute(string code)
        {
            Code = code;
        }

        internal ulong GetCodeAsULong()
        {
            var code = Code.Replace(" ", "");
            if (code.Length != 10)
                throw new Exception("Replacement code should contain exactly 5 bytes, got: " + Code);
            var result = 0uL;
            for (var i = 0; i < 5; i++)
                result |= ulong.Parse(code.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) << (i * 8);
            return result;
        }

        internal bool CheckFeatures(ReplaceWithAttribute features)
        {
            return
                (features.Cpiud1EDX & Cpiud1EDX) == Cpiud1EDX &&
                (features.Cpiud1ECX & Cpiud1ECX) == Cpiud1ECX &&
                (features.Cpiud7EBX & Cpiud7EBX) == Cpiud7EBX &&
                (features.Cpiud7ECX & Cpiud7ECX) == Cpiud7ECX &&
                (features.Cpiud80000001EDX & Cpiud80000001EDX) == Cpiud80000001EDX &&
                (features.Cpiud80000001ECX & Cpiud80000001ECX) == Cpiud80000001ECX;
        }
    }
}
