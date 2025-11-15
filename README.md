# Application Health Check Windows Service

A Windows Service that monitors your applications' health status and sends email notifications when they go down.

## Features

- ✅ Monitors multiple applications simultaneously
- ✅ Configurable check intervals
- ✅ HTTP/HTTPS health checks with SSL validation
- ✅ Email notifications with detailed HTML reports
- ✅ Response time tracking
- ✅ Automatic logging to file system
- ✅ Runs as a Windows Service (automatic startup)

## Download as ZIP

Click the green "Code" button at the top of this page and select "Download ZIP" to download all files.

## Prerequisites

- .NET Framework 4.7.2 or higher
- Windows OS (Windows 10, Windows Server 2016+)
- Administrator privileges to install the service

## Quick Start

1. **Download** this repository as ZIP
2. **Extract** the files
3. **Open** in Visual Studio
4. **Configure** App.config with your settings
5. **Build** and install the service

## Configuration

### Email Setup (Gmail Example)

Update `App.config` with your Gmail credentials:

```xml
<add key="SmtpServer" value="smtp.gmail.com"/>
<add key="SmtpPort" value="587"/>
<add key="SmtpUsername" value="your-email@gmail.com"/>
<add key="SmtpPassword" value="your-app-password"/>
<add key="FromEmail" value="your-email@gmail.com"/>
<add key="ToEmail" value="ksubash00@gmail.com"/>
```

**Important:** Use an App Password, not your regular Gmail password!

### Configure Applications to Monitor

```xml
<add key="Applications" value="
    SubashKumar Website|https://subashkumar.com|200;
    My API|https://api.myapp.com/health|200;
    Dashboard|https://dashboard.myapp.com|200
"/>
```

## Installation

```powershell
# Build the project in Visual Studio
# Then open Command Prompt as Administrator:

cd C:\Path\To\Project\bin\Release
installutil HealthCheckService.exe
net start ApplicationHealthCheckService
```

## Features in Detail

- **Automated Monitoring**: Checks applications every 5 minutes (configurable)
- **Email Alerts**: Sends HTML email when applications are down
- **SSL Handling**: Properly handles SSL certificate errors
- **Detailed Logging**: Logs all operations to `C:\Logs\HealthCheckService\`
- **Response Tracking**: Monitors response times for performance insights

## Email Notification Preview

When an application is down, you receive:
- ⚠️ Alert header with timestamp
- Count of unhealthy applications  
- Detailed error messages and status codes
- Complete overview of all monitored applications
- Response times for healthy apps

## Troubleshooting

### Service won't start
- Check Event Viewer for errors
- Verify App.config is in same directory
- Ensure you have Administrator privileges

### Emails not sending
- Use Gmail App Password (not regular password)
- Check firewall settings for port 587
- Verify SMTP credentials in App.config

### SSL Certificate Errors
The service will detect and report SSL issues (like the subashkumar.com self-signed certificate) in the email alert.

## Managing the Service

```powershell
# Check status
sc query ApplicationHealthCheckService

# Stop service
net stop ApplicationHealthCheckService

# Restart service  
net stop ApplicationHealthCheckService && net start ApplicationHealthCheckService

# Uninstall
net stop ApplicationHealthCheckService
installutil /u HealthCheckService.exe
```

## Project Structure

```
HealthCheckService/
├── HealthCheckService.cs          # Main service logic
├── HealthCheckService.Designer.cs # Designer component
├── Program.cs                      # Entry point
├── ProjectInstaller.cs            # Service installer
├── App.config                      # Configuration
└── README.md                       # This file
```

## Security Best Practices

1. Never commit credentials to source control
2. Use App Passwords for email authentication
3. Run with least privilege account in production
4. Encrypt sensitive configuration data
5. Implement rate limiting for checks

## Support

For help with this project, check the repository issues or create a new one.

---

**Ready to use?** Download the ZIP, configure App.config, build, and install!