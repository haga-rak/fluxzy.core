using System.Collections.Generic;

namespace Fluxzy.Clipboard
{
    public class CopyableData
    {
        public CopyableData(int id, List<CopyArtefact> artefacts)
        {
            Id = id;
            Artefacts = artefacts;
        }

        public int Id { get; }

        public List<CopyArtefact> Artefacts { get; }
    }
}