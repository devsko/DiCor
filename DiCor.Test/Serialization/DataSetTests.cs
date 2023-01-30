using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DiCor.IO;
using DotNext.Threading;
using FellowOakDicom;
using Xunit;
using static DiCor.Test.Serialization.DataSetTests;

namespace DiCor.Test.Serialization
{
    public class DataSetTests
    {
        [Fact]
        public void FoDicom()
        {
            var file = DicomFile.Open(@"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI");
        }

        [Fact]
        public async Task SmokeTest()
        {
            // TODO Test perf of bufferSize (Default, 0, 1), isAsync

            using FileStream stream = new FileStream(
                @"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI",
                FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            await FileReader.ReadAsync(stream).ConfigureAwait(false);
        }

        public class C
        {
            private float[] _payload;
            public C(float[] payload) => _payload = payload;
            public float[] Payload => _payload;
            public override string ToString() => _payload[0].ToString();
        }
        public struct S1
        {
            public C C;

            public unsafe IntPtr AddressOfC()
            {
                fixed (void* ptr = &C.Payload[0])
                    return (IntPtr)ptr;
            }
        }

        private static S1[] s_s1Arr = new S1[1];
        private S1Ref CreateRef()
        {
            return S1Ref.From(ref s_s1Arr[0]);
        }
        private void CreateS(S1Ref s1Ref, float[] payload, out IntPtr p)
        {
            S1 s1 = new S1() { C = new(payload) };
            s1Ref.As<S1>() = s1;

            p = s1.AddressOfC();
        }
        public ref struct S1Ref
        {
            private struct Sx { }
            private ref Sx _ref;

            public static S1Ref From<T>(ref T s1) where T : struct
            {
                return new() { _ref = ref Unsafe.As<T, Sx>(ref s1) };
            }

            public ref T As<T>() where T : struct
            {
                return ref Unsafe.As<Sx, T>(ref _ref);
            }
        }

        [Fact]
        public void Test2()
        {
            S1Ref s1Ref = CreateRef();

            float[] payload = new float[1] { 1234 };

            // Hope C gets moved around when somthing is allocated before
            int[]? f = new int[1024];

            CreateS(s1Ref, payload, out IntPtr p);

            // C should get collected if unrooted
            f = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Make some pressure
            int[] array;
            for (int i = 0; i < 1000; i++)
            {
                array = new int[1024];
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            S1 s1 = s1Ref.As<S1>();

            IntPtr p2 = s1.AddressOfC();

            Assert.NotEqual(p, p2);
            Assert.Same(payload, s1.C.Payload);
            //Assert.Equal("1234", s1.C.ToString());
        }
    }
}
