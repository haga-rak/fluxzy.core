// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Desktop.Services.ContextualFilters
{
    public class QuickActionBuilder : ObservableProvider<QuickActionResult>
    {
        private readonly ToolBarFilterProvider _toolBarFilterProvider;

        protected override BehaviorSubject<QuickActionResult> Subject { get; } = new(new(new())); 

        public QuickActionBuilder(IObservable<TrunkState> trunkStateObservable,
            IObservable<ContextualFilterResult> contextFilterResult, ToolBarFilterProvider toolBarFilterProvider)
        {
            _toolBarFilterProvider = toolBarFilterProvider;

            trunkStateObservable.CombineLatest(
                                    contextFilterResult)
                                .Select(t => Build(t.First, t.Second))
                                .Do(t => Subject.OnNext(t))
                                .Subscribe();
        }
        
        private QuickActionResult Build(TrunkState _, ContextualFilterResult contextFilterResult)
        {

            var quickActions = contextFilterResult.ContextualFilters
                                                  .Select(s =>
                                                      new QuickAction(s.Filter.Identifier.ToString(),
                                                          "Filter",
                                                          s.Filter.FriendlyName,
                                                          false,
                                                          new QuickActionPayload(s.Filter), QuickActionType.Filter)).ToList();

            return new QuickActionResult(quickActions);
        }
        
        /// <summary>
        /// Actions that are unchanged 
        /// </summary>
        /// <returns></returns>
        public QuickActionResult GetStaticQuickActions()
        {
            var listActions = new List<QuickAction>();

            listActions.Add(BuildFromFilter(new ContentTypeJsonFilter()));
            listActions.Add(BuildFromFilter(new ContentTypeXmlFilter()));
            listActions.Add(BuildFromFilter(new ImageFilter(), "png", "jpeg", "jpg", "gif", "bitmap", "svg"));
            listActions.Add(BuildFromFilter(new CssStyleFilter(), "sass", "scss", "cascading", "sheet"));

            var allHttpMethods = new string[] {"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"};

            foreach (var method in allHttpMethods) {
                listActions.Add(BuildFromFilter(new MethodFilter(method)));
            }

            listActions.Add(BuildFromFilter(new NetworkErrorFilter()));
            listActions.Add(BuildFromFilter(new StatusCodeSuccessFilter(), "200", "201", "204"));
            listActions.Add(BuildFromFilter(new StatusCodeRedirectionFilter(), "301", "302", "307"));
            listActions.Add(BuildFromFilter(new StatusCodeClientErrorFilter(), Enumerable.Range(400,100).Select(s => s.ToString()).ToArray()));
            listActions.Add(BuildFromFilter(new StatusCodeServerErrorFilter(), Enumerable.Range(500,100).Select(s => s.ToString()).ToArray()));
            listActions.Add(BuildFromFilter(new IsWebSocketFilter()));
            listActions.Add(BuildFromFilter(new HasCommentFilter()));
            listActions.Add(BuildFromFilter(new HasTagFilter()));
            listActions.Add(BuildFromFilter(new HasAnyCookieOnRequestFilter()));
            listActions.Add(BuildFromFilter(new HasCookieOnRequestFilter("cookie_name", "")));

            listActions.Add(BuildFromFilter(new FontFilter(), "woff", "ttf"));

            listActions.Add(BuildFromAction(new ForceHttp11Action()));
            listActions.Add(BuildFromAction(new ForceHttp2Action()));
            listActions.Add(BuildFromAction(new AddRequestHeaderAction(string.Empty, string.Empty)));
            listActions.Add(BuildFromAction(new AddResponseHeaderAction(string.Empty, string.Empty)));
            listActions.Add(BuildFromAction(new ApplyCommentAction(string.Empty)));
            listActions.Add(BuildFromAction(new ApplyTagAction()));
            listActions.Add(BuildFromAction(new DeleteRequestHeaderAction(""), "delete"));
            listActions.Add(BuildFromAction(new DeleteResponseHeaderAction(""), "delete"));
            listActions.Add(BuildFromAction(new RemoveCacheAction()));
            listActions.Add(BuildFromAction(new SetClientCertificateAction(null)));
            listActions.Add(BuildFromAction(new SkipRemoteCertificateValidationAction()));
            listActions.Add(BuildFromAction(new SpoofDnsAction()));
            listActions.Add(BuildFromAction(new UpdateRequestHeaderAction(string.Empty, string.Empty)));
            listActions.Add(BuildFromAction(new UpdateResponseHeaderAction(string.Empty, string.Empty)));
            
            // listActions.Add(BuildFromFilter(new ClientEr));

            return new QuickActionResult(listActions); 
        }

        private static QuickAction BuildFromFilter(Filter filter, params string[] keywords)
        {
            return new QuickAction(filter.Identifier.ToString(),
                "Filter",
                filter.FriendlyName,
                false,
                new QuickActionPayload(filter), QuickActionType.Filter) {
                Keywords = keywords
            };
        }

        public static QuickAction BuildFromAction(Fluxzy.Rules.Action action, params string[] keywords)
        {
            return new QuickAction(action.Identifier.ToString(),
                               "Action",
                                              action.FriendlyName,
                                              false,
                                              new QuickActionPayload(action)  , QuickActionType.Action) {
                Keywords = keywords
            };
        }
    }
}
