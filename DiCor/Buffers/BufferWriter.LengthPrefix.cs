using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        [UnscopedRef]
        public LengthPrefix BeginLengthPrefix(int length = sizeof(ushort))
        {
            return new LengthPrefix(ref this, length);
        }

        public readonly ref struct LengthPrefix
        {
            private readonly Span<byte> _span;
            private readonly ref State _state;
            private readonly int _position;
            private readonly int _currentPrefixCount;

            public LengthPrefix(ref BufferWriter buffer, int prefixLength)
            {
                buffer.Ensure(prefixLength);
                _span = buffer.Span.Slice(0, prefixLength);
                buffer.Advance(prefixLength);

                _state = ref buffer._state;
                _position = _state._committed + _state._buffered;
                _currentPrefixCount = _state._lengthPrefixCount++;
            }

            public void Write()
            {
                if (_currentPrefixCount != --_state._lengthPrefixCount)
                {
                    throw new InvalidOperationException("Legnth prefix mismatch");
                }

                uint length = (uint)(_state._committed + _state._buffered - _position);

                if (_span.Length == sizeof(uint))
                {
                    BinaryPrimitives.WriteUInt32BigEndian(_span, length);
                }
                else if (_span.Length == sizeof(ushort))
                {
                    BinaryPrimitives.WriteUInt16BigEndian(_span, (ushort)length);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public void Dispose()
            {
                Write();
            }
        }
    }
}
