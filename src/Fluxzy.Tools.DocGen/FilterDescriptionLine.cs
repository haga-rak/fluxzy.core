// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Tools.DocGen
{
    public class FilterDescriptionLine
    {
        public FilterDescriptionLine(string propertyName, string type, string description, string defaultValue)
        {
            PropertyName = propertyName;
            Type = type;
            Description = description;
            DefaultValue = defaultValue;
        }

        public string PropertyName { get;  }

        public string Type { get;  } 

        public string Description { get;  }

        public string DefaultValue { get;  }
    }
}
