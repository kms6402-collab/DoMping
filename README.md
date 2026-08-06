# DoMping

A multi-host ping/network monitor for Windows, rebranded and extended from **[vmPing](https://github.com/R-Smith/vmPing)** by Ryan Smith. This repository is a fork: the original ICMP ping engine, favorites/aliases system, and options infrastructure come from vmPing; DoMping adds a dark "terminal grid" UI, service port checks, a latency graph, and a few other features described below.

## Features

- Ping many hosts in parallel, each shown as a color-coded status tile (`UP` / `TIMEOUT` / `SLOW` / `SCANNING...` / `DNS ERROR` / `IDLE`)
- Per-host latency history graph (`PingSparkline`) alongside the classic scrolling reply log — choose **log only**, **graph only**, or **both** in Options → Display
- **Service port checks**: prefix a hostname with `S/` (e.g. `S/10.0.1.10`) to test a configurable set of TCP and UDP ports in one probe (defaults: TCP 22, 23, 80, 56; UDP 6), each reported individually as OPEN/CLOSED
- Single TCP port monitor (`host:port`), DNS lookup (`D/host`), and traceroute (`T/host`)
- Sent / received / lost counters per host
- Status filter in the toolbar to show only hosts matching a given state (Up, Down, Slow, Scanning, Error, Idle)
- Favorites, aliases, multi-host input, flood ping, popup/email/audio alerts, per-host or global logging
- System tray integration, always-on-top, minimize/exit to tray

## Requirements

DoMping is a framework-dependent build — it needs the **[.NET 10 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0)** installed on the machine that runs it. Windows 10/11, x64.

## Building

```
dotnet build -c Release
```

## Publishing a single-file build

```
dotnet publish -c Release -r win-x64 -p:SelfContained=false
```

This produces a single `.exe` under `bin/Release/net10.0-windows/win-x64/publish/` — it still requires the .NET Desktop Runtime described above (`SelfContained=false`), it just merges DoMping's own DLL into the executable so there's only one file to hand out.

## Command line

```
DoMping [-i interval] [-w timeout] [<target_host>...]
```

Hosts can also be prefixed to change probe type: `D/host` (DNS), `T/host` (traceroute), `S/host` (service ports).

## License / attribution

This project is a derivative of vmPing (https://github.com/R-Smith/vmPing) by Ryan Smith. See the Help window in the app for the upstream project link.
