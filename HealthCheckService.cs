using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace HealthCheckService
{
    public partial class HealthCheckService : ServiceBase
    {
        private Timer _timer;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly object _lockObject = new object();

        public HealthCheckService()
        {
            InitializeComponent();
            ServiceName = "ApplicationHealthCheckService";
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("Service starting...");
            
            // Check interval in milliseconds (default: 5 minutes)
            int interval = int.Parse(ConfigurationManager.AppSettings["CheckIntervalMinutes"] ?? "5") * 60000;
            
            _timer = new Timer(interval);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            // Run initial check
            Task.Run(() => PerformHealthCheck());
            
            WriteLog("Service started successfully.");
        }

        protected override void OnStop()
        {
            WriteLog("Service stopping...");
            _timer?.Stop();
            _timer?.Dispose();
            WriteLog("Service stopped.");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                Task.Run(() => PerformHealthCheck());
            }
        }

        private async Task PerformHealthCheck()
        {
            try
            {
                WriteLog("Starting health check...");
                
                var applications = GetApplicationsToMonitor();
                var results = new List<HealthCheckResult>();

                foreach (var app in applications)
                {
                    var result = await CheckApplicationHealth(app);
                    results.Add(result);
                    WriteLog($"Checked {app.Name}: {(result.IsHealthy ? "Healthy" : "Unhealthy")}");
                }

                // Send email if any application is unhealthy
                var unhealthyApps = results.FindAll(r => !r.IsHealthy);
                if (unhealthyApps.Count > 0)
                {
                    await SendHealthStatusEmail(results, unhealthyApps);
                }

                WriteLog("Health check completed.");
            }
            catch (Exception ex)
            {
                WriteLog($"Error during health check: {ex.Message}");
            }
        }

        private List<ApplicationInfo> GetApplicationsToMonitor()
        {
            var applications = new List<ApplicationInfo>();
            
            // Read from app.config
            string appsConfig = ConfigurationManager.AppSettings["Applications"];
            if (!string.IsNullOrEmpty(appsConfig))
            {
                var apps = appsConfig.Split(';');
                foreach (var app in apps)
                {
                    var parts = app.Split('|');
                    if (parts.Length >= 2)
                    {
                        applications.Add(new ApplicationInfo
                        {
                            Name = parts[0].Trim(),
                            Url = parts[1].Trim(),
                            ExpectedStatusCode = parts.Length > 2 ? int.Parse(parts[2]) : 200
                        });
                    }
                }
            }

            return applications;
        }

        private async Task<HealthCheckResult> CheckApplicationHealth(ApplicationInfo app)
        {
            var result = new HealthCheckResult
            {
                ApplicationName = app.Name,
                Url = app.Url,
                CheckTime = DateTime.Now
            };

            try
            {
                var response = await _httpClient.GetAsync(app.Url);
                result.StatusCode = (int)response.StatusCode;
                result.ResponseTime = response.Headers.Date.HasValue 
                    ? DateTime.Now - response.Headers.Date.Value.DateTime 
                    : TimeSpan.Zero;
                result.IsHealthy = response.StatusCode == (HttpStatusCode)app.ExpectedStatusCode;
                result.Message = response.IsSuccessStatusCode ? "OK" : $"Status code: {response.StatusCode}";
            }
            catch (HttpRequestException ex)
            {
                result.IsHealthy = false;
                result.Message = $"Connection failed: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                result.IsHealthy = false;
                result.Message = "Request timeout";
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Message = $"Error: {ex.Message}";
            }

            return result;
        }

        private async Task SendHealthStatusEmail(List<HealthCheckResult> allResults, List<HealthCheckResult> unhealthyApps)
        {
            try
            {
                string smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
                int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
                string fromEmail = ConfigurationManager.AppSettings["FromEmail"];
                string toEmail = ConfigurationManager.AppSettings["ToEmail"];
                bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"] ?? "true");

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail);
                    message.To.Add(toEmail);
                    message.Subject = $"⚠️ Application Health Alert - {unhealthyApps.Count} Application(s) Down";
                    message.IsBodyHtml = true;
                    message.Body = GenerateEmailBody(allResults, unhealthyApps);

                    using (var smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                        smtp.EnableSsl = enableSsl;
                        await smtp.SendMailAsync(message);
                    }
                }

                WriteLog($"Email notification sent to {toEmail}");
            }
            catch (Exception ex)
            {
                WriteLog($"Failed to send email: {ex.Message}");
            }
        }

        private string GenerateEmailBody(List<HealthCheckResult> allResults, List<HealthCheckResult> unhealthyApps)
        {
            var body = @"
                <html>
                <head>
                    <style>
                        body { font-family: Arial, sans-serif; }
                        .header { background-color: #d9534f; color: white; padding: 10px; }
                        .healthy { background-color: #d4edda; }
                        .unhealthy { background-color: #f8d7da; }
                        table { border-collapse: collapse; width: 100%; margin-top: 20px; }
                        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
                        th { background-color: #4CAF50; color: white; }
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>Application Health Check Alert</h2>
                    </div>
                    <p><strong>Alert Time:</strong> " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</p>
                    <p><strong>Unhealthy Applications:</strong> " + unhealthyApps.Count + @"</p>
                    
                    <h3>❌ Unhealthy Applications</h3>
                    <table>
                        <tr>
                            <th>Application</th>
                            <th>URL</th>
                            <th>Status</th>
                            <th>Message</th>
                            <th>Check Time</th>
                        </tr>";

            foreach (var app in unhealthyApps)
            {
                body += $@"
                        <tr class='unhealthy'>
                            <td>{app.ApplicationName}</td>
                            <td>{app.Url}</td>
                            <td>{app.StatusCode}</td>
                            <td>{app.Message}</td>
                            <td>{app.CheckTime:HH:mm:ss}</td>
                        </tr>";
            }

            body += @"
                    </table>
                    
                    <h3>✅ All Applications Status</h3>
                    <table>
                        <tr>
                            <th>Application</th>
                            <th>URL</th>
                            <th>Status</th>
                            <th>Response Time</th>
                            <th>Message</th>
                        </tr>";

            foreach (var app in allResults)
            {
                string rowClass = app.IsHealthy ? "healthy" : "unhealthy";
                string status = app.IsHealthy ? "✅ Healthy" : "❌ Unhealthy";
                body += $@"
                        <tr class='{rowClass}'>
                            <td>{app.ApplicationName}</td>
                            <td>{app.Url}</td>
                            <td>{status}</td>
                            <td>{app.ResponseTime.TotalMilliseconds:F0}ms</td>
                            <td>{app.Message}</td>
                        </tr>";
            }

            body += @"
                    </table>
                    <br/>
                    <p style='color: #666; font-size: 12px;'>This is an automated message from the Application Health Check Service.</p>
                </body>
                </html>";

            return body;
        }

        private void WriteLog(string message)
        {
            try
            {
                string logPath = ConfigurationManager.AppSettings["LogPath"] ?? @"C:\Logs\HealthCheckService\";
                System.IO.Directory.CreateDirectory(logPath);
                
                string logFile = System.IO.Path.Combine(logPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                
                System.IO.File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }

    public class ApplicationInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public int ExpectedStatusCode { get; set; } = 200;
    }

    public class HealthCheckResult
    {
        public string ApplicationName { get; set; }
        public string Url { get; set; }
        public bool IsHealthy { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime CheckTime { get; set; }
    }
}