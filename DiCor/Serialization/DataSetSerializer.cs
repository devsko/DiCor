using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using DiCor.Values;

namespace DiCor.Serialization
{
    internal record struct ElementMessage(Tag Tag, VR VR, uint Length);

    internal partial class DataSetSerializer : IMessageReader<ElementMessage>
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
            public GrowingArray<DataSet> SequenceItems;
        }

        private const uint UndefinedLength = unchecked((uint)-1);

        private ProtocolReader _reader = null!;
        private CancellationToken _cancellationToken;
        private State _state;

        public async ValueTask<(DataSet FileMetaInfoSet, DataSet DataSet)> DeserializeFileAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;

            DataSet fileMetaInfoSet = new(false);
            DataSet dataSet = new(false);
            await using ((_reader = new ProtocolReader(stream)).ConfigureAwait(false))
            {
                await DeserializeAsync(new State(fileMetaInfoSet), OnFileMetaInformationElementAsync).ConfigureAwait(false);
                await DeserializeAsync(new State(dataSet), OnDataElementAsync).ConfigureAwait(false);
            }

            return (fileMetaInfoSet, dataSet);
        }

        private async ValueTask<State> DeserializeAsync(State state, Func<ElementMessage, ValueTask> onElementAsync)
        {
            State parent = _state;
            _state = state;

            while (_reader.GetConsumedIndex() < _state.EndIndex)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                ProtocolReadResult<ElementMessage> result = await _reader.ReadAsync(this, null, _cancellationToken).ConfigureAwait(false);
                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }

                ElementMessage message = result.Message;
                _reader.Advance();

                await onElementAsync(message).ConfigureAwait(false);
            }

            state = _state;
            _state = parent;

            return state;
        }

        private async ValueTask<SQValue> DeserializeSequenceAsync(uint sequenceLength)
        {
            DataSet sequenceSet = new(false);
            State state = await DeserializeAsync(new State(sequenceSet, sequenceLength), OnSequenceElementAsync).ConfigureAwait(false);

            return state.SequenceItems.Length == 1
                ? new SQValue(state.SequenceItems.SingleValue)
                : new SQValue(state.SequenceItems.MultipleValues);
        }

        private async ValueTask<DataSet> DeserializeSequenceItemAsync(uint itemLength)
        {
            DataSet itemSet = new(false);
            await DeserializeAsync(new State(itemSet, itemLength), OnSequenceItemElementAsync).ConfigureAwait(false);

            return itemSet;
        }

        private ValueTask OnFileMetaInformationElementAsync(ElementMessage message)
        {
            if (message.Tag == Tag.FileMetaInformationGroupLength)
            {
                _state.EndIndex = CalculateEnd(_state.Store.Get<uint>(Tag.FileMetaInformationGroupLength));
            }

            return ValueTask.CompletedTask;
        }

        private async ValueTask OnDataElementAsync(ElementMessage message)
        {
            if (message.VR == VR.SQ)
            {
                _state.Store.SetSequence(message.Tag, await DeserializeSequenceAsync(message.Length).ConfigureAwait(false));
            }
        }

        private async ValueTask OnSequenceElementAsync(ElementMessage message)
        {
            if (message.Tag == Tag.Item)
            {
                _state.SequenceItems.Add(await DeserializeSequenceItemAsync(message.Length).ConfigureAwait(false));
            }
            else if (message.Tag == Tag.SequenceDelimitationItem)
            {
                _state.EndIndex = CalculateEnd(0);
            }
        }

        private async ValueTask OnSequenceItemElementAsync(ElementMessage message)
        {
            if (message.Tag == Tag.ItemDelimitationItem)
            {
                _state.EndIndex = CalculateEnd(0);
            }
            await OnDataElementAsync(message).ConfigureAwait(false);
        }

        private long CalculateEnd(uint length)
            => length == UndefinedLength ? long.MaxValue : _reader.GetConsumedIndex() + length;
    }
}
