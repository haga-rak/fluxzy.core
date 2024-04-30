using System.Collections.Generic;

namespace Fluxzy.Clipboard
{
    public class ExchangeData : CopyableData
    {
        public ExchangeData(int id, List<CopyArtefact> artefacts)
            : base(id, artefacts)
        {
        }
    }
}