using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WASM_To_MC.Shared;

namespace WASM_To_MC.Parsing.Binary
{
    public partial class WasmFileParser
    {
        private readonly Stack<BinaryParser> Parsers = new();
        private BinaryParser Parser;

        private bool PushParser(int length)
        {
            if (Parser.TryMakeChildParser(length, out BinaryParser? child))
            {
                Parsers.Push(Parser);
                Parser = child;
                return true;
            }

            return false;
        }

        private void PopParser() => Parser = Parsers.Pop();

        public WasmFileParser(byte[] bytes) => Parser = BinaryParser.From(bytes);

        internal T[] Vector<T>(uint expectedLength, string wrongLengthMsg, Func<T> valueParser)
        {
            uint uLength = Parser.LEB128(new UInt(32)).Value;
            if(uLength != expectedLength)
            {
                throw new ParseException(wrongLengthMsg);
            }

            if(uLength > int.MaxValue)
            {
                throw new ParseException($"Length is too large for this implementation: {uLength}");
            }

            int length = (int)uLength;
            var arr = new T[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = valueParser();
            }

            return arr;
        }

        internal T[] Vector<T>(Func<T> valueParser)
        {
            uint uLength = Parser.LEB128(new UInt(32)).Value;
            if (uLength > int.MaxValue)
            {
                throw new ParseException($"Length is too large for this implementation: {uLength}");
            }

            int length = (int)uLength;
            var arr = new T[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = valueParser();
            }

            return arr;
        }

        internal byte[] ByteVector()
        {
            uint size = Parser.LEB128(new UInt(32)).Value;
            if (size > int.MaxValue)
            {
                throw new ParseException($"Length is too large for this implementation: {size}");
            }

            return Parser.NextBytes((int)size);
        }

        internal string Name()
        {
            try
            {
                return Encoding.UTF8.GetString(Vector(Parser.NextByte));
            }
            catch (Exception e) when (e is not ParseException)
            {
                throw new ParseException("Invalid name, see inner exception for details", e);
            }
        }

        internal WasmValueType ValueType()
        {
            var b = Parser.NextByte();
            var type = (WasmValueType)b;
            return type is > WasmValueType._Min and < WasmValueType._Max ? type : throw new ParseException($"Unknown value type: {b}");
        }

        internal FunctionType FuncType()
        {
            if (Parser.NextByte() != 0x60)
            {
                throw new ParseException("Expected a function type");
            }

            return new FunctionType(Vector(ValueType), Vector(ValueType));
        }

        internal Limits ParseLimits() => Parser.NextByte() switch
        {
            0x00 => new Limits(Parser.LEB128(new UInt(32)).Value, null),
            0x01 => new Limits(Parser.LEB128(new UInt(32)).Value, Parser.LEB128(new UInt(32)).Value),
            var b => throw new ParseException($"Unknown limit type: {b}")
        };

        internal TableType ParseTableType()
        {
            var b = Parser.NextByte();
            var elemType = (ElementType)b;
            if (elemType is <= ElementType._Min or >= ElementType._Max)
            {
                throw new ParseException($"Unknown element type: {b}");
            }

            return new TableType(elemType, ParseLimits());
        }

        internal GlobalType ParseGlobalType()
        {
            var valType = ValueType();
            return Parser.NextByte() switch
            {
                0x00 => new GlobalType(valType, false),
                0x01 => new GlobalType(valType, true),
                var b => throw new ParseException($"Unknown global mutability: {b}")
            };
        }

        internal Import ParseImport()
        {
            string module = Name();
            string name = Name();
            ImportDescription desc = Parser.NextByte() switch
            {
                0x00 => new ImportDescription.Func(Parser.LEB128(new UInt(32)).Value),
                0x01 => new ImportDescription.Table(ParseTableType()),
                0x02 => new ImportDescription.Mem(ParseLimits()),
                0x03 => new ImportDescription.Global(ParseGlobalType()),
                var b => throw new ParseException($"Unknown Import description: {b}")
            };

            return new Import(module, name, desc);
        }

        private const string InvalidBlockTypeMsg = "Invalid block type, must be a positive type index, a value type, or the empty type. Got: ";
        internal BlockType ParseBlockType()
        {
            long type = Parser.LEB128(new SLong(33)).Value;
            if(type < 0)
            {
                if(type < -0x7F)
                {
                    throw new ParseException(InvalidBlockTypeMsg + type);
                }

                byte b = (byte)(type & 0x7F);
                if(b == 0x40)
                {
                    return BlockType.Empty;
                }

                var valType = (WasmValueType)b;
                if (Enum.IsDefined(valType))
                {
                    return new BlockType.ValueType(valType);
                }

                throw new ParseException(InvalidBlockTypeMsg + type);
            }

            Trace.Assert(type < uint.MaxValue);
            return new BlockType.Index((uint)type);
        }
    }
}