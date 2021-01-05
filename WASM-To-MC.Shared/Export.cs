using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public record Export(string Name, ExportType Type, uint Index) { }

    public enum ExportType : byte
    {
        Function,
        Table,
        Memory,
        Global
    }
}
