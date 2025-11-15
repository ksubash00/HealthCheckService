using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace HealthCheckService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Service will run under the Local System account
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            // Service configuration
            serviceInstaller.ServiceName = "ApplicationHealthCheckService";
            serviceInstaller.DisplayName = "Application Health Check Service";
            serviceInstaller.Description = "Monitors application health and sends email notifications when applications are down.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Add installers to the collection
            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}