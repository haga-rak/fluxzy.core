using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Formatters.Producers.Requests;

namespace Fluxzy
{
    public class CookieFlowAnalyzer
    {
        public CookieFlow Execute(string cookieName,
            string domain,
            string? path,
            IReadOnlyCollection<ExchangeInfo> exchanges)
        {
            path ??= "/";

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            var events = new List<CookieTrackingEvent>();

            foreach (var exchange in exchanges)
            {
                if (!Uri.TryCreate(exchange.FullUrl, UriKind.Absolute, out var uri))
                    continue;

                var previous = events.LastOrDefault();

                if (!uri.Host.EndsWith(domain))
                {
                    continue;
                }

                if (path != "/" && !uri.PathAndQuery.StartsWith(path))
                {
                    continue;
                }

                var foundInClient = false;

                foreach (var requestCookie in
                         HttpHelper
                             .ReadRequestCookies(exchange)
                             .Where(r => r.Name == cookieName))
                {

                    foundInClient = true;

                    var tracingEvent = new CookieTrackingEvent(previous == null ?
                            CookieUpdateType.AddedFromClient : 
                            previous.Value == requestCookie.Value ?
                                CookieUpdateType.None : CookieUpdateType.UpdatedFromClient
                        , requestCookie.Value, exchange);

                    events.Add(tracingEvent);
                }

                if (!foundInClient && previous != null && previous.UpdateType != CookieUpdateType.RemovedByClient
                    && previous.UpdateType != CookieUpdateType.RemovedByServer)
                {
                    events.Add(new CookieTrackingEvent(CookieUpdateType.RemovedByClient,
                        string.Empty, exchange));
                }

                foreach (var setCookieItem in
                         HttpHelper
                             .ReadResponseCookies(exchange)
                             .Where(r => r.Name == cookieName))
                {
                    var updateType = previous == null ?
                        CookieUpdateType.AddedFromServer : CookieUpdateType.UpdatedFromServer;

                    bool expires = setCookieItem.Expired != default &&
                                   exchange.Metrics.RequestHeaderSending > setCookieItem.Expired
                                   || setCookieItem.MaxAge != null && setCookieItem.MaxAge <= 0;

                    if (expires)
                    {
                        updateType = CookieUpdateType.RemovedByServer;
                    }

                    var tracingEvent = new CookieTrackingEvent(updateType,
                        setCookieItem.Value, exchange)
                    {
                        SetCookieItem = setCookieItem
                    };

                    events.Add(tracingEvent);
                }
            }

            return new CookieFlow(cookieName, domain, events);
        }
    }
}