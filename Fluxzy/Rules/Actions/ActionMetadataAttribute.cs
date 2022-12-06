// Copyright © 2022 Haga RAKOTOHARIVELO

using System;

namespace Fluxzy.Rules.Actions
{
    public class ActionMetadataAttribute : Attribute
    {
        public ActionMetadataAttribute(string longDescription)
        {
            LongDescription = longDescription;
        }

        public string LongDescription { get; }
    }
}
