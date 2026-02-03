; Inno Setup script for CoachPad WPF
; Build the app and set MyAppExePath to the compiled exe path

#define MyAppName "CoachPad"
#define MyAppPublisher "CoachPad"
#define MyAppVersion "1.0.0"
#define MyAppExePath "C:\\Path\\To\\CoachPadWpf.exe"

[Setup]
AppId={{C4D2C2D4-7F59-4D6D-A0CF-4C7D2DE3D9D6}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=CoachPadSetup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
; Set this to your compiled exe and supporting files
Source: "{#MyAppExePath}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\CoachPad"; Filename: "{app}\CoachPadWpf.exe"
Name: "{commondesktop}\CoachPad"; Filename: "{app}\CoachPadWpf.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; Flags: unchecked

[Run]
Filename: "{app}\CoachPadWpf.exe"; Description: "Launch CoachPad"; Flags: nowait postinstall

[Code]
var
	HostPage: TInputQueryWizardPage;

function DefaultSettingsJson(const HostValue: string): string;
begin
	Result := '{' + #13#10 +
						'  "Host": "' + HostValue + '",' + #13#10 +
						'  "IsSystemAudioEnabled": true,' + #13#10 +
						'  "IsMicEnabled": true,' + #13#10 +
						'  "HasAudioConsent": false,' + #13#10 +
						'  "HasCameraConsent": false,' + #13#10 +
						'  "StartWithWindows": false,' + #13#10 +
						'  "FollowActiveWindow": true' + #13#10 +
						'}';
end;

procedure InitializeWizard;
begin
	HostPage := CreateInputQueryPage(wpWelcome,
		'Backend host',
		'Enter the backend host and port',
		'Example: 127.0.0.1:8000');
	HostPage.Add('Host', False);
	HostPage.Values[0] := '127.0.0.1:8000';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
	SettingsDir: string;
	SettingsPath: string;
	HostValue: string;
begin
	if CurStep = ssPostInstall then
	begin
		SettingsDir := ExpandConstant('{localappdata}\CoachPadWpf');
		SettingsPath := SettingsDir + '\settings.json';
		ForceDirectories(SettingsDir);
		HostValue := HostPage.Values[0];
		if HostValue = '' then
			HostValue := '127.0.0.1:8000';
		SaveStringToFile(SettingsPath, DefaultSettingsJson(HostValue), False);
	end;
end;
