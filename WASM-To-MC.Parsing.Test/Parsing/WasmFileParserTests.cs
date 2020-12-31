using System;
using System.Linq;
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
        [InlineData(new byte[] { 0b01111111             }, (byte)0b01111111)]
        [InlineData(new byte[] { 0b11111111, 0b00000000 }, (byte)0b01111111)]
        [InlineData(new byte[] { 0b10000000, 0b00000001 }, (byte)0b10000000)]
        [InlineData(new byte[] { 0b10001010, 0b00000001 }, (byte)0b10001010)]
        [InlineData(new byte[] { 0b11111111, 0b00000001 }, (byte)0b11111111)]
        public void ULEB128ValidByte(byte[] input, byte result)
        {
            var parser = new WasmFileParser(input);
            var @int = parser.ULEB128(new UByte(8));
            Assert.Equal(result, @int.Value);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 0b11111111, 0b00000010 })]
        [InlineData(new byte[] { 0b10000000, 0b10000000 })]
        [InlineData(new byte[] { 0b10001010, 0b00010001 })]
        [InlineData(new byte[] { 0b10000000, 0b00000010 })]
        public void ULEB128TooLargeByte(byte[] input)
        {
            Assert.Throws<ParseException>(() =>
            {
                var parser = new WasmFileParser(input);
                _ = parser.ULEB128(new UByte(8));
            });
        }

        [Theory]
        [InlineData(new byte[] { 0b01111111                         }, (ushort)0b0000000001111111)]
        [InlineData(new byte[] { 0b11111111, 0b00000000             }, (ushort)0b0000000001111111)]
        [InlineData(new byte[] { 0b10000000, 0b00000001             }, (ushort)0b0000000010000000)]
        [InlineData(new byte[] { 0b10001010, 0b00000001             }, (ushort)0b0000000010001010)]
        [InlineData(new byte[] { 0b10001010, 0b00010001             }, (ushort)0b0000100010001010)]
        [InlineData(new byte[] { 0b10001010, 0b10010001, 0b00000010 }, (ushort)0b1000100010001010)]
        [InlineData(new byte[] { 0b11111111, 0b11111111, 0b00000011 }, (ushort)0b1111111111111111)]
        public void ULEB128ValidShort(byte[] input, ushort result)
        {
            var parser = new WasmFileParser(input);
            var @int = parser.ULEB128(new UShort(16));
            Assert.Equal(result, @int.Value);
            Assert.Equal(input.Length, parser.Index);
        }

        [Theory]
        [InlineData(new byte[] { 0b10001010, 0b10010001, 0b01000010 })]
        [InlineData(new byte[] { 0b10000000, 0b10000000, 0b00000100 })]
        [InlineData(new byte[] { 0b10000000, 0b10000000, 0b10000000 })]
        public void ULEB128TooLargeShort(byte[] input)
        {
            Assert.Throws<ParseException>(() =>
            {
                var parser = new WasmFileParser(input);
                _ = parser.ULEB128(new UShort(16));
            });
        }
    }
}
