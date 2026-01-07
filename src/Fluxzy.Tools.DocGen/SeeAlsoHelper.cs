// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Tools.DocGen
{
    public static class SeeAlsoHelper
    {
        public static List<HashSet<Type>> FilterCategories { get; } = new();
        public static List<HashSet<Type>> ActionCategories { get; } = new();

        static SeeAlsoHelper()
        {
            InitSeeAlsoFilters();
            InitSeeAlsoActions();
        }

        private static void InitSeeAlsoFilters()
        {
            FilterCategories.Add(
                new() {
                    typeof(MethodFilter), typeof(GetFilter), typeof(PostFilter),
                    typeof(PutFilter), typeof(DeleteFilter), typeof(PatchFilter)
                });

            FilterCategories.Add(
                new() {
                    typeof(AgentFilter), typeof(HasCommentFilter), typeof(CommentSearchFilter)
                });

            FilterCategories.Add(
                new() {
                    typeof(AbsoluteUriFilter), typeof(HostFilter), typeof(AuthorityFilter),
                    typeof(PathFilter), typeof(QueryStringFilter), typeof(RequestHeaderFilter)
                });

            FilterCategories.Add(
                new() {
                    typeof(HasAnyCookieOnRequestFilter), typeof(HasCookieOnRequestFilter), 
                    typeof(HasSetCookieOnResponseFilter)
                });

            FilterCategories.Add(
                new() {
                    typeof(HasAuthorizationFilter), typeof(HasAuthorizationBearerFilter), 
                    typeof(HasSetCookieOnResponseFilter), typeof(RequestHeaderFilter)
                });

            FilterCategories.Add(
                new() {
                    typeof(H11TrafficOnlyFilter), typeof(H2TrafficOnlyFilter),
                });

            FilterCategories.Add(
                new() {
                    typeof(StatusCodeFilter), typeof(StatusCodeSuccessFilter),
                    typeof(StatusCodeRedirectionFilter), typeof(StatusCodeClientErrorFilter),
                    typeof(StatusCodeServerErrorFilter)
                });

            FilterCategories.Add(
                new() {
                    typeof(CssStyleFilter), typeof(ContentTypeXmlFilter), typeof(FontFilter),
                    typeof(JsonRequestFilter), typeof(JsonResponseFilter), typeof(ImageFilter), 
                    typeof(HtmlResponseFilter)
                });
        }

        private static void InitSeeAlsoActions()
        {
            ActionCategories.Add(
                new() {
                    typeof(AddRequestHeaderAction), typeof(AddResponseHeaderAction),
                    typeof(UpdateRequestHeaderAction), typeof(UpdateResponseHeaderAction),
                    typeof(DeleteRequestHeaderAction), typeof(DeleteResponseHeaderAction),
                    typeof(SetUserAgentAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(ApplyCommentAction), typeof(ApplyTagAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(ChangeRequestMethodAction), typeof(ChangeRequestPathAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(ForceHttp11Action), typeof(ForceHttp2Action), typeof(ForceTlsVersionAction), 
                    typeof(SetClientCertificateAction), typeof(SkipSslTunnelingAction), typeof(UseDnsOverHttpsAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(ForwardAction), typeof(SpoofDnsAction),
                    typeof(ServeDirectoryAction),
                    typeof(MockedResponseAction), typeof(InjectHtmlTagAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(SetRequestCookieAction), typeof(SetResponseCookieAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(FileAppendAction), typeof(StdOutAction), typeof(StdErrAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(MountCertificateAuthorityAction), typeof(MountWelcomePageAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(AbortAction), 
                    typeof(RejectAction), 
                    typeof(RejectWithMessageAction),
                    typeof(RejectWithStatusCodeAction),
                    typeof(MockedResponseAction)
                });

            ActionCategories.Add(
                new() {
                    typeof(ApplySessionAction), 
                    typeof(CaptureSessionAction), 
                    typeof(ClearSessionAction)
                });
        }

        public static IEnumerable<Type> GetSeeAlsoFilters(Type type)
        {
            foreach (var category in FilterCategories) {
                if (category.Contains(type))
                    return category;
            }

            return Array.Empty<Type>();
        }

        public static IEnumerable<Type> GetSeeAlsoActions(Type type)
        {
            foreach (var category in ActionCategories) {
                if (category.Contains(type))
                    return category;
            }

            return Array.Empty<Type>();
        }
    }
}
