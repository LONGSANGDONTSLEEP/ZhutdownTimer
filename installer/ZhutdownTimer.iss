#define MyAppName "Zhutdown Timer"
#ifndef MyAppVersion
  #define MyAppVersion "2.0.0"
#endif
#define MyAppPublisher "LONGSANGDONTSLEEP"
#define MyAppURL "https://github.com/LONGSANGDONTSLEEP/ZhutdownTimer"
#define MyAppExeName "ZhutdownTimer.exe"

[Setup]
AppId={{A52879F0-6D2F-4FC1-B4B2-21EA1B4D7D97}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\Zhutdown Timer
DefaultGroupName=Zhutdown Timer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=ZhutdownTimer-Setup-v{#MyAppVersion}
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Zhutdown Timer Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\dist\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Zhutdown Timer"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall Zhutdown Timer"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Zhutdown Timer"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "ZhutdownTimer"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Zhutdown Timer}"; Flags: nowait postinstall skipifsilent
