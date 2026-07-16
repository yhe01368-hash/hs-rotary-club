# 豐原西南扶輪社社務行政系統 (.NET 8 重寫版)

取代舊版 `C:\Program Files (x86)\Project1\` 的 VB6 + Jet (`.mdb`) 系統。

| | 舊版 | 新版 |
|---|---|---|
| 架構 | VB6 + MS Access (Jet) + SetupFactory | .NET 8 + WPF + SQLite + EF Core 8 |
| DB | 29 個 `.mdb` 散落 | 1 個 `rotary.db` (SQLite) |
| 部署 | EXE 直裝 | WiX 3.x MSI (之後再說) |
| 大小 | 每版 6-7 MB × 11 版 | 單檔 < 30 MB |

## v0.1 — bootstrap scope (current)

3 模組:
- **M1 會員資料維護** (Member)
- **M2 會內收款管理** (ClubCollection + MonthlyReceivableSpec)
- **M3 友社捐款** (FriendlyClub + ClubDonation)

出席 / 扶輪支出 / 會計月報 / 信件 → v0.2 之後。

## Tech stack

- **Runtime:** .NET 8 (`net8.0-windows`)
- **UI:** WPF (XAML + MVVM via CommunityToolkit.Mvvm)
- **DB:** SQLite via EF Core 8 (`Microsoft.EntityFrameworkCore.Sqlite`)
- **DI:** Microsoft.Extensions.DependencyInjection
- **最終安裝檔打包:** **Inno Setup 6.x** → `installer/bin/HsRotaryClubSetup-v0.X.exe` (單檔 exe,非 MSI)

## Layout

```
hs-rotary-club/
├── README.md
├── LICENSE
├── .gitignore
├── src/
│   ├── HsRotaryClub.sln
│   ├── HsRotaryClub.Domain/         # POCO entities
│   ├── HsRotaryClub.Infrastructure/  # AppDbContext + EF
│   └── HsRotaryClub.App/             # WPF main app
└── tests/
    └── HsRotaryClub.SmokeTest/       # CRUD round-trip
```

## Build & Test

```powershell
dotnet build src/HsRotaryClub.sln -c Debug
dotnet test  tests/HsRotaryClub.SmokeTest/
```

## Status

- ✅ 2026-07-17 v0.1 bootstrap
- ⏳ smoke test pending
