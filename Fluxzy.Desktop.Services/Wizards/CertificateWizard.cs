// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Certificates;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services.Wizards
{
    public class CertificateWizard
    {
        private readonly GlobalUiSettingStorage _globalUiSettingStorage;
        private readonly CertificateAuthorityManager _certificateAuthorityManager;
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
        /// Check if the certificate wizard shall be prompt to the user
        /// </summary>
        /// <param name="setting"></param>
        public async Task<CertificateWizardStatus> ShouldAskCertificateWizard()
        {
            // Check for installed certificate 

            var setting = (await _settingHolder.FirstAsync()).StartupSetting;
            var certificate = setting.CaCertificate.GetCertificate(); 
            
            var certificateFriendlyName = certificate.FriendlyName;
            
            var runningCertificateThumbPrint = certificate.Thumbprint;
            
            return new CertificateWizardStatus(_certificateAuthorityManager.IsCertificateInstalled(runningCertificateThumbPrint),
                _globalUiSettingStorage.UiUserSetting.StartupWizardSettings.NoCertificateInstallExplicit, certificateFriendlyName);
        }
        
        public async Task<bool> InstallCertificate()
        {
            var setting = (await _settingHolder.FirstAsync()).StartupSetting; 
            var certificate = setting.CaCertificate.GetCertificate();

            return await _certificateAuthorityManager.InstallCertificate(certificate);
        }
        
        public void RefuseCertificate()
        {
            _globalUiSettingStorage.UiUserSetting.StartupWizardSettings.NoCertificateInstallExplicit = true;
            _globalUiSettingStorage.UpdateUiSetting();
        }
    }
}
