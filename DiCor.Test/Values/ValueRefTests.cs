using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DiCor.Values;
using Xunit;

namespace DiCor.Test.Values
{
    public class C
    {
        public char[] Payload;
        public C(string payload) => Payload = payload.ToCharArray();
        public IntPtr AdressOf()
        {
            unsafe
            {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                fixed (void* ptr = &Payload)
                {
                    return (IntPtr)ptr;
                }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            }
        }
        public override string ToString() => new string(Payload);
    }

    public readonly struct V : IValue<V>
    {
        private readonly C _c;
        public V(C c) => _c = c;

        public C C => _c;
        public static int PageSize => 5;

        public static V Create<T>(T content) => new V(new C(Unsafe.As<T, string>(ref content)));
        public static bool IsCompatible<T>() => typeof(T) == typeof(string);
        public T Get<T>()
        {
            return Unsafe.As<C, T>(ref Unsafe.AsRef(in _c));
        }
    }
    public class ValueRefTests
    {
        private readonly V[] _store = new V[1];

        private ValueRef CreateValueRef() => ValueRef.Of(ref _store[0]);
        private IntPtr CreateValue(ValueRef valueRef, string content)
        {
            C c = new(content);
            IntPtr adress = c.AdressOf();
            valueRef.Set(new V(c));
            return adress;
        }

        private IntPtr GetContent(ValueRef valueRef, out string content)
        {
            C c = valueRef.As<V>().Get<C>();
            content = c.ToString();
            return c.AdressOf();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ManagedTypesInValuesAreSafe(bool collect)
        {
            ValueRef valueRef = CreateValueRef();

            int[]? d1 = new int[1024];

            IntPtr p1 = CreateValue(valueRef, "Hallo");

            if (collect)
            {
                d1 = null;
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
            }

            IntPtr p2 = GetContent(valueRef, out string content);

            Assert.Equal("Hallo", content);
            if (collect)
                Assert.NotEqual(p1, p2);
            else
                Assert.Equal(p1, p2);
        }
    }
}
