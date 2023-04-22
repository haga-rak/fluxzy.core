namespace Fluxzy.Desktop.Services.Wizards
{
    public class CertificateWizardStatus
    {
        public CertificateWizardStatus(bool installed, bool userExplicitlyRefused, string certificateFriendlyName)
        {
            Installed = installed;
            UserExplicitlyRefused = userExplicitlyRefused;
            CertificateFriendlyName = certificateFriendlyName;
        }

        public bool Installed { get; } 
        public bool UserExplicitlyRefused { get;  }
        public string CertificateFriendlyName { get; }
        

        public bool IgnoreStep => Installed || UserExplicitlyRefused;
    }
}