using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public record Global(GlobalType Type, IReadOnlyList<Instruction> Init) { }

    public record GlobalType(WasmValueType Type, bool Mutable) { }
}
