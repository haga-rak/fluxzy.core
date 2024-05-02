// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Build
{
    internal static class Targets
    {
        public static string RestoreTests => "restore-tests";
        public static string RestoreFluxzyCore => "restore-fluxzy-core";
        public static string BuildFluxzyCore => "build-fluxzy-core";
        public static string BuildTests => "build-tests";
        public static string ValidateNugetToken => "validate-nuget-token";
        public static string ValidatePartnerSecret => "validate-partner-secret";
        public static string MustBeRelease => "must-be-release";
        public static string MustNotBeRelease => "must-not-be-release";
        public static string InstallTools => "install-tools";
        public static string FluxzyCoreCreatePackage => "fluxzy-core-create-package";
        public static string FluxzyCorePcapCreatePackage => "fluxzy-core-pcap-create-package";
        public static string FluxzyPackageSign => "fluxzy-package-sign";
        public static string FluxzyPackagePushGithub => "fluxzy-package-push-github";
        public static string FluxzyPackagePushPartner => "fluxzy-package-push-partner";
        public static string FluxzyPackagePushPublicInternal => "fluxzy-package-push-public-internal";
        public static string FluxzyCliPackageBuild => "fluxzy-cli-package-build";
        public static string FluxzyCliPackageZip => "fluxzy-cli-package-zip";
        public static string FluxzyCliPublishInternal => "fluxzy-cli-publish-internal";
        public static string FluxzyCliPackageSign => "fluxzy-cli-package-sign";
        public static string Docs => "docs";
        public static string Default => "default";
        public static string ValidateMain => "validate-main";
        public static string OnPullRequest => "on-pull-request";
        public static string FluxzyPublishNuget => "fluxzy-publish-nuget";
        public static string FluxzyPublishNugetPublicWithNote => "fluxzy-publish-nuget-public-with-note";
        public static string FluxzyCliFullPackage => "fluxzy-cli-full-package";
        public static string FluxzyCliPublish => "fluxzy-cli-publish";
        public static string FluxzyPublishNugetPublic => "fluxzy-publish-nuget-public";
        public static string FluxzyPublishNugetPublicPreRelease => "fluxzy-publish-nuget-public-pre-release";
        public static string FluxzyCliPublishWithNote => "fluxzy-cli-publish-with-note";
        public static string FluxzyCliPublishDocker => "fluxzy-cli-publish-docker";
    }
}
