﻿using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

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
            private readonly Refs _refs;
            private readonly int _position;
            private readonly int _currentPrefixCount;

            public LengthPrefix(ref BufferWriter buffer, int prefixLength)
            {
                buffer.Ensure(prefixLength);
                _span = buffer.Span.Slice(0, prefixLength);
                buffer.Advance(prefixLength);

                Refs refs = _refs = buffer._refs;
                _position = refs._committed + refs._buffered;
                _currentPrefixCount = refs._lengthPrefixCount++;
            }

            public void Write()
            {
                if (_span.Length == 0)
                    throw new InvalidOperationException("Length prefix already written.");

                Refs refs = _refs;
                if (_currentPrefixCount != --refs._lengthPrefixCount)
                    throw new InvalidOperationException("Legnth prefix mismatch");

                uint length = (uint)(refs._committed + refs._buffered - _position);

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

                _span = default;
            }

            public void Dispose()
                => Write();

        }
    }
}
