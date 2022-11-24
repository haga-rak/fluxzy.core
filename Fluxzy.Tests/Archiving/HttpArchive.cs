using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Tests.Archiving.Fixtures;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class HttpArchive : IClassFixture<HarFileFixture>
    {
        private readonly HarFileFixture _fixture;

        public HttpArchive(HarFileFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void VerifyBaseContent()
        {
            var document = _fixture.Document.RootElement;
            var log = document.GetProperty("log");
            var entries = log.GetProperty("entries").EnumerateArray().ToList();
            var exchanges = _fixture.Exchanges; 
            
            Assert.Equal("1.2", log.GetProperty("version").GetString());
            Assert.Equal(4, exchanges.Count);
            
        }

    }
}
