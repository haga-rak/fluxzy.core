// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy;
using Fluxzy.Clients;
using Fluxzy.Desktop.Services;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;
using ConfigurationBuilder = Reinforced.Typings.Fluent.ConfigurationBuilder;
using ConnectionInfo = Fluxzy.ConnectionInfo;

[assembly: TsGlobal(CamelCaseForProperties = true, AutoOptionalProperties = true)]

namespace Echoes.Desktop.Ui
{
    public static class ReinforcedTypingsConfiguration
    {
        public static void Configure(ConfigurationBuilder builder)
        {
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
                .DontIncludeToNamespace()
                .AutoI(false)
                .WithPublicProperties();

            return result; 
        }
    }
}