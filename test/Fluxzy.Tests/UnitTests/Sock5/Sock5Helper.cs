using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Sock5
{
    public class Sock5Helper
    {
        [Fact]
        public void Test()
        {
            var memoryStream = new MemoryStream();
            
            memoryStream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);




        }
    }
}
