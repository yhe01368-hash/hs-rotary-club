; ============================================================
; 豐原西南扶輪社社務行政系統 — Inno Setup installer script
; 編譯產出:HsRotaryClubSetup-v0.6.exe (單檔 self-extracting)
; 工具:ISCC.exe (C:\Program Files (x86)\Inno Setup 6\)
; 用 .NET 8 的 single-file, framework-dependent 模式發佈
; ============================================================

#define MyAppName "豐原西南扶輪社社務行政系統"
#define MyAppShortName "HsRotaryClub"
#define MyAppVersion "0.6.0"
#define MyAppPublisher "Chia Chang"
#define MyAppUrl "https://github.com/yhe01368-hash/hs-rotary-club"
#define MyAppExeName "HsRotaryClub.App.exe"

#define SolutionDir "..\src"
#define PublishDir "..\src\HsRotaryClub.App\bin\Release\net8.0-windows\win-x64\publish"
#define OutputBaseDir "bin"

[Setup]
; AppId 升版必須改,降版必須保留前一版碼 (避免同時裝兩個版本)
AppId={{B6F3E9A4-1C2D-4F88-9A11-7E2C9B5A0606}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}
AppUpdatesURL={#MyAppUrl}

; 安裝路徑:ProgramFiles\HsRotaryClub
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; 輸出檔
OutputDir={#OutputBaseDir}
OutputBaseFilename=HsRotaryClubSetup-v{#MyAppVersion}

; 壓縮跟圖示
Compression=lzma2/ultra
SolidCompression=yes
SetupIconFile=

; 需要 admin (WMI machine-binding 之後 v0.7+ 才嚴格要)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; v0.6 還沒 license.dat 機制, 之後改
Uninstallable=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
; Inno 6.x 沒有內建 Chinese (Traditional).isl — fallback Default + [Messages] 自訂中文

[Messages]
; 自訂中文安裝字串
BeveledLabel=豐原西南扶輪社
SetupWindowTitle=安裝 豐原西南扶輪社社務行政系統
WelcomeLabel2=本安裝程式會安裝 [name/ver] 到您的電腦。%n%nRotary Club 社務行政系統 v0.6%nn請按「下一步」繼續。

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 主要 exe (從 publish/ 拿)
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; 附隨 dll (Native + WPF 自帶 .NET 8 runtime)
Source: "{#PublishDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\*.json"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 無 sqlite seed db — 啟動時自動 EnsureCreated + SeedData.SeedIfEmpty

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; 勾選才執行
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 卸載清掉 user data (旋轉自己的 rotate 目錄才有效, %AppData% 留著被 rename)
Type: filesandordirs; Name: "{userappdata}\HsRotaryClub"
