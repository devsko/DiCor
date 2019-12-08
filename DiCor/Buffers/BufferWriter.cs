using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Policy;

namespace DiCor.Buffers
{
    public ref partial struct BufferWriter
    {
        private struct State
        {
            public int _buffered;
            public int _committed;
            public int _lengthPrefixCount;

            public unsafe Span<State> AsSpan()
            {
                return new Span<State>(Unsafe.AsPointer(ref this), 1);
            }
        }

        private readonly IBufferWriter<byte> _output;
        private Span<byte> _span;
        private State _state;

        public Span<byte> Span => _span;

        public BufferWriter(IBufferWriter<byte> output)
        {
            _output = output;
            _span = output.GetSpan();
            _state = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            int buffered = _state._buffered;
            if (buffered > 0)
            {
                _state._committed += _state._buffered;
                _state._buffered = 0;
                _output.Advance(buffered);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _span = Span.Slice(count);
            _state._buffered += count;
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
            if (_span.Length < count)
            {
                EnsureMore(count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            Commit();
            _span = _output.GetSpan(count);
        }

        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (_span.Length == 0)
                {
                    EnsureMore();
                }

                int writable = Math.Min(source.Length, _span.Length);
                source.Slice(0, writable).CopyTo(_span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }
    }
}
