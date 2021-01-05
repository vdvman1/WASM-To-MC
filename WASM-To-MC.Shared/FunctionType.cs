using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public record FunctionType(IReadOnlyList<WasmValueType> Inputs, IReadOnlyList<WasmValueType> Outputs) {}
}
