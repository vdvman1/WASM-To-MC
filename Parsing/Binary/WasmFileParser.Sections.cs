using System;
using System.Collections.Generic;
using System.Linq;
using WASM_To_MC.Shared;

namespace WASM_To_MC.Parsing.Binary
{
    public partial class WasmFileParser
    {
        internal class Segment : IDisposable
        {
            private readonly WasmFileParser wasmFile;
            public readonly int Size;
            private readonly string name;
            private bool disposed = false;

            public Segment(WasmFileParser wasmFile, int size, string name)
            {
                this.wasmFile = wasmFile;
                Size = size;
                this.name = name;
            }
            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    if(wasmFile.Parser.Index < wasmFile.Parser.MaxIndex)
                    {
                        throw new ParseException($"Specified {name} size is larger than the parsed size of the {name}");
                    }

                    wasmFile.PopParser();
                }
            }
        }

        internal Segment StartSegment(string name)
        {
            uint size = Parser.LEB128(new UInt(32)).Value;
            if (size > int.MaxValue)
            {
                throw new ParseException($"{name} too large for this implementation: {size}");
            }

            if (PushParser((int)size))
            {
                return new Segment(this, (int)size, name);
            }

            throw new ParseException($"{name} size larger than the WASM binary: {size}");
        }

        internal CustomSection LoadCustomSection(int sectionSize)
        {
            int start = Parser.Index;
            string name = Name();
            int size = sectionSize - (Parser.Index - start);

            byte[] contents;
            try
            {
                contents = Parser.NextBytes(size);
            }
            catch (Exception e) when (e is not ParseException)
            {
                throw new ParseException($"Unable to allocate memory for section with size: {size}", e);
            }

            // TODO: Parse contents of known custom sections
            // never throw an exception! "If an implementation interprets the data of a custom section, then errors in that data, or the placement of the section, must not invalidate the module."

            return new CustomSection.Unknown(name, contents);
        }

        internal bool TrySectionId(out SectionId secId)
        {
            if (Parser.TryNextByte(out byte id))
            {
                secId = (SectionId)id;
                return secId < SectionId._Max ? true : throw new ParseException($"Unknown section ID: {id}");
            }

            secId = default;
            return false;
        }

        internal Global ParseGlobal() => new Global(ParseGlobalType(), ParseExpression());

        internal Export ParseExport()
        {
            var name = Name();
            var type = (ExportType)Parser.NextByte();
            if(!Enum.IsDefined(type))
            {
                throw new ParseException($"Unknown export type: {type}");
            }
            return new Export(name, type, Parser.LEB128(new UInt(32)).Value);
        }

        internal Element ParseElement() => new Element(Parser.LEB128(new UInt(32)).Value, ParseExpression(), Vector(() => Parser.LEB128(new UInt(32)).Value));

        internal (int count, WasmValueType type) ParseLocal()
        {
            uint count = Parser.LEB128(new UInt(32)).Value;
            if(count > int.MaxValue)
            {
                throw new ParseException($"Local count too large for this implementation: {count}");
            }
            return ((int)count, ValueType());
        }

        internal Function ParseFunction()
        {
            using var segment = StartSegment("function");

            IReadOnlyList<WasmValueType> locals;
            try
            {
                locals = Vector(ParseLocal).SelectMany(l => Enumerable.Repeat(l.type, l.count)).ToList();
            }
            catch (Exception e) when (e is not ParseException)
            {
                throw new ParseException($"Unable to build locals list, see inner exception for details", e);
            }

            return new Function(locals, ParseExpression());
        }

        internal Data ParseData() => new Data(Parser.LEB128(new UInt(32)).Value, ParseExpression(), ByteVector());
    }
}