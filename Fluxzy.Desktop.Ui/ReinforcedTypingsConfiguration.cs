// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using Fluxzy.Clients;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Services.Rules;
using Fluxzy.Desktop.Ui.ViewModels;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Formatters.Producers.Responses;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;
using Action = Fluxzy.Rules.Action;
using ConfigurationBuilder = Reinforced.Typings.Fluent.ConfigurationBuilder;

[assembly: TsGlobal(CamelCaseForProperties = true, AutoOptionalProperties = true)]

namespace Fluxzy.Desktop.Ui
{
    // ReSharper disable once IdentifierTypo - Loaded by reflections
    public static class ReinforcedTypingsConfiguration
    {
        public static void Configure(ConfigurationBuilder builder)
        {
            builder.Global(config => config.CamelCaseForProperties()
                .AutoOptionalProperties()
                .UseModules());

            builder.ExportAsInterface<PolymorphicObject>()
                   .ApplyGenericProperties();

            ConfigureViewModels(builder);
            ConfigureProducers(builder);
            ConfigureFilters(builder); 
            ConfigureRules(builder); 

            // UI objects

            builder.ExportAsInterface<UiState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ForwardMessage>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ProxyState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ProxyEndPoint>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ProxyBindPoint>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ArchivingPolicy>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<FileState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ViewFilter>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ToolBarFilter>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<FilteredExchangeState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<FluxzySettingsHolder>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<FluxzySetting>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ExchangeState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ExchangeBrowsingState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ExchangeContainer>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ConnectionContainer>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<TrunkState>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ContextMenuAction>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<FileContentDelete>()
                .ApplyGenericProperties();

            // Core objects 

            builder.ExportAsInterface<ArchiveMetaInformation>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<Tag>()
                .ApplyGenericProperties();


            builder.ExportAsInterface<ExchangeInfo>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<RequestHeaderInfo>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ResponseHeaderInfo>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ExchangeMetrics>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<HeaderFieldInfo>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<ConnectionInfo>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<AuthorityInfo>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<SslInfo>()
                .ApplyGenericProperties();
        }


        private static void ConfigureViewModels(ConfigurationBuilder builder)
        {

            builder.ExportAsInterface<FileSaveViewModel>()
                   .ApplyGenericProperties();
        }
        private static void ConfigureProducers(ConfigurationBuilder builder)
        {
            builder.ExportAsInterface<ExchangeContextInfo>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<FormattingResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<AuthorizationBasicResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<AuthorizationBearerResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<AuthorizationResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<QueryStringResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<RequestCookieResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<RequestJsonResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<RawRequestHeaderResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<RequestTextBodyResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<RequestBodyAnalysisResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<FormUrlEncodedResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<FormUrlEncodedItem>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<MultipartFormContentResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<MultipartItem>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<SaveFileMultipartActionModel>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<FormatterContainerViewModel>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<ResponseBodySummaryResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<ResponseTextContentResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<ResponseJsonResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<SetCookieResult>()
                   .ApplyGenericProperties();

            builder.ExportAsInterface<SetCookieItem>()
                   .ApplyGenericProperties();
        }

        private static void ConfigureFilters(ConfigurationBuilder builder)
        {
            builder.ExportAsInterface<TemplateToolBarFilterModel>().ApplyGenericProperties();
            builder.ExportAsInterface<FilterTemplate>().ApplyGenericProperties();

            builder.ExportAsInterface<StoredFilter>().ApplyGenericProperties();

            var foundTypes = typeof(Filter).Assembly.GetTypes()
                                      .Where(derivedType => typeof(Filter).IsAssignableFrom(derivedType)
                                                            && derivedType.IsClass).ToList();


            builder.ExportAsInterfaces(foundTypes, a => a.ApplyGenericPropertiesGeneric());
        }
        
        private static void ConfigureRules(ConfigurationBuilder builder)
        {
            builder.ExportAsInterface<Rule>().ApplyGenericProperties();
            builder.ExportAsInterface<RuleContainer>().ApplyGenericProperties();
            builder.ExportAsInterface<Certificate>().ApplyGenericProperties();

            var foundTypes = typeof(Action).Assembly.GetTypes()
                           .Where(derivedType => typeof(Action).IsAssignableFrom(derivedType)
                                                 && derivedType.IsClass).ToList();

            builder.ExportAsInterfaces(foundTypes, a => a.ApplyGenericPropertiesGeneric());
        }
    }

    internal static class ReinforceExtensions
    {
        public static InterfaceExportBuilder<T> ApplyGenericProperties<T>(this InterfaceExportBuilder<T> builder)
        {
            var result =  builder
                .Substitute(typeof(DateTime), new RtSimpleTypeName("Date"))
                .Substitute(typeof(Guid), new RtSimpleTypeName("string"))
                .Substitute(typeof(ReadOnlyMemory<char>), new RtSimpleTypeName("string"))
                .Substitute(typeof(IPAddress), new RtSimpleTypeName("string"))
                .Substitute(typeof(StoreLocation), new RtSimpleTypeName("string"))
                .Substitute(typeof(StringSelectorOperation), new RtSimpleTypeName("string"))
                .Substitute(typeof(HashSet<int>), new RtSimpleTypeName("Set<number>"))
                .DontIncludeToNamespace()
                .AutoI(false)
                .WithPublicProperties();

            return result; 
        }

        public static InterfaceExportBuilder ApplyGenericPropertiesGeneric(this InterfaceExportBuilder builder)
        {
            var result =  builder
                .Substitute(typeof(DateTime), new RtSimpleTypeName("Date"))
                .Substitute(typeof(Guid), new RtSimpleTypeName("string"))
                .Substitute(typeof(ReadOnlyMemory<char>), new RtSimpleTypeName("string"))
                .Substitute(typeof(IPAddress), new RtSimpleTypeName("string"))
                .Substitute(typeof(StoreLocation), new RtSimpleTypeName("string"))
                .Substitute(typeof(StringSelectorOperation), new RtSimpleTypeName("string"))
                .Substitute(typeof(HashSet<int>), new RtSimpleTypeName("Set<number>"))
                .DontIncludeToNamespace()
                .AutoI(false)
                .WithPublicProperties();

            return result; 
        }

        public static InterfaceExportBuilder ExportAsInterface(this InterfaceExportBuilder builder, Type type )
        {
            var staticClassInfo = typeof(TypeConfigurationBuilderExtensions);
            var methodInfo = staticClassInfo.GetMethod(nameof(ExportAsInterface), 
                System.Reflection.BindingFlags.Static)!;

            var genericMethod = methodInfo.MakeGenericMethod(type);
            genericMethod.Invoke(null, null);
            
            return builder;
        }
    }
}