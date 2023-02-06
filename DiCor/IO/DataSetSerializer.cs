using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using DiCor.Values;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiCor.IO
{
    public record struct ElementMessage(Tag Tag, VR VR, uint Length);

    public partial class DataSetSerializer : IMessageReader<ElementMessage>, IAsyncDisposable
    {
        private struct State
        {
            public ValueStore Store;
            public GrowableArray<CharacterEncoding.Details> CharacterEncodings;
            public long EndIndex;

            public State(DataSet set, GrowableArray<CharacterEncoding.Details> characterEncodings, long endIndex = long.MaxValue)
            {
                Store = set.Store;
                CharacterEncodings = characterEncodings;
                EndIndex = endIndex;
            }
        }

        private const uint UndefinedLength = unchecked((uint)-1);

        private readonly ProtocolReader _reader;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger _logger;
        private State _state;

        public DataSetSerializer(Stream stream, CancellationToken cancellationToken, ILogger? logger = null)
        {
            Debug.Assert(stream is not null);

            _logger = logger ?? NullLogger.Instance;
            _reader = new ProtocolReader(stream);
            _cancellationToken = cancellationToken;
        }

        internal ValueStore Store
            => _state.Store;

        internal GrowableArray<CharacterEncoding.Details> CharacterEncodings
        {
            get => _state.CharacterEncodings;
            set => _state.CharacterEncodings = value;
        }

        internal long CalculateEndIndex(uint length)
            => length == UndefinedLength ? long.MaxValue : _reader.GetConsumedIndex() + length;

        internal void SetEndIndex(uint length)
            => _state.EndIndex = CalculateEndIndex(length);

        public async ValueTask<DataSet> DeserializeAsync(Func<ElementMessage, ValueTask>? messageHandler)
        {
            DataSet set = new(false);
            await DeserializeAsync(new State(set, CharacterEncodings), messageHandler).ConfigureAwait(false);

            return set;
        }

        private async ValueTask DeserializeAsync(State state, Func<ElementMessage, ValueTask>? messageHandler)
        {
            State parent = _state;
            _state = state;

            while (_reader.GetConsumedIndex() < _state.EndIndex)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                ProtocolReadResult<ElementMessage> result = await _reader.ReadAsync(this, null).ConfigureAwait(false);
                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }

                ElementMessage message = result.Message;
                _reader.Advance();

#pragma warning disable CA2012 // Use ValueTasks correctly
                await (messageHandler?.Invoke(message) ?? ValueTask.CompletedTask).ConfigureAwait(false);
#pragma warning restore CA2012 // Use ValueTasks correctly

                if (message.VR == VR.SQ)
                {
                    SQValue sequence = await DeserializeSequenceAsync(message.Length).ConfigureAwait(false);
                    Store.SetSequence(message.Tag, sequence);
                }
                else if (message.Tag == Tag.SpecificCharacterSet)
                {
                    GrowableArray<CharacterEncoding.Details> characterEncodings = default;
                    ushort i = 0;
                    while (Store.TryGet(Tag.SpecificCharacterSet, i++, out AsciiString characterSet))
                    {
                        if (new CharacterEncoding(characterSet).IsKnown(out CharacterEncoding.Details? characterEncoding))
                        {
                            characterEncodings.Add(characterEncoding);
                        }
                    }
                    CharacterEncodings = characterEncodings;
                }
            }

            _state = parent;
        }

        private async ValueTask<SQValue> DeserializeSequenceAsync(uint sequenceLength)
        {
            // The sequenceSet contains the Item and ItemDelimitationItem tags after deserialization and gets discarded.
            DataSet sequenceSet = new(false);
            GrowableArray<DataSet> sequenceItems = default;
            await DeserializeAsync(new State(sequenceSet, CharacterEncodings, CalculateEndIndex(sequenceLength)), HandleSequenceElement).ConfigureAwait(false);

            return new SQValue(sequenceItems);

            async ValueTask HandleSequenceElement(ElementMessage message)
            {
                if (message.Tag == Tag.Item)
                {
                    sequenceItems.Add(await DeserializeSequenceItemAsync(message.Length).ConfigureAwait(false));
                }
                else if (message.Tag == Tag.SequenceDelimitationItem)
                {
                    SetEndIndex(0);
                }
            }
        }

        private async ValueTask<DataSet> DeserializeSequenceItemAsync(uint itemLength)
        {
            DataSet itemSet = new(false);
            await DeserializeAsync(new State(itemSet, CharacterEncodings, CalculateEndIndex(itemLength)), HandleSequenceItemElement).ConfigureAwait(false);

            return itemSet;

            ValueTask HandleSequenceItemElement(ElementMessage message)
            {
                if (message.Tag == Tag.ItemDelimitationItem)
                {
                    SetEndIndex(0);
                }

                return ValueTask.CompletedTask;
            }
        }

        public ValueTask DisposeAsync()
        {
            return _reader.DisposeAsync();
        }
    }
}
