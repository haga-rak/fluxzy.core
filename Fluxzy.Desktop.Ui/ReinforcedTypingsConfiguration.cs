// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using Fluxzy.Clients;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Ui.ViewModels;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;
using ConfigurationBuilder = Reinforced.Typings.Fluent.ConfigurationBuilder;

[assembly: TsGlobal(CamelCaseForProperties = true, AutoOptionalProperties = true)]

namespace Fluxzy.Desktop.Ui
{
    public static class ReinforcedTypingsConfiguration
    {
        private static void ConfigureViewModels(ConfigurationBuilder builder)
        {
            builder.ExportAsInterface<FileOpeningViewModel>()
                .ApplyGenericProperties();

            builder.ExportAsInterface<FileSaveViewModel>()
                .ApplyGenericProperties();
        }

        public static void Configure(ConfigurationBuilder builder)
        {
            builder.Global(config => config.CamelCaseForProperties()
                .AutoOptionalProperties()
                .UseModules());


            ConfigureViewModels(builder); 

            // UI objects

            builder.ExportAsInterface<UiState>()
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

            builder.ExportAsInterface<FileContentDelete>()
                .ApplyGenericProperties();

            // Core objects 


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
                .DontIncludeToNamespace()
                .AutoI(false)
                .WithPublicProperties();

            return result; 
        }
    }
}