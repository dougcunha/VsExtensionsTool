#define MyAppVersion GetEnv('GITVERSION_FULLSEMVER')

[Setup]
AppName=VsExtensionsTool
AppPublisher=dougcunha
AppPublisherURL=https://github.com/dougcunha
AppSupportURL=https://github.com/dougcunha/VsExtensionsTool/issues
AppVersion={#MyAppVersion}
DefaultDirName={pf}\VsExtensionsTool
DefaultGroupName=VsExtensionsTool
OutputDir=.
OutputBaseFilename=VsExtensionsTool-{#MyAppVersion}.win-x64
Compression=lzma
SolidCompression=yes
LicenseFile=..\\LICENSE

[Dirs]
Name: "{commonappdata}\VsExtensionsTool"

[Files]
Source: "..\VsExtensionsTool\bin\Release\net9.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\VsExtensionsTool\bin\Release\net9.0\win-x64\publish\VsExtensionsTool.exe"; DestDir: "{commonappdata}\VsExtensionsTool"; Flags: ignoreversion

[Icons]
Name: "{group}\VsExtensionsTool"; Filename: "{app}\VsExtensionsTool.exe"
Name: "{group}\Uninstall VsExtensionsTool"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\VsExtensionsTool.exe"; Description: "Run VsExtensionsTool"; Flags: nowait postinstall skipifsilent
Filename: "cmd.exe"; Parameters: '/C setx /M PATH "%PATH%;{commonappdata}\\VsExtensionsTool"'; StatusMsg: "Adding VsExtensionsTool to system PATH..."; Flags: runhidden

[UninstallRun]
Filename: "cmd.exe"; Parameters: '/C for /f "delims=" %i in (''echo %PATH%'') do setx /M PATH "%i:{commonappdata}\\VsExtensionsTool;=%"'; StatusMsg: "Removing VsExtensionsTool from system PATH..."; Flags: runhidden