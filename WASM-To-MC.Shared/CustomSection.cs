using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    // Discriminated union pattern
    public abstract record CustomSection
    {
        public string Name { get; init; }

        protected CustomSection(string name)
        {
            Name = name;
        }

        public void Deconstruct(out string name) => name = Name;

        // TODO: Add supported custom sections
        public record Unknown(string Name, byte[] Contents) : CustomSection(Name) { }
    }
}
