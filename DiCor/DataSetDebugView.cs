using System.Diagnostics;
using System.Linq;

namespace DiCor
{
    internal class DataSetDebugView
    {
        private readonly DataSet _set;

        public DataSetDebugView(DataSet set)
            => _set = set;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public (Tag Tag, object Value)[] Items
            => _set.Store.EnumerateValuesForDebugger().ToArray();
    }
}
