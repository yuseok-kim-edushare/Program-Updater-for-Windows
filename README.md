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

- Windows 7 or later
- .NET Framework 4.8 or later
- Visual Studio 2019/2022 Community Edition or any other compatible IDE

## Building the Project

1. Clone the repository:
```bash
git clone https://github.com/YOUR_USERNAME/Program_updater_for_win.git
```

2. Open the solution in Visual Studio or your preferred IDE

3. Build the solution:
```bash
dotnet build --configuration Release
```

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
- Specify the update configuration file location (HTTP, HTTPS, FTP, or FTPS)

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

The configuration file can be hosted on HTTP, HTTPS, or FTP:
```
ftp://your-server.com/path/to/update_config.json
or
ftp://username:password@your-server.com/updates/app.exe
```

To get file hash from Windows PowerShell:
```PowerShell
Get-FileHash -Path "C:\Program Files\YourApp\app.exe" -Algorithm SHA256
```

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

- Built with .NET Framework 4.8
- Uses Newtonsoft.Json for JSON parsing
- Inspired by the need for a lightweight, open-source program updater

## Disclaimer

This software is provided "as is" without warranty of any kind. Use at your own risk. 