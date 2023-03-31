// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.ContextualFilters
{
    public class QuickActionResult
    {
        public QuickActionResult(List<QuickAction> actions)
        {
            Actions = actions;
        }

        public List<QuickAction> Actions { get;  }
    }

    
    public class QuickAction
    {
        public QuickAction(string id, 
            string category, 
            string label, 
            bool needExchangeId,
            QuickActionPayload quickActionPayload, QuickActionType type)
        {
            Id = id;
            Category = category;
            Label = label;
            NeedExchangeId = needExchangeId;
            QuickActionPayload = quickActionPayload;
            Type = type;
        }

        /// <summary>
        /// Unique id of the action 
        /// </summary>
        public string Id { get; }

        public QuickActionType Type { get; }

        /// <summary>
        /// Name 
        /// </summary>
        public string Category { get; }
        

        public string Label { get;  }

        /// <summary>
        /// 
        /// </summary>
        public string[] IconClass { get; set; } = new string[0];

        /// <summary>
        /// 
        /// </summary>
        public string[] OtherClasses { get; set; } = new string[0];

        /// <summary>
        /// 
        /// </summary>
        public bool NeedExchangeId { get;  }

        public QuickActionPayload QuickActionPayload { get;  }

        public List<string> Keywords { get; } = new();
    }


    public enum QuickActionType
    {
        BackendCall,
        ClientOperation,
        Filter
    }

    public class QuickActionPayload
    {
        public QuickActionPayload(Filter? filter)
        {
            Filter = filter;
        }

        public Filter? Filter { get; }
    }
}
