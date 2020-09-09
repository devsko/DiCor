using System;
using System.Buffers.Binary;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public LengthPrefix BeginLengthPrefix(int length = sizeof(ushort))
        {
            return new LengthPrefix(ref this, length);
        }

        public ref struct LengthPrefix
        {
            private Span<byte> _span;
            // WORKAROUND: private readonly ref State _state;
            private readonly Span<State> _stateRef;
            private readonly int _position;
            private readonly int _currentPrefixCount;

            public LengthPrefix(ref BufferWriter buffer, int prefixLength)
            {
                buffer.Ensure(prefixLength);
                _span = buffer.Span.Slice(0, prefixLength);
                buffer.Advance(prefixLength);

                _stateRef = buffer._state.AsSpan();
                ref State state = ref _stateRef[0];
                _position = state._committed + state._buffered;
                _currentPrefixCount = state._lengthPrefixCount++;
            }

            public void Write()
            {
                if (_span.Length == 0)
                    throw new InvalidOperationException("Length prefix already written.");

                ref State state = ref _stateRef[0];
                if (_currentPrefixCount != --state._lengthPrefixCount)
                    throw new InvalidOperationException("Legnth prefix mismatch");

                uint length = (uint)(state._committed + state._buffered - _position);

                if (_span.Length == sizeof(uint))
                    BinaryPrimitives.WriteUInt32BigEndian(_span, length);
                else if (_span.Length == sizeof(ushort))
                    BinaryPrimitives.WriteUInt16BigEndian(_span, (ushort)length);
                else
                    throw new InvalidOperationException();

                _span = default;
            }

            public void Dispose()
            {
                if (_stateRef.Length > 0)
                    Write();
            }
        }
    }
}
