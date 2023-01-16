using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiCor.Generator
{
    internal class DocBookData
    {
        public Part06 Part06 { get; }
        public Part07 Part07 { get; }
        public Part16 Part16 { get; }

        public DocBookData(Part06 part06, Part07 part07, Part16 part16)
        {
            Part06 = part06;
            Part07 = part07;
            Part16 = part16;
        }

        public async Task ExtractAsync()
        {
            await Task.WhenAll(
                Part06.Initialization,
                Part07.Initialization,
                Part16.Initialization
            ).ConfigureAwait(false);

            await Task.WhenAll(
                GetCidAndPart06TablesAsync(),
                Part07.GetTablesAsync(this)
            ).ConfigureAwait(false);

            async Task GetCidAndPart06TablesAsync()
            {
                await Part16.GetSectionsByIdAsync(this).ConfigureAwait(false);
                await Part06.GetTablesAsync(this).ConfigureAwait(false);
            }
        }

        public Dictionary<int, CidValues>? CidTable { get; set; }
        public UidValues[]? TableA1 { get; set; }
        public UidValues[]? TableA3 { get; set; }
        public TagValues[]? Table61 { get; set; }
        public TagValues[]? Table71 { get; set; }
        public TagValues[]? Table81 { get; set; }
        public TagValues[]? Table91 { get; set; }
        public TagValues[]? TableE11 { get; set; }
        public TagValues[]? TableE21 { get; set; }
    }
}
