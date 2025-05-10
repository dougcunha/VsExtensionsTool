# VsExtensionsTool

Command-line tool to list, filter, and remove Visual Studio extensions.

## Motivation

I created this tool because I often struggled to remove extensions from Visual Studio. Many times, after marking an extension for removal and restarting Visual Studio, the extension would remain installed. The only reliable way I found to remove extensions was by using the VSIXInstaller manually. This tool automates and simplifies that process.

I do not plan to add support for VSCode, as its extension management interface is already excellent and adding support would not provide significant value. This project is focused exclusively on Visual Studio.

## Features

- List all Visual Studio installations
- List installed extensions, with optional filtering by name or id
- Show installed version and latest version available on the Marketplace
- Remove extensions by id
- Beautiful and interactive console output powered by [Spectre.Console](https://spectreconsole.net/)

## Marketplace API Disclaimer

This tool uses a non-public, undocumented API from the Visual Studio Marketplace to retrieve the latest available version of extensions. The API endpoint and its contract are not officially supported by Microsoft and may change or stop working at any time, without notice. The initial implementation of VsExtensionsTool was based on API version `3.2-preview.1`.

If the Marketplace version check stops working, it is likely due to changes in this unofficial API.

## Installation

1. Clone the repository:

   ```sh
   git clone https://github.com/your-username/VsExtensionsTool.git
   cd VsExtensionsTool
   ```

2. Restore NuGet packages:

   ```sh
   dotnet restore
   ```

3. Build the project:

   ```sh
   dotnet build -c Release
   ```

## Usage

```sh
VsExtensionsTool.exe /list_vs
VsExtensionsTool.exe /list [filter] [/version]
VsExtensionsTool.exe /remove <id>
```

- `/list_vs`: Lists all Visual Studio installations
- `/list`: Lists extensions for the selected instance (optionally filter by name/id, `/version` shows Marketplace version)
- `/remove <id>`: Removes an extension by id

## Dependencies

This project uses [Spectre.Console](https://spectreconsole.net/) to provide rich and interactive console output. Many thanks to the Spectre.Console team and contributors for their amazing work!

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

MIT. See the [LICENSE](LICENSE) file.
