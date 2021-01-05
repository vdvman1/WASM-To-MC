using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public record TableType(ElementType ElType, Limits Limits) {}

    public enum ElementType : byte
    {
        _Min = 0x6F,
        FuncRef,
        _Max
    }
}
