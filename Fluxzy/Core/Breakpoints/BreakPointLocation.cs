// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Fluxzy.Core.Breakpoints
{
    public enum BreakPointLocation
    {
        Start = 0,
        [Description("After authority received")]
        ReceivingAuthority = 1,

        [Description("Before sending request")]
        WaitingRequest,

        [Description("Solving DNS")]
        WaitingEndPoint,

        [Description("Before sending response")]
        WaitingResponse
    }

    public static class DescriptionHelper
    {
        public static string? GetEnumDescription(this Enum value)
        {
            var type = value.GetType();
            var memberInfos = type.GetMember(value.ToString());
            var attribute = memberInfos.FirstOrDefault()?.GetCustomAttribute<DescriptionAttribute>(); 
            return attribute?.Description;
        }
    }
}
