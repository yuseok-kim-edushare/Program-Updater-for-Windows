# Program Updater for Windows

A lightweight, open-source program updater for Windows applications built with .NET Framework 4.8. This updater provides a modern UI with progress tracking and supports automatic updates for multiple files with hash verification.

## Features

- Modern Windows Forms UI with progress tracking
- XML-based settings configuration
- JSON-based update configuration
- Secure file verification using SHA256 hashes
- Automatic backup of existing files
- Process management (stops running programs before update)
- Detailed logging with color-coded messages
- Cancellation support
- Error handling and rollback capabilities

## Requirements

- Windows 10 20H2 or Later (server 2022 or later) (for using release artifacts)
  - .NET Framework 4.8.1
- Not Ensured, but it should work with .NET 4.8 and windows 7 or later (also you can self build targe framework with net48)
  - MS introduce no comapatibility change from .NET 4.8 to .NET 4.8.1
    - then, it should work with .NET 4.8 and windows 7 or later
- (For Development) Visual Studio 2022+
  - or .NET 8+ SDK for using dotnet build command

## Building the Project

1. Clone the repository:
```bash
git clone https://github.com/YOUR_USERNAME/Program_updater_for_win.git
```

2. Open the solution in Visual Studio or your preferred IDE

3. Build the solution:
```bash
dotnet build --configuration Admin -f net481
dotnet build --configuration NonAdmin -f net481
```
- If you need to exe requires admin privileges, you need to build the project with the Admin configuration.
  - like update program in C:\Program Files\YourApp\
- If you need to exe does not require admin privileges, you need to build the project with the NonAdmin configuration.
  - like update program in C:\Users\YourName\AppData\Local\YourApp\

## Configuration

### Settings Configuration (settings.xml)

Create a `settings.xml` file in the same directory as the executable to customize the updater's behavior:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Settings>
  <UI>
    <WindowTitle>Program Updater</WindowTitle>
    <TitleText>Program Update in Progress</TitleText>
  </UI>
  <Configuration>
    <ConfigurationFilePath>https://raw.githubusercontent.com/your-username/your-repo/main/update_config.json</ConfigurationFilePath>
  </Configuration>
</Settings>
```

The settings.xml file allows you to:
- Customize the window title and update message
- Specify the update configuration file location (supports local files, HTTP, HTTPS, FTP, or FTPS)

For the ConfigurationFilePath, you can use any of these formats:
- Local file path: `C:\path\to\update_config.json`
- File protocol: `file:///C:/path/to/update_config.json`
- HTTP/HTTPS: `https://your-server.com/update_config.json`
- FTP/FTPS: `ftp://your-server.com/update_config.json`

### Update Configuration (JSON)

Create a configuration file (update_config.json) with your update details:
```json
{
    "files": [
        {
            "name": "MainApplication",
            "isExecutable": true,
            "currentPath": "C:\\Program Files\\YourApp\\app.exe",
            "newPath": "C:\\Program Files\\YourApp\\app_new.exe",
            "backupPath": "C:\\Program Files\\YourApp\\Backup\\app.exe",
            "downloadUrl": "http://your-server.com/updates/app.exe",
            "expectedHash": "SHA256_HASH_OF_NEW_FILE"
        }
    ]
}
```

The configuration file can be hosted:
- Locally on your machine
- On a web server via HTTP/HTTPS
- On an FTP server:
  ```
  ftp://your-server.com/path/to/update_config.json
  or
  ftp://username:password@your-server.com/updates/app.exe
  ```

To get file hash from Windows PowerShell:
```PowerShell
Get-FileHash -Path "C:\Program Files\YourApp\app.exe" -Algorithm SHA256
```

### Config Manager

The Config Manager is a Windows Forms application that allows you to manage the update configuration file.

Detailed information about the Config Manager is available in the [ConfigManager/README.md](ConfigManager/README.md) file.

## Usage

1. Run the updater:
```bash
updater.exe
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with .NET Framework 4.8.1
- Uses Newtonsoft.Json for JSON parsing
- Inspired by the need for a lightweight, open-source program updater

## Disclaimer

This software is provided "as is" without warranty of any kind. Use at your own risk. 
