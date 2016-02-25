using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetIntrinsics
{
    public static unsafe class Patcher
    {
        const BindingFlags DefaultBinding = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        const ulong Mask5Bytes = (1uL << 40) - 1;

        static readonly byte* PatchCallSitePtr;
        static readonly ReplaceWithAttribute CpuFeatures;
        public static string Error { get; private set; }
        public static bool ThrowOnPatchError = false;

        static Patcher()
        {
            if (IntPtr.Size != 8) {
                Error = "Not a 64 bit";
                return;
            }
            PatchCpuid(typeof(Patcher).GetMethod("Cpuid", DefaultBinding));
            if (Error != null) {
                return;
            }
            PatchCallSitePtr = GetMethodPtr(typeof(Patcher).GetMethod("PatchCallSite", DefaultBinding));
            fixed (int* cpuInfo = new int[4]) {
                CpuFeatures = new ReplaceWithAttribute("<unused>");
                Cpuid(1, cpuInfo);
                CpuFeatures.Cpiud1EDX = cpuInfo[3];
                CpuFeatures.Cpiud1ECX = cpuInfo[2];
                Cpuid(7, cpuInfo);
                CpuFeatures.Cpiud7EBX = cpuInfo[1];
                CpuFeatures.Cpiud7ECX = cpuInfo[2];
                Cpuid(0x80000001, cpuInfo);
                CpuFeatures.Cpiud80000001EDX = cpuInfo[3];
                CpuFeatures.Cpiud80000001ECX = cpuInfo[2];
            }
        }

        //EAX, EBX, ECX, and EDX registers (in that order)
        static void Cpuid(uint level, int* buffer)
        {
            // some code just to reserve space
            buffer[0] = buffer[3] + 1;
            buffer[1] = buffer[3] + 2;
            buffer[2] = buffer[3] + 3;
            buffer[3] = buffer[0] + 4;
            buffer[0] = buffer[3] + 5;
            buffer[1] = buffer[3] + 6;
            buffer[2] = buffer[3] + 7;
            buffer[3] = buffer[0] + 8;
        }

        static byte* GetMethodPtr(MethodInfo method)
        {
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            var ptr = (byte*)method.MethodHandle.GetFunctionPointer();
            // skip all jumps
            while (*ptr == 0xE9) {
                var delta = *(int*)(ptr + 1) + 5;
                ptr += delta;
            }
            return ptr;
        }

        static void PatchCallSite(byte* rsp, ulong code)
        {
            var ptr = rsp - 5;
            // check call opcode
            if (*ptr != 0xE8) {
                ptr += 5;
                ReportPatchError("Unexpected code before ret address 0x{0:X16}: ... {1}", ptr, ptr - 16);
                return;
            }
            // check call target
            var calltarget = ptr + *(int*)(ptr + 1) + 5;
            while (*calltarget == 0xE9) {
                var delta = *(int*)(calltarget + 1) + 5;
                calltarget += delta;
            }
            code &= Mask5Bytes;
            if ((*(ulong*)calltarget & Mask5Bytes) != code) {
                ReportPatchError("Unexpected code at call target 0x{0:X16}: {1} ...", calltarget, calltarget);
                return;
            }
            *(ulong*)ptr = *(ulong*)ptr & ~Mask5Bytes | code;
        }

        static void ReportPatchError(string msg, byte* ptr, byte* printAt)
        {
            if (ThrowOnPatchError)
                throw new Exception(string.Format(msg, (ulong)ptr, PrintBytes(printAt, 16)));
        }

        static string PrintBytes(byte* ptr, int count)
        {
            var sb = new StringBuilder(count * 3);
            for (var i = 0; i < count; i++) {
                sb.Append((*(ptr + i)).ToString("X2"));
                sb.Append(' ');
            }
            sb.Length--;
            return sb.ToString();
        }

        static void PatchCpuid(MethodInfo method)
        {
            var ptr = GetMethodPtr(method);
            if (!VerifyPrologue(ptr)) {
                Error = string.Format("Unexpected prologue: {0} ...", PrintBytes(ptr, 16));
                return;
            }
            foreach (var b in new byte[] {
                    // see: http://stackoverflow.com/questions/3216535/x86-x64-cpuid-in-c-sharp
                    // rcx is level, rdx is buffer.
                    0x53,                           // push rbx     ; this gets clobbered by cpuid
                    0x49, 0x89, 0xD0,               // mov r8,  rdx ; Save rdx (buffer) to r8
                    0x89, 0xC8,                     // mov eax, ecx ; Move ecx (level) to eax to call cpuid
                    0x0F, 0xA2,                     // cpuid
                    0x41, 0x89, 0x00,               // mov    dword ptr [r8],    eax ; write eax, ... to buffer
                    0x41, 0x89, 0x58, 0x04,         // mov    dword ptr [r8+4],  ebx
                    0x41, 0x89, 0x48, 0x08,         // mov    dword ptr [r8+8],  ecx
                    0x41, 0x89, 0x50, 0x0C,         // mov    dword ptr [r8+12], edx
                    0x5B,                           // pop rbx
                    0xC3,                           // ret
                })
                *ptr++ = b;
        }

        static bool VerifyPrologue(byte* ptr)
        {
            foreach (var expected in new[] {
                    new byte[] { 0x53, 0x48, 0x83, 0xEC, 0x20 },
                    new byte[] { 0x56, 0x48, 0x83, 0xEC, 0x20 },
                    new byte[] { 0x55, 0x48, 0x83, 0xEC, 0x20 },
                    new byte[] { 0x55, 0x57, 0x56, 0x48, 0x83, 0xEC, 0x30 },
                    new byte[] { 0x48, 0x89, 0x54, 0x24 },
                    new byte[] { 0x48, 0x83, 0xEC, 0x28 },
                }) {
                var goodMatch = true;
                for (var i = 0; i < expected.Length; i++) {
                    if (ptr[i] != expected[i]) {
                        goodMatch = false;
                        break;
                    }
                }
                if (goodMatch)
                    return true;
            }
            return false;
        }

        internal static string PatchClass(Type type)
        {
            if (Error != null) {
                return "Error: " + Error;
            }
            var result = new StringBuilder("Ok");
            result.AppendLine();
            var methods = type.GetMethods(DefaultBinding);
            foreach (var m in methods) {
                var attr = GetCustomAttribute<ReplaceWithAttribute>(m);
                if (attr != null) {
                    if ((m.GetMethodImplementationFlags() & MethodImplAttributes.NoInlining) == 0)
                        throw new Exception("Method should be marked with MethodImplOptions.NoInlining");
                    // TODO: check that method has <= 2 args
                    if (attr.CheckFeatures(CpuFeatures)) {
                        PatchMethod(m, attr.GetCodeAsULong());
                    } else {
                        result.AppendLine("Feature " + m.Name + " is not supported by this CPU");
                    }
                }
            }
            return result.ToString();
        }

        static T GetCustomAttribute<T>(MethodInfo method) where T : class
        {
            var attrs = method.GetCustomAttributes(typeof(T), false);
            return attrs.Length > 0 ? attrs[0] as T : null;
        }

        static void PatchMethod(MethodInfo method, ulong code)
        {
            var ptr = GetMethodPtr(method);
            *(ulong*)ptr = *(ulong*)ptr & ~Mask5Bytes | code; ptr += 5;
            foreach (var b in new byte[] {
                    0x50,                           // push   rax
                    0x48, 0x83, 0xEC, 0x20,         // sub    rsp, 0x20
                    0x48, 0x8B, 0x4C, 0x24, 0x28,   // mov    rcx, QWORD PTR [rsp+0x28]
                    0x48, 0x8B, 0x15, 0xEA,         // mov    rdx, QWORD PTR [rip-0x16]
                    0xFF, 0xFF, 0xFF,
                    0x48, 0xB8,                     // movabs rax, <PatchCallSite>
                })
                *ptr++ = b;
            *(long*)ptr = (long)PatchCallSitePtr; ptr += 8;
            foreach (var b in new byte[] {
                    0xFF, 0xD0,                     // call   rax
                    0x48, 0x83, 0xC4, 0x20,         // add    rsp, 0x20
                    0x58,                           // pop    rax
                    0xC3,                           // ret
                })
                *ptr++ = b;
        }
    }
}
