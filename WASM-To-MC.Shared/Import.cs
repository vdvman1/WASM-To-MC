using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public record Import(string Module, string Name, ImportDescription Description) {}

    // Discriminated union pattern
    public record ImportDescription
    {
        protected ImportDescription() { }

        public record Func(uint Index) : ImportDescription { }
        public record Table(TableType Type) : ImportDescription { }
        public record Mem(Limits Limits) : ImportDescription { }
        public record Global(GlobalType Type) : ImportDescription { }
    }
}
