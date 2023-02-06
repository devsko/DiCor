using System;
using System.Buffers;
using System.Text;
using DotNext;
using DotNext.Buffers;

namespace DiCor.IO
{
    public partial class DataSetSerializer
    {
        // PS3.5 - 6.1.2.2 Extension or Replacement of the Default Character Repertoire
        // For Data Elements with Value Representations of SH(Short String), LO(Long String), UC(Unlimited Characters),
        // ST(Short Text), LT(Long Text), UT(Unlimited Text) or PN(Person Name) the Default Character Repertoire may
        // be extended or replaced(these Value Representations are described in more detail in Section 6.2). If such
        // an extension or replacement is used, the relevant "Specific Character Set" shall be defined as an Attribute
        // of the SOP Common Module(0008,0005) (see PS3.3) and shall be stated in the Conformance Statement.PS3.2
        // gives conformance guidelines.
        // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.1.2.2

        private static ReadOnlySpan<byte> ControlExtensionResetBytes => "\x09\x0A\x0C\x0D"u8;
        private static ReadOnlySpan<byte> PNValueExtensionResetBytes => "^="u8;

        private string ReadAndDecodeString(ref SequenceReader<byte> reader, ReadOnlySpan<byte> extensionResetBytes = default)
        {
            using SparseBufferWriter<char> bufferWriter = new();
            if (CharacterEncodings.Length < 2)
            {
                (CharacterEncodings.SingleValueOrNull ?? CharacterEncoding.Details.Default).Encoding.GetChars(reader.UnreadSequence, bufferWriter);
            }
            else
            {
                // PS3.3 - C.12.1.1.2 Specific Character Set
                // ...
                // If the Attribute Specific Character Set(0008,0005) has more than one value and value 1 is empty,
                // it is assumed that value 1 is ISO 2022 IR 6.

                CharacterEncoding.Details[] characterEncodings = CharacterEncodings.MultipleValues;
                CharacterEncoding.Details firstCharacterEncoding = characterEncodings.Length == 0
                    ? CharacterEncoding.Details.Default
                    : characterEncodings[0] ?? CharacterEncoding.Details.DefaultIso2022;
                Decoder currentDecoder = firstCharacterEncoding.Decoder;
                currentDecoder.Reset();

                Span<byte> escapeSequence = stackalloc byte[4];
                bool hasMoreComponents = true;
                while (hasMoreComponents)
                {
                    // Detect the next byte that maybe reset the character set extension. It doesn't necessarily do
                    // reset it because it is eventually part of a multi byte sequence. That gets detected further down.

                    byte separator = 0;
                    if (hasMoreComponents = reader.TryReadToAny(out ReadOnlySequence<byte> component, extensionResetBytes, false))
                    {
                        reader.TryRead(out separator);
                    }
                    else
                    {
                        component = reader.UnreadSequence;
                    }

                    SequenceReader<byte> componentReader = new SequenceReader<byte>(component);
                    while (componentReader.TryReadTo(out ReadOnlySequence<byte> part, 0x1B, false))
                    {
                        if (part.Length > 0)
                        {
                            currentDecoder.Convert(in part, bufferWriter, flush: true, out _, out _);
                        }

                        escapeSequence.Clear();
                        componentReader.TryCopyTo(escapeSequence.Slice(0, Math.Min((int)componentReader.Remaining, escapeSequence.Length)));
                        bool validEscapeSequence = false;

                        for (int i = 1; i < CharacterEncodings.Length + 1; i++)
                        {
                            CharacterEncoding.Details characterEncoding = i < CharacterEncodings.Length
                                ? characterEncodings[i]
                                : CharacterEncoding.Details.DefaultIso2022;

                            if (escapeSequence.Slice(1, characterEncoding.EscapeSequence.Length).SequenceEqual(characterEncoding.EscapeSequence))
                            {
                                validEscapeSequence = true;
                                escapeSequence = escapeSequence.Slice(0, characterEncoding.EscapeSequence.Length + 1);

                                currentDecoder = characterEncoding.Decoder;
                                currentDecoder.Reset();
                                if (characterEncoding.EncodingIsIso2022Aware)
                                {
                                    currentDecoder.Convert(escapeSequence, bufferWriter, flush: false, out _, out _);
                                }

                                componentReader.Advance(escapeSequence.Length);
                                break;
                            }
                        }
                        if (!validEscapeSequence)
                        {
                            currentDecoder.Convert(componentReader.CurrentSpan.Slice(0, 1), bufferWriter, flush: true, out _, out _);
                            componentReader.Advance(1);
                        }
                    }
                    if (!componentReader.End)
                    {
                        currentDecoder.Convert(componentReader.UnreadSequence, bufferWriter, flush: false, out _, out _);
                    }

                    if (hasMoreComponents)
                    {
                        // We have to detect if the separator byte is part of a multi byte encoded character.
                        // Only if it is not, it resets the current encoding.

                        char separatorChar = '\0';
                        currentDecoder.Convert(new Span<byte>(ref separator), new Span<char>(ref separatorChar), flush: false, out _, out int charsUsed, out _);

                        if (charsUsed == 1)
                        {
                            bufferWriter.Add(separatorChar);
                            if (separatorChar == (char)separator)
                            {
                                currentDecoder = firstCharacterEncoding.Decoder;
                                currentDecoder.Reset();
                            }
                        }
                    }
                }
            }
            reader.AdvanceToEnd();

            return string.Create((int)bufferWriter.WrittenCount, bufferWriter,
                (span, bufferWriter) => bufferWriter.CopyTo(span));
        }
    }
}
