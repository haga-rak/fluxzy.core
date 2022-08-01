﻿using System;
using Fluxzy.Clients;

namespace Fluxzy.Rules
{
    public class ByRequestRule
    {
        public ByRequestRule(Func<Request, bool> filter, ReplyContent replyContent)
        {
            Filter = filter;
            ReplyContent = replyContent;
        }

        public Func<Request, bool> Filter { get; }

        public ReplyContent ReplyContent { get; }
    }
}