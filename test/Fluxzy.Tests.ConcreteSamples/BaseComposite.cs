// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Tests.ConcreteSamples
{
    public abstract class BaseComposite
    {
        /// <summary>
        /// Default args scommand for testing 
        /// </summary>
        public string DefaultCommandLine { get; } = "start --llo --rule ";

        /// <summary>
        /// Describe what's going to be tested 
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Describe the yaml configuration used by this test 
        /// </summary>
        public abstract string Configuration { get;  }


        public virtual string LongDescription { get;  } = string.Empty;
    }
}
