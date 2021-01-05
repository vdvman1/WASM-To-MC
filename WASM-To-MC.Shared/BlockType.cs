using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    // Discriminated union pattern
    public record BlockType
    {
        protected BlockType() { }

        public static readonly BlockType Empty = new BlockType();
        public record ValueType(WasmValueType Type) : BlockType { }
        public record Index(uint TypeIndex) : BlockType {}
    }
}
