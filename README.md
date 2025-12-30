<div id="top"></div>
<!--
*** Genie Client is a Community focused development of Conny's Open Source Version of GenieClient.
*** we want to take a moment and thank Conny for his hard work on GenieClient over the years and 
*** for allowing the community to take a part in the future development of the Client.
*** 
*** Thanks again! Now team go create something AMAZING! :D
-->



<!-- PROJECT SHIELDS -->
<!--
*** This Readme is using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links
-->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![GPU License][license-shield]][license-url]




<!-- PROJECT LOGO -->
<br />
<div align="center">


<h1 align="center">Genie 5</h1>

  <p align="center">
    Genie is an alternative front-end for use with the Simutronics Corporation's game DragonRealms.
    <br />
    <strong>Now available in two editions:</strong>
    <br />
    🖥️ <strong>Classic</strong> (Windows Forms) - Full-featured, Windows-only
    <br />
    🍎🐧 <strong>Cross-Platform</strong> (Avalonia UI) - <strong>Now runs on macOS and Linux!</strong>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#building-from-source">Building from Source</a></li>
    <li><a href="#running-tests">Running Tests</a></li>
    <li><a href="#project-architecture">Project Architecture</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>


<!-- ROADMAP -->
## Roadmap

### Completed ✅
- [x] .NET 6 Upgrade
- [x] .NET 10 Upgrade  
- [x] Refactor Core Logic away from GUI
- [x] Cross-Platform UI Foundation (Avalonia)
  - [x] Basic window with game text output
  - [x] Vitals display (Health, Mana, Conc, Stamina, Spirit)
  - [x] Compass and status effects  
  - [x] Connection dialog with saved profiles
  - [x] Game server connection
  - [x] Highlights system
  - [x] Multiple windows (Room, Inventory, Thoughts)
  - [x] Script Explorer (run, stop, view scripts)
  - [x] Aliases configuration dialog
  - [x] Preferences dialog

### In Progress 🚧
- [ ] Cross-Platform UI - Advanced Features
  - [x] Script management ✅
  - [x] Aliases configuration ✅
  - [ ] Auto-mapper (UI started, integration pending)
  - [ ] Command history
  - [ ] Macros configuration
  - [ ] Triggers configuration
  - [ ] Highlights configuration
- [ ] Upgrade Plugin Interface

### Future 📋
- [ ] Get Latest Version (OneButton) <AInstallLogo>
    <img src="https://cdn.advancedinstaller.com/svg/pressinfo/AiLogoColor.svg" width="70" height="40"></AInstallLogo>
- [ ] Native installers (.dmg for macOS, .deb/.rpm for Linux)


