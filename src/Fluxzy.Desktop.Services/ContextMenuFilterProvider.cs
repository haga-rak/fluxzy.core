// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Extensions;
using Fluxzy.Readers;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Utils;

namespace Fluxzy.Desktop.Services
{
    public class ContextMenuFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ExchangeInfo exchange, IArchiveReader archiveReader)
        {
            // Filter by hostname 

            var authority = exchange.RequestHeader.Authority.ToString();

            yield return new HostFilter(authority,
                StringSelectorOperation.Exact) {
                Description = $"Host : {authority}"
            };

            if (SubdomainUtility.TryGetSubDomain(authority, out var subDomain)) {
                yield return new HostFilter(subDomain!,
                    StringSelectorOperation.EndsWith) {
                    Description = $"Subdomain : «*.{subDomain}»"
                };
            }

            yield return new MethodFilter(exchange.Method)
            {
                Description = $"{exchange.Method} request"
            };

            if (Uri.TryCreate(exchange.FullUrl, UriKind.Absolute, out var absoluteUri) &&
                absoluteUri.AbsolutePath.Length > 3)
                yield return new PathFilter(absoluteUri.AbsolutePath, StringSelectorOperation.Contains);

            if (exchange.ResponseHeader != null) {
                var contentTypeHeader = exchange.GetResponseHeaderValue("content-type");

                if (contentTypeHeader != null)
                    yield return new ResponseHeaderFilter(contentTypeHeader, "Content-Type");
            }

            if (!string.IsNullOrWhiteSpace(exchange.Comment)) {
                yield return new CommentSearchFilter(exchange.Comment!) {
                    Description = $"Comment contains  «{exchange.Comment}»"
                };

                yield return new HasCommentFilter();
            }

            if (exchange.Tags.Any()) {
                yield return new HasTagFilter();

                foreach (var tag in exchange.Tags.Take(5)) {
                    yield return new TagContainsFilter(tag) {
                        Description = $"Has tag «{tag.Value}»"
                    };
                }
            }
        }
    }
}
