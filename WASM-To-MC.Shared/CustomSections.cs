using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public class CustomSections
    {
        private List<CustomSection>? currentSections = new List<CustomSection>();
        private readonly List<(SectionId End, IReadOnlyList<CustomSection> Sections)> allSections = new();

        public void NextSection(SectionId id)
        {
            if(currentSections is null)
            {
                throw new InvalidOperationException("Tried to move to the next section after all sections have been processed");
            }

            if(allSections.Count > 0 && id <= allSections[^1].End)
            {
                throw new InvalidOperationException($"Tried to process sections out of order: {id} must come after {allSections[^1].End}");
            }

            allSections.Add((id, currentSections));
            currentSections = new();
        }

        public void Finish()
        {
            if (currentSections == null) return;

            allSections.Add((SectionId._Max, currentSections));
            currentSections = null;
        }

        public void Add(CustomSection section)
        {
            if(currentSections is null)
            {
                throw new InvalidOperationException("Tried to add a new custom section after all sections have been processed");
            }

            currentSections.Add(section);
        }

        // TODO: Provide read access to the custom sections
    }

    public enum SectionId : byte
    {
        Custom,
        Type,
        Import,
        Function,
        Table,
        Memory,
        Global,
        Export,
        Start,
        Element,
        Code,
        Data,
        _Max
    }
}
