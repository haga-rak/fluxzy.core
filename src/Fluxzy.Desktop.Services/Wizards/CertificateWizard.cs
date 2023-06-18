// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Certificates;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services.Wizards
{
    public class CertificateWizard
    {
        private readonly CertificateAuthorityManager _certificateAuthorityManager;
        private readonly GlobalUiSettingStorage _globalUiSettingStorage;
        private readonly IObservable<FluxzySettingsHolder> _settingHolder;

        public CertificateWizard(
            GlobalUiSettingStorage globalUiSettingStorage,
            CertificateAuthorityManager certificateAuthorityManager,
            IObservable<FluxzySettingsHolder> settingHolder)
        {
            _globalUiSettingStorage = globalUiSettingStorage;
            _certificateAuthorityManager = certificateAuthorityManager;
            _settingHolder = settingHolder;
        }

        /// <summary>
        ///     Check if the certificate wizard shall be prompt to the user
        /// </summary>
        /// <param name="setting"></param>
        public async Task<CertificateWizardStatus> ShouldAskCertificateWizard()
        {
            // Check for installed certificate 

            var setting = (await _settingHolder.FirstAsync()).StartupSetting;
            var certificate = setting.CaCertificate.GetX509Certificate();

            var certificateFriendlyName = certificate.Subject;

            var runningCertificateThumbPrint = certificate.Thumbprint;

            return new CertificateWizardStatus(
                _certificateAuthorityManager.IsCertificateInstalled(certificate),
                _globalUiSettingStorage.UiUserSetting.StartupWizardSettings.NoCertificateInstallExplicit,
                certificateFriendlyName);
        }

        public async Task<bool> InstallCertificate()
        {
            var setting = (await _settingHolder.FirstAsync()).StartupSetting;
            var certificate = setting.CaCertificate.GetX509Certificate();

            return await _certificateAuthorityManager.InstallCertificate(certificate);
        }

        public void RefuseCertificate()
        {
            _globalUiSettingStorage.UiUserSetting.StartupWizardSettings.NoCertificateInstallExplicit = true;
            _globalUiSettingStorage.UpdateUiSetting();
        }

        public void ReviveWizard()
        {
            _globalUiSettingStorage.UiUserSetting.StartupWizardSettings.NoCertificateInstallExplicit = false;
            _globalUiSettingStorage.UpdateUiSetting();
        }
    }
}