See the [open issues](https://github.com/GenieClient/Genie4/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the GPL 3.0 License. See `LICENSE` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [https://github.com/walcon] Conny - Origional Developer
* [https://github.com/mj-colonel-panic] Mark Cherry (Djordje aka Colonel Panic) Genie 4
* All others who contributed to the Genie open-source effort.


<p align="right">(<a href="#top">back to top</a>)</p>



## Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

### Which Edition Should I Use?

| Feature | Classic (Windows Forms) | Cross-Platform (Avalonia) |
|---------|------------------------|---------------------------|
| **Platforms** | Windows only | ✅ Windows, ✅ macOS, ✅ Linux |
| **Maturity** | Full-featured, stable | Actively developed |
| **Game Connection** | ✅ Yes | ✅ Yes |
| **Highlights** | ✅ Yes | ✅ Yes |
| **Multiple Windows** | ✅ Yes | ✅ Yes |
| **Script Explorer** | ✅ Yes | ✅ Yes |
| **Aliases Config** | ✅ Yes | ✅ Yes |
| **Preferences** | ✅ Yes | ✅ Yes |
| **Auto-Mapper** | ✅ Yes | 🚧 In progress |
| **Macros Config** | ✅ Yes | ⏳ Coming soon |
| **Triggers Config** | ✅ Yes | ⏳ Coming soon |
| **Plugins** | ✅ Yes | ⏳ Coming soon |

**Recommendation:** 
- Use **Classic** if you're on Windows and need all features (auto-mapper, plugins, macros)
- Use **Cross-Platform** if you're on **macOS or Linux** — it works great! Also available on Windows if you prefer the modern UI

---

### Build Scripts

We provide convenient build scripts for both editions:

| Script | Platform | Builds | Description |
|--------|----------|--------|-------------|
| `build.ps1` | Windows | Classic (WinForms) | Quick build, copies to `bin\Run` |
| `build-and-run.ps1` | Windows | Classic (WinForms) | Build and optionally launch |
| `build-avalonia.ps1` | Windows | Cross-Platform (Avalonia) | Quick build, copies to `bin\Avalonia` |
| `build-crossplatform.ps1` | Windows | Cross-Platform (Avalonia) | Build/publish for any platform |
| `build-crossplatform.sh` | macOS/Linux | Cross-Platform (Avalonia) | Build/publish for any platform |

---

### Cross-Platform UI (Avalonia) 🌐

**✅ Works on Windows, macOS, and Linux!** Self-contained builds available for all platforms.

#### Quick Build for Windows (Recommended for Development)

```powershell
# Build and copy to bin\Avalonia
.\build-avalonia.ps1

# Build and run immediately
.\build-avalonia.ps1 -Run
```

This is the simplest option for day-to-day Windows development. It copies output to `bin\Avalonia`, so you can keep Genie running while rebuilding.

#### Cross-Platform Publishing

For creating self-contained executables for distribution:

**Windows (PowerShell):**
```powershell
# Simple build for current platform
.\build-crossplatform.ps1

# Create self-contained executable
.\build-crossplatform.ps1 -Publish

# Build for a specific platform
.\build-crossplatform.ps1 -Runtime osx-arm64 -Publish   # macOS Apple Silicon
.\build-crossplatform.ps1 -Runtime linux-x64 -Publish   # Linux 64-bit
.\build-crossplatform.ps1 -Runtime win-x64 -Publish     # Windows 64-bit
```

**macOS/Linux (Bash):**
```bash
# Make executable (first time only)
chmod +x build-crossplatform.sh

# Simple build for current platform
./build-crossplatform.sh

# Create self-contained executable
./build-crossplatform.sh --publish

# Build for a specific platform
./build-crossplatform.sh --runtime osx-arm64 --publish  # macOS Apple Silicon
./build-crossplatform.sh --runtime linux-x64 --publish  # Linux 64-bit
```

**Supported Runtime Identifiers (RIDs):**
| Runtime | Platform |
|---------|----------|
| `win-x64` | Windows 64-bit |
| `win-arm64` | Windows ARM64 |
| `osx-x64` | macOS Intel |
| `osx-arm64` | macOS Apple Silicon |
| `linux-x64` | Linux 64-bit |
| `linux-arm64` | Linux ARM64 |

**Output Locations:**
- Build: `src/Genie.UI/bin/Release/net10.0/`
- Publish: `bin/CrossPlatform/<runtime>/`

#### Direct Commands (Alternative)

Dev Mode, Windows:
```powershell
pushd src\Genie.UI; try { dotnet run } finally { popd }
```

Dev Mode, macOS/Linux:
```bash
( cd src/Genie.UI && dotnet run )
```

Build Release version and run, Windows:
```powershell
dotnet build Genie5.sln --configuration Release; if ($LASTEXITCODE -eq 0) { dotnet run --project src/Genie.UI/Genie.UI.csproj }
```

Build Release version and run, macOS/Linux:
```bash
dotnet build Genie5.sln --configuration Release && dotnet run --project src/Genie.UI/Genie.UI.csproj
```

---

### Classic Windows UI (Windows Forms) 🖥️

Full-featured Windows-only edition.

#### Using Build Scripts (Recommended)

**Quick build:**
```powershell
.\build.ps1
```
This builds and copies output to `bin\Run`, allowing you to keep Genie running while rebuilding.

**Build and run interactively:**
```powershell
.\build-and-run.ps1
```
This builds, copies to `bin\Run`, and prompts to launch Genie.

**Build and run immediately:**
```powershell
.\build.ps1; if ($LASTEXITCODE -eq 0) { .\bin\Run\Genie.exe }
```

#### Direct Build (Alternative)

```powershell
dotnet build Genie5.sln --configuration Release; if ($LASTEXITCODE -eq 0) { .\src\Genie.Windows\bin\Release\net10.0-windows\Genie.exe }
```

> **Note:** If Genie is running, direct builds will fail because files are locked. Use the build scripts instead, which output to a separate `bin\Run` folder.

## Running Tests

The project uses xUnit for testing. Tests are located in the `src/Genie.UI.Tests` directory.

### Run All Tests

Windows (PowerShell):
```powershell
dotnet test Genie5.sln
```

macOS/Linux:
```bash
dotnet test Genie5.sln
```

### Run Tests for a Specific Project

```powershell
dotnet test src/Genie.UI.Tests/Genie.UI.Tests.csproj
```

### Run Tests with Detailed Output

```powershell
dotnet test Genie5.sln --verbosity detailed
```

### Run Tests with Code Coverage

```powershell
dotnet test Genie5.sln --collect:"XPlat Code Coverage"
```

Coverage reports will be generated in the `TestResults` folder of each test project.

## Project Architecture

The codebase uses a shared core library with two UI options:

```
Genie5.sln                   # Main solution
├── src/
│   ├── Genie.Core/          # Shared core logic (multi-targeted: net10.0 + net10.0-windows)
│   │   └── Genie.Core.csproj
│   ├── Genie.UI/            # Cross-platform Avalonia UI 🌐
│   │   ├── Views/           # AXAML windows and dialogs
│   │   ├── Services/        # Game manager, highlight processor
│   │   └── Genie.UI.csproj
│   ├── Genie.UI.Tests/      # Unit tests (xUnit)
│   │   └── Genie.UI.Tests.csproj
│   └── Genie.Windows/       # Classic Windows Forms UI 🖥️
│       ├── Services/        # Windows-specific service implementations
│       └── Genie.Windows.csproj
└── Plugin/
    └── Plugins.vbproj       # Plugin interfaces (VB.NET)
```

### Genie.Core (Shared)
Platform-independent business logic used by both UIs:
- Connection handling and game communication
- Script engine (Genie scripts, JavaScript, Lua)
- Configuration management
- Highlights, triggers, and macros
- Service interfaces for platform-specific features

### Genie.UI (Cross-Platform Edition) 🌐
The new cross-platform UI built with Avalonia:
- **Platforms:** ✅ Windows, ✅ macOS, ✅ Linux (all with working builds!)
- Modern dark theme interface
- Game connection and text output
- Vitals display, compass, status effects
- Highlights and multiple windows (Room, Inventory, Thoughts, etc.)
- Script Explorer (run, stop, view scripts)
- Aliases configuration dialog
- Preferences dialog
- *In active development - auto-mapper and more features coming!*

### Genie.Windows (Classic Edition) 🖥️
The full-featured Windows-only GUI:
- **Platforms:** Windows only
- Complete Windows Forms UI
- Auto-mapper with visual display
- Full script manager
- All configuration dialogs
- Plugin support
- *Mature and stable - use this for all features*
