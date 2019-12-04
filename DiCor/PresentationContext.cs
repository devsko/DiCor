using System;
using System.Collections.Generic;
using System.Text;

namespace DiCor
{
    public class PresentationContext
    {
        public Uid AbstractSyntax { get; set; }
        public IEnumerable<Uid> TransferSyntaxes { get; }
        public bool? SupportsScpRole { get; set; }
        public bool? SupportsScuRole { get; set; }
        public byte Id { get; internal set; }
    }
}
