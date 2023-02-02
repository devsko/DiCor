using System.Diagnostics;
using System.Linq;
using DiCor.Values;

namespace DiCor
{
    internal class DataSetDebugView
    {
        private readonly ValueStore _store;

        public DataSetDebugView(DataSet set)
            : this(set.Store)
        { }

        public DataSetDebugView(ValueStore store)
            => _store = store;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public (Tag Tag, object Value)[] Items
            => _store.EnumerateValuesForDebugger().ToArray();
    }
}
