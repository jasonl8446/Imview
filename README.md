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
- Your own copy of the game client, for the two source-generator inputs described below —
  Imcodec ships no game data and will not build without them

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

### Generator inputs (bring your own data)

Imcodec follows a BYOD (bring-your-own-data) philosophy: it ships no copyrighted game
files. Two source generators need input that you must supply from your own game client, or
the build fails:

| Directory | Input | Source |
| --- | --- | --- |
| `submodule/Imcodec/src/Imcodec.ObjectProperty/GeneratorInput/` | a type dump `*.json` | generate with [wiztype](https://github.com/wizspoil/wiztype) |
| `submodule/Imcodec/src/Imcodec.MessageLayer/GeneratorInput/` | `*Messages.xml` | unpack `Root.wad` with the Imcodec CLI |

Both directories start out holding only an empty `!base` placeholder. Without the type dump
the `Imcodec.ObjectProperty` build fails with ~165 `CS0246`/`CS0234` errors about missing
base types (`Result`, `CoreObjectInfo`, `BehaviorTemplate`, ...). Without the message XML,
`Imcodec.MessageLayer` fails with `CS0234` on the `Imcodec.MessageLayer.Generated`
namespace, preceded by warning `IMC001: No XML files found`.

The message XML lives inside the client's `Root.wad`, so bootstrap in this order:

```bash
# 1. Drop your wiztype dump into the ObjectProperty generator input
cp /path/to/dump.json submodule/Imcodec/src/Imcodec.ObjectProperty/GeneratorInput/

# 2. Build the Imcodec CLI — it does not depend on the message layer, so it builds now
dotnet build submodule/Imcodec/src/Imcodec.Cli/Imcodec.Cli.csproj

# 3. Unpack Root.wad and copy the message definitions out of it
# (adjust the runtime-identifier folder for your platform)
./submodule/Imcodec/src/Imcodec.Cli/bin/Debug/net9.0/linux-x64/imcodec \
    wad unpack /path/to/Root.wad /tmp/root
find /tmp/root -name '*Messages.xml' \
    -exec cp {} submodule/Imcodec/src/Imcodec.MessageLayer/GeneratorInput/ \;
```

A `Root.wad` for a chosen revision can be downloaded from within the app itself once it
runs, but for this first build you need one obtained independently.

### Build and run

```bash
dotnet build src/Imview.Core/Imview.Core.csproj
dotnet run --project src/Imview.Core
```

In VS Code, <kbd>F5</kbd> runs the **Launch Avalonia App** configuration, which builds
first and launches with the repository root as the working directory.

If an analyzer complains that source-generated classes are missing, run `dotnet build`
once to make the generators produce them.

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
