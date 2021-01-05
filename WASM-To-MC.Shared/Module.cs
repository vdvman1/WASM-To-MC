using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public record Module(
        IReadOnlyList<FunctionType> Types,
        IReadOnlyList<Import> Imports,
        IReadOnlyList<uint> Functions,
        IReadOnlyList<TableType> Tables,
        IReadOnlyList<Limits> Memories,
        IReadOnlyList<Global> Globals,
        IReadOnlyList<Export> Exports,
        uint? Start,
        IReadOnlyList<Element> Elements,
        IReadOnlyList<Function> Code,
        IReadOnlyList<Data> Data,
        CustomSections CustomSections
    ) {}
}
