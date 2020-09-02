using System.Collections.Generic;
using System.IO;
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
            string path = typeof(GeneratorTest).Assembly.Location;
            path = Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..");
            using var httpClient = new HttpClient();
            using var part16 = new Part16(httpClient, path, default);
            using var part06 = new Part06(httpClient, path, default);

            Dictionary<int, string> cidTable = await part16.GetSectionsByIdAsync();

            var tables = await part06.GetTablesAsync(cidTable);

        }
    }
}
