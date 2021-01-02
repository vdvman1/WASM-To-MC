using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WASM_To_MC.Parsing;
using WASM_To_MC.Shared;
using Xunit;

namespace WASM_To_MC.Test.Test.Parsing
{
    public class WasmFileParserTests
    {
        [Fact]
        public void ParserStartsAtBeginning()
        {
            var parser = new WasmFileParser(new byte[] { 0, 1 });
            Assert.Equal(0, parser.Index);
        }

        [Fact]
        public void TryNextByteFalseWhenEmpty()
        {
            var parser = new WasmFileParser(Array.Empty<byte>());
            Assert.False(parser.TryNextByte(out _));
            Assert.Equal(0, parser.Index);
        }

        [Theory]
        [InlineData((byte)0x00)]
        [InlineData((byte)0xFF)]
        [InlineData((byte)0xF0)]
        public void TryNextByteProducesByteWhenNotEmpty(byte test)
        {
            var parser = new WasmFileParser(new[] { test });
            Assert.True(parser.TryNextByte(out byte b));
            Assert.Equal(test, b);
            Assert.Equal(1, parser.Index);
        }

        [Theory]
        [InlineData((byte)0x00, (byte)0x01)]
        [InlineData((byte)0xFF, (byte)0x05)]
        [InlineData((byte)0xF0, (byte)0xA0)]
        public void TryNextByteProducesMultipleBytesInOrder(byte test1, byte test2)
        {
            var parser = new WasmFileParser(new[] { test1, test2 });
            Assert.True(parser.TryNextByte(out byte b));
            Assert.Equal(test1, b);
            Assert.Equal(1, parser.Index);

            Assert.True(parser.TryNextByte(out b));
            Assert.Equal(test2, b);
            Assert.Equal(2, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 0b0_1111111              }, (byte)0b0_1111111)]
        [InlineData(new byte[] { 0b1_1111111, 0b0_0000000 }, (byte)0b0_1111111)]
        [InlineData(new byte[] { 0b1_0000000, 0b0_0000001 }, (byte)0b1_0000000)]
        [InlineData(new byte[] { 0b1_0001010, 0b0_0000001 }, (byte)0b1_0001010)]
        [InlineData(new byte[] { 0b1_1111111, 0b0_0000001 }, (byte)0b1_1111111)]
        public void ULEB128ValidByte(byte[] input, byte result)
        {
            var parser = new WasmFileParser(input);
            var @int = parser.LEB128(new UByte(8));
            Assert.Equal(result, @int.Value);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 0b1_1111111, 0b0_0000010 })]
        [InlineData(new byte[] { 0b1_0000000, 0b1_0000000 })]
        [InlineData(new byte[] { 0b1_0001010, 0b0_0010001 })]
        [InlineData(new byte[] { 0b1_0000000, 0b0_0000010 })]
        public void ULEB128TooLargeByte(byte[] input)
        {
            Assert.Throws<ParseException>(() =>
            {
                var parser = new WasmFileParser(input);
                _ = parser.LEB128(new UByte(8));
            });
        }

        [Theory]
        [InlineData(new byte[] { 0b0_1111111                           }, (ushort)0b00_0000000_1111111)]
        [InlineData(new byte[] { 0b1_1111111, 0b0_0000000              }, (ushort)0b00_0000000_1111111)]
        [InlineData(new byte[] { 0b1_0000000, 0b0_0000001              }, (ushort)0b00_0000001_0000000)]
        [InlineData(new byte[] { 0b1_0001010, 0b0_0000001              }, (ushort)0b00_0000001_0001010)]
        [InlineData(new byte[] { 0b1_0001010, 0b0_0010001              }, (ushort)0b00_0010001_0001010)]
        [InlineData(new byte[] { 0b1_0001010, 0b1_0010001, 0b0_0000010 }, (ushort)0b10_0010001_0001010)]
        [InlineData(new byte[] { 0b1_1111111, 0b1_1111111, 0b0_0000011 }, (ushort)0b11_1111111_1111111)]
        public void ULEB128ValidShort(byte[] input, ushort result)
        {
            var parser = new WasmFileParser(input);
            var @int = parser.LEB128(new UShort(16));
            Assert.Equal(result, @int.Value);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 0b1_0001010, 0b1_0010001, 0b0_1000010 })]
        [InlineData(new byte[] { 0b1_0000000, 0b1_0000000, 0b0_0000100 })]
        [InlineData(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000 })]
        public void ULEB128TooLargeShort(byte[] input)
        {
            Assert.Throws<ParseException>(() =>
            {
                var parser = new WasmFileParser(input);
                _ = parser.LEB128(new UShort(16));
            });
        }

        [Theory]
        [InlineData(new byte[] { 0b0_1111111                           }, unchecked((short)0b11_1111111_1111111))]
        [InlineData(new byte[] { 0b1_1111111, 0b0_0000000              }, unchecked((short)0b00_0000000_1111111))]
        [InlineData(new byte[] { 0b1_0000000, 0b0_0000001              }, unchecked((short)0b00_0000001_0000000))]
        [InlineData(new byte[] { 0b1_0000000, 0b0_1000001              }, unchecked((short)0b11_1000001_0000000))]
        [InlineData(new byte[] { 0b1_0001010, 0b0_0000001              }, unchecked((short)0b00_0000001_0001010))]
        [InlineData(new byte[] { 0b1_1111111, 0b0_0000001              }, unchecked((short)0b00_0000001_1111111))]
        [InlineData(new byte[] { 0b1_0001010, 0b1_0010001, 0b0_0000010 }, unchecked((short)0b10_0010001_0001010))]
        [InlineData(new byte[] { 0b1_0001010, 0b1_0010001, 0b0_0000001 }, unchecked((short)0b01_0010001_0001010))]
        [InlineData(new byte[] { 0b1_1111111, 0b1_1111111, 0b0_0000011 }, unchecked((short)0b11_1111111_1111111))]
        public void ULEB128ValidSShort(byte[] input, short result)
        {
            var parser = new WasmFileParser(input);
            var @int = parser.LEB128(new SShort(16));
            Assert.Equal(result, @int.Value);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 0b00000000, 0b00000000, 0b01000110, 0b01000001 }, 12.375f)]
        public void Float(byte[] input, float result)
        {
            var parser = new WasmFileParser(input);
            var @float = parser.Float(new Float32());
            Assert.Equal(result, @float.Value);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 2, 12, 10 }, new byte[2] { 12, 10 })]
        [InlineData(new byte[] { 2, 0b1_0000000, 0b0_0000001, 10 }, new byte[2] { 0b1_0000000, 10 })]
        public void VecUByte(byte[] input, byte[] result)
        {
            var parser = new WasmFileParser(input);
            var values = parser.Vector(() => parser.LEB128(new UByte(8))).Select(b => b.Value);
            Assert.Equal(result, values);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("€")]
        public void Name(string result)
        {
            var bytes = Encoding.UTF8.GetBytes(result);
            bytes = bytes.Prepend((byte)bytes.Length).ToArray();
            var parser = new WasmFileParser(bytes);
            var val = parser.Name();
            Assert.Equal(result, val);
            Assert.Equal(bytes.Length, parser.Index);
        }
    }
}
