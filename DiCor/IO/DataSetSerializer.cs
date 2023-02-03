using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using DiCor.Values;

namespace DiCor.IO
{
    internal record struct ElementMessage(Tag Tag, VR VR, uint Length);

    internal partial class DataSetSerializer : IMessageReader<ElementMessage>, IAsyncDisposable
    {
        private struct State
        {
            public State(DataSet set, long endIndex = long.MaxValue)
            {
                Store = set.Store;
                EndIndex = endIndex;
            }

            public ValueStore Store;
            public long EndIndex;
        }

        private const uint UndefinedLength = unchecked((uint)-1);

        private readonly ProtocolReader _reader;
        private readonly CancellationToken _cancellationToken;
        private State _state;

        public DataSetSerializer(Stream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(stream is not null);

            _reader = new ProtocolReader(stream);
            _cancellationToken = cancellationToken;
        }

        internal ValueStore Store
            => _state.Store;

        internal void SetEndIndex(uint length)
            => _state.EndIndex = length == UndefinedLength ? long.MaxValue : _reader.GetConsumedIndex() + length;

        public async ValueTask<DataSet> DeserializeAsync(Func<ElementMessage, ValueTask>? messageHandler)
        {
            DataSet set = new(false);
            await DeserializeAsync(new State(set), messageHandler).ConfigureAwait(false);

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
                    _state.Store.SetSequence(message.Tag, sequence);
                }
            }

            _state = parent;
        }

        private async ValueTask<SQValue> DeserializeSequenceAsync(uint sequenceLength)
        {
            DataSet sequenceSet = new(false);
            GrowableArray<DataSet> sequenceItems = default;
            await DeserializeAsync(new State(sequenceSet, sequenceLength), HandleSequenceElement).ConfigureAwait(false);

            return sequenceItems.Length == 1
                ? new SQValue(sequenceItems.SingleValue)
                : new SQValue(sequenceItems.MultipleValues);

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
            await DeserializeAsync(new State(itemSet, itemLength), HandleSequenceItemElement).ConfigureAwait(false);

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
