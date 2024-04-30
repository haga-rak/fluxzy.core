using System.Collections.Generic;

namespace Fluxzy.Clipboard
{
    public class ConnectionData : CopyableData
    {
        public ConnectionData(int id, List<CopyArtefact> artefacts)
            : base(id, artefacts)
        {
        }
    }
}