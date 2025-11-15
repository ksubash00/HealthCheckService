using System.ServiceProcess;

namespace HealthCheckService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new HealthCheckService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}