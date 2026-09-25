# Imview

**Imlight Server Management Tool** — a desktop GUI for inspecting and editing game
content: quests, zones, drop tables, NPC inventories and locale data.

Imview reads the game client's WAD archives through the
[Imcodec](https://github.com/Jooty/Imcodec) submodule and reads/writes content to a
[RavenDB](https://ravendb.net/) document database.

Built with [Avalonia](https://avaloniaui.net/) 11 and ReactiveUI on .NET 9.

## Features

Available from the splash page:

| Tool | What it does |
| --- | --- |
| Quest Editor | Browse and edit quest templates |
| Get Quests From Packet Capture | Reconstruct quests from a captured packet session |
| Edit Zone | Edit zone data and zone transfers |
| Drop Table Editor | Create and edit drop tables |
| Analyze Object Property Blob | Decode a serialized ObjectProperty blob |
| Unpack KIWADs | Extract KIWAD archives |
| View WAD Files | Browse WAD archive contents |
| Download WAD Files | Fetch client files for a chosen revision |
| Configure Database & Client Files | Database connection and client-file settings |

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download) — projects target `net9.0`
- A reachable RavenDB instance, for the database-backed editors
- Git, for the submodules

On NixOS, `nix shell nixpkgs#dotnet-sdk_9` provides the SDK.

## Getting started

Clone with submodules — `Imview.Core` project-references several `Imcodec` projects and
`WizardTea`, so the build fails without them:

```bash
git clone --recurse-submodules git@github.com:Revive101/Imview.git
cd Imview
```

Already cloned without them:

```bash
git submodule update --init --recursive
```

Build and run:

```bash
dotnet build src/Imview.Core/Imview.Core.csproj
dotnet run --project src/Imview.Core
```

In VS Code, <kbd>F5</kbd> runs the **Launch Avalonia App** configuration, which builds
first and launches with the repository root as the working directory.

## Configuration

On first launch the app writes a default INI file and opens to the splash page:

| Platform | Path |
| --- | --- |
| Linux | `~/.config/Imview/config.ini` |
| Windows | `%APPDATA%\Imview\config.ini` |
| macOS | `~/.config/Imview/config.ini` |

```ini
[Database]
WorldDatabaseUrl =
WorldDatabaseName = WorldDB
WorldDatabaseCertificatePath =
DatabaseMaxNumberOfRequestsPerSession = 16
DatabaseRequestTimeoutInSeconds = 90
DatabaseWaitForNonStaleResultsTimeout = 5

[Application]
FirstRun = True

[ClientFiles]
RevisionsUrl = https://patcher.r10.one/revisions
SelectedRevision =
```

Both sections can be filled in from **Configure Database & Client Files** on the splash
page instead of editing the file by hand.

Two setup steps matter, in this order:

1. **Database.** Set `WorldDatabaseUrl` to your RavenDB instance. While it is empty the
   document store is never created and the database-backed editors do nothing.
   `WorldDatabaseCertificatePath` is resolved relative to the working directory.
2. **Client files.** Choose a revision, then **Download Root.wad Now**. The file is cached
   under `<config dir>/Imview/ClientFiles/<revision>/Root.wad` and loaded into memory
   along with the locale data on startup. Most content tools depend on it being loaded.

> [!WARNING]
> The RavenDB client is currently configured to accept *any* TLS certificate, including
> self-signed and invalid ones, to ease local development. Do not point a build with this
> behaviour at a production database over an untrusted network.

## Project layout

```
src/Imview.Core           Avalonia application — views, view models, services, database
src/Imview.PacketReader   Packet-capture parsing for the quest importer
submodule/Imcodec         Game file formats: WAD, ObjectProperty, BCD, message layer
submodule/WizardTea       Shared support library
```

`Imview.Core` is the only runnable project; `Imview.PacketReader` is a library it
consumes.

## License

BSD 3-Clause. See the license headers in the source files.
