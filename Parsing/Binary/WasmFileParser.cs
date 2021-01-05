using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASM_To_MC.Shared;

namespace WASM_To_MC.Parsing.Binary
{
    public partial class WasmFileParser
    {
        public static readonly byte[] Magic = Encoding.ASCII.GetBytes("\0asm");
        public static readonly int MinSize = Magic.Length + sizeof(int);

        private const string CodeLengthNotEqualToFuncLengthMsg = "'Code' section must have the same number of elements as the 'Function' section";

        public Module Parse()
        {
            if(Parser.MaxIndex < MinSize)
            {
                throw new ParseException($"WASM binary too small, must be at least {MinSize} bytes");
            }

            byte[] magic = Parser.NextBytes(Magic.Length);
            if(!magic.SequenceEqual(Magic))
            {
                throw new ParseException($"Incorrect magic number for WASM binary files, expected {BitConverter.ToString(Magic).Replace('-', ' ')}, got {BitConverter.ToString(magic).Replace('-', ' ')}");
            }

            byte[] versionBytes = Parser.NextBytes(4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(versionBytes);
            }

            int version = BitConverter.ToInt32(versionBytes);
            if(version != 1)
            {
                throw new ParseException($"Unsupported WASM binary version: {version}");
            }

            var customSections = new CustomSections();
            IReadOnlyList<FunctionType>? types = null;
            IReadOnlyList<Import>? imports = null;
            IReadOnlyList<uint>? functions = null;
            IReadOnlyList<TableType>? tables = null;
            IReadOnlyList<Limits>? memories = null;
            IReadOnlyList<Global>? globals = null;
            IReadOnlyList<Export>? exports = null;
            uint? start = null;
            IReadOnlyList<Element>? elements = null;
            IReadOnlyList<Function>? code = null;
            IReadOnlyList<Data>? data = null;

            SectionId? prevSection = null;
            while(TrySectionId(out SectionId sectionId))
            {
                if(sectionId != SectionId.Custom)
                {
                    if (prevSection.HasValue && sectionId <= prevSection.Value)
                    {
                        throw new ParseException($"Sections are out of order, section {sectionId} must come before {prevSection.Value}");
                    }

                    prevSection = sectionId;
                    customSections.NextSection(sectionId);
                }

                using var section = StartSegment("section");

                switch (sectionId)
                {
                    case SectionId.Custom:
                        customSections.Add(LoadCustomSection(section.Size));
                        break;
                    case SectionId.Type:
                        types = Vector(FuncType);
                        break;
                    case SectionId.Import:
                        imports = Vector(ParseImport);
                        break;
                    case SectionId.Function:
                        functions = Vector(() => Parser.LEB128(new UInt(32)).Value);
                        break;
                    case SectionId.Table:
                        tables = Vector(ParseTableType);
                        break;
                    case SectionId.Memory:
                        memories = Vector(ParseLimits);
                        break;
                    case SectionId.Global:
                        globals = Vector(ParseGlobal);
                        break;
                    case SectionId.Export:
                        exports = Vector(ParseExport);
                        break;
                    case SectionId.Start:
                        start = Parser.LEB128(new UInt(32)).Value;
                        break;
                    case SectionId.Element:
                        elements = Vector(ParseElement);
                        break;
                    case SectionId.Code:
                        if(functions is null)
                        {
                            throw new ParseException(CodeLengthNotEqualToFuncLengthMsg);
                        }

                        code = Vector(ParseFunction);
                        break;
                    case SectionId.Data:
                        data = Vector(ParseData);
                        break;
                    case SectionId._Max:
                        break;
                }
            }

            customSections.Finish();
            
            return new Module(
                types ?? Array.Empty<FunctionType>(),
                imports ?? Array.Empty<Import>(),
                functions ?? Array.Empty<uint>(),
                tables ?? Array.Empty<TableType>(),
                memories ?? Array.Empty<Limits>(),
                globals ?? Array.Empty<Global>(),
                exports ?? Array.Empty<Export>(),
                start,
                elements ?? Array.Empty<Element>(),
                code ?? Array.Empty<Function>(),
                data ?? Array.Empty<Data>(),
                customSections
            );
        }
    }
}
