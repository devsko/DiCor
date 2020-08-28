using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using DiCor.Generator;

using Xunit;

namespace DiCor.Test.Generator
{
    public static class GeneratorTest
    {
        [Fact]
        public static async Task SmokeTest()
        {
            using var httpClient = new HttpClient();
            using var part16 = new Part16(httpClient, default);
            using var part06 = new Part06(httpClient, default);

            bool is16UpToDate = await part16.IsUpToDateAsync();
            Dictionary<int, string> cidTable = await part16.GetSectionsByIdAsync();

            bool is06UpToDate = await part06.IsUpToDateAsync();
            await part06.GetTablesAsync(cidTable);

        }
    }
}
