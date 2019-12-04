using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DiCor.Buffers
{
    public ref partial struct BufferWriter
    {
        private class Refs
        {
            public int _buffered;
            public int _committed;
            public int _lengthPrefixCount;
        }

        public BufferWriter(IBufferWriter<byte> output)
        {
            _output = output;
            _refs = new Refs();
            Span = output.GetSpan();
        }

        private readonly IBufferWriter<byte> _output;
        private readonly Refs _refs;

        public Span<byte> Span { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            Refs refs = _refs;
            int buffered = refs._buffered;
            if (buffered > 0)
            {
                refs._committed += refs._buffered;
                refs._buffered = 0;
                _output.Advance(buffered);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            Refs refs = _refs;
            refs._buffered += count;
            Span = Span.Slice(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> source)
        {
            if (source.Length > Span.Length)
            {
                WriteMultiBuffer(source);
            }
            else
            {
                source.CopyTo(Span);
                Advance(source.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ensure(int count = 1)
        {
            if (Span.Length < count)
            {
                EnsureMore(count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            Commit();
            Span = _output.GetSpan(count);
        }

        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (Span.Length == 0)
                {
                    EnsureMore();
                }

                int writable = Math.Min(source.Length, Span.Length);
                source.Slice(0, writable).CopyTo(Span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }
    }
}
