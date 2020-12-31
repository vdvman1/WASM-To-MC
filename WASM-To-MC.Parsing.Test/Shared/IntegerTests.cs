using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASM_To_MC.Shared;
using Xunit;

namespace WASM_To_MC.Test.Shared
{
    public class IntegerTests
    {
        [Fact]
        public void UByteMax()
        {
            Assert.Equal((byte)0xFF, new UByte(8).Max.Value);
        }
    }
}
