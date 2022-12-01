// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;

namespace Fluxzy.Rules.Filters
{
    public class FilterMetaDataAttribute : Attribute
    {

        public FilterMetaDataAttribute(string? longDescription = null)
        {
            LongDescription = longDescription;
        }

        /// <summary>
        /// Long description of the filter : eg : Documentation
        /// </summary>
        public string? LongDescription { get; init; }

        /// <summary>
        /// The filter shall be present on toolbar 
        /// </summary>
        public bool ToolBarFilter { get; init; }

        /// <summary>
        /// The order of apperance on the toolbar
        /// </summary>
        public int ToolBarFilterOrder { get; init;  }

        /// <summary>
        /// Quickly reachable filter 
        /// </summary>
        public bool QuickReachFilter { get; set; } 

        /// <summary>
        /// Order 
        /// </summary>
        public int QuickReachFilterOrder { get; init;  }

        /// <summary>
        /// The filter shall not be saved in last used filter history. 
        /// </summary>
        public bool DoNotHistorize { get; init; }

    }
}