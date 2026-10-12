; Inno Setup script. Driven by build-windows-installer.ps1, which passes every /D define below.
#ifndef AppName
  #error AppName must be defined
#endif

[Setup]
AppId={{{#AppGuid}}
AppName={#AppTitle}
AppVersion={#AppVersion}
AppPublisher=Majorsilence
AppPublisherURL=https://github.com/majorsilence/Reporting
DefaultDirName={autopf}\Majorsilence Reporting\{#AppName}
DefaultGroupName=Majorsilence Reporting
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppName}.exe
OutputDir={#OutDir}
OutputBaseFilename={#AppName}-{#AppVersion}-{#Rid}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
#if Rid == "win-arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppTitle}"; Filename: "{app}\{#AppName}.exe"
Name: "{autodesktop}\{#AppTitle}"; Filename: "{app}\{#AppName}.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\{#AppName}.exe"; Description: "Launch {#AppTitle}"; Flags: nowait postinstall skipifsilent
