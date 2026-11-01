# ZenStates-Core-AOT

[中文](README.zh-CN.md)

> A **Native AOT-compatible** fork of [`irusanov/ZenStates-Core`](https://github.com/irusanov/ZenStates-Core).

> [!IMPORTANT]
> This AOT fork runs WMI-backed features (e.g. `AOD.GetWmiFunctions`) only when elevated (Run as administrator).

## Fork & AOT Changes

This repository is a maintained fork of the original ZenStates-Core by
[irusanov](https://github.com/irusanov). Its sole purpose is to remove every obstacle that
prevented the library from being consumed by a **Native AOT** (.NET ILC) application.

Key changes relative to upstream:

- **Removed the `System.Management` (WMI) dependency.** The original code relied on WMI for SMU
  function discovery and DRAM timing probing. `System.Management` is built on late-bound COM
  interop that Native AOT (ILC) cannot compile — publishing the library produced
  `IL2104` / `IL3053` warnings, and every WMI call crashed at runtime. The dependency has been
  **fully replaced** with the AOT-safe [`WmiLight`](https://www.nuget.org/packages/WmiLight)
  NuGet package.
- **Dropped the `net20` target framework.** Only modern .NET targets remain
  (`netstandard2.1`, `net8.0-windows`), because WmiLight requires at least `netstandard2.0`.
- **Fixed Native AOT trimming warnings.** Several `IL2091` / `IL2026` / `IL3000` warnings that
  broke AOT trimming were eliminated by replacing ineffective `#pragma warning disable`
  suppressions — which the trim analyzer ignores — with real AOT annotations
  (`DynamicallyAccessedMembers`, `UnconditionalSuppressMessage`).

The public API surface (`Cpu`, `Aod`, `WMI`, etc.) keeps the same member names, parameter names,
and return semantics. The only underlying change is that the WMI object types exposed by `WMI`
(`ManagementScope` / `ManagementObject` / `ManagementBaseObject`) are now thin AOT-safe wrappers
(`WmiScope` / `WmiInstance`). The `WMI.RunCommand` method was removed because WmiLight exposes no
`byte[]` parameter setter for it, and it had no callers in this assembly.

## CPU Support

The following AMD Zen-based codenames are supported:

* SummitRidge
* Whitehaven
* Naples
* RavenRidge
* PinnacleRidge
* Colfax
* Picasso
* FireFlight
* Matisse
* CastlePeak
* Rome
* Dali
* Renoir
* VanGogh
* Vermeer
* Chagall
* Milan
* Cezanne
* Rembrandt
* Lucienne
* Raphael
* Phoenix
* Phoenix2
* Mendocino
* Genoa
* StormPeak
* DragonRange
* Mero
* HawkPoint
* StrixPoint
* GraniteRidge
* KrackanPoint
* StrixHalo
* Turin
* Bergamo

## How To

First, reference the DLL in your .NET project and import it:

```c#
using ZenStates.Core;
```

Then initialize it. `Cpu` is the main module:

```c#
private readonly Cpu cpu = new Cpu();
```

## Modules

### `Cpu.info`

| field          | type        |
|----------------|-------------|
| cpuid          | uint        |
| family         | Family      |
| codeName       | CodeName    |
| cpuName        | string      |
| packageType    | PackageType |
| baseModel      | uint        |
| extModel       | uint        |
| model          | uint        |
| ccds           | uint        |
| ccxs           | uint        |
| coresPerCcx    | uint        |
| cores          | uint        |
| logicalCores   | uint        |
| physicalCores  | uint        |
| threadsPerCore | uint        |
| patchLevel     | uint        |
| coreDisableMap | uint        |
| svi2           | SVI2        |

where `svi2` exposes the following fields corresponding to the SVI2 power planes:

| field          | type        |
|----------------|-------------|
| coreAddress    | uint        |
| socAddress     | uint        |

### `Cpu.systemInfo`

The module combines `Cpu.info` with additional information about the motherboard.

## Projects used

- [ryzen_smu (gitlab)](https://gitlab.com/leogx9r/ryzen_smu/)
- [ryzen_nb_smu (github)](https://github.com/flygoat/ryzen_nb_smu)
- [zenpower (github)](https://github.com/ocerman/zenpower)
- [Linux kernel (github)](https://github.com/torvalds/linux)
- [AMD's public documentation](https://www.amd.com/en/support/tech-docs)
- [Open Hardware Monitor](https://github.com/openhardwaremonitor/openhardwaremonitor)
- [WmiLight (nuget)](https://www.nuget.org/packages/WmiLight)

with the help of patrickschur, leogx9r, PJVol, FlyGoat, sbski, 1usmus
— and based on the original work of [irusanov/ZenStates-Core](https://github.com/irusanov/ZenStates-Core).
