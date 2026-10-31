# ZenStates-Core-AOT

> A **Native AOT compatible** fork of [`irusanov/ZenStates-Core`](https://github.com/irusanov/ZenStates-Core),
> maintained to provide the AOT-ready low-level foundation for the main project **Zentwik**.

## Fork & AOT Changes

### English
This repository is a maintained fork of the original ZenStates-Core by
[irusanov](https://github.com/irusanov). Its sole purpose is to remove every obstacle that
prevented the library from being consumed by a **Native AOT** (.NET ILC) application.

Key changes relative to upstream:

- **Removed the `System.Management` (WMI) dependency.** The original code relied on WMI for SMU
  function discovery and DRAM timing probing. `System.Management` is built on late-bound COM
  interop that Native AOT (ILC) cannot compile — publishing the library produced
  `IL2104` / `IL3053` warnings and every WMI call crashed at runtime. The dependency has been
  **fully replaced** with the AOT-safe [`WmiLight`](https://www.nuget.org/packages/WmiLight)
  NuGet package.
- **Dropped the `net20` target framework.** Only modern .NET targets remain
  (`netstandard2.1`, `net8.0-windows`), because WmiLight requires at least `netstandard2.0`.
- **Fixed Native AOT trimming warnings.** Several `IL2091` / `IL2026` / `IL3000` warnings that
  broke AOT trimming were eliminated by replacing ineffective `#pragma warning disable`
  suppressions (which the trim analyzer ignores) with real AOT annotations
  (`DynamicallyAccessedMembers`, `UnconditionalSuppressMessage`).

The public API surface (`Cpu`, `Aod`, `WMI`, etc.) keeps the same member names, parameter names
and return semantics. The only change underneath is that the WMI object types exposed by `WMI`
(`ManagementScope` / `ManagementObject` / `ManagementBaseObject`) are now thin AOT-safe wrappers
(`WmiScope` / `WmiInstance`). The `WMI.RunCommand` method was removed because WmiLight exposes no
`byte[]` parameter setter for it, and it had no callers in this assembly.

### 中文
本仓库是 irusanov 原版 ZenStates-Core 的维护分支，主要目的是为上层主项目 **Zentwik** 提供
**Native AOT 兼容**的底层支持，消除一切阻碍该库被 Native AOT（.NET ILC）应用消费的因素。

相对上游的核心变更：

- **移除 `System.Management`（WMI）依赖。** 原代码通过 WMI 进行 SMU 函数发现与 DRAM 时序探测。
  `System.Management` 基于晚绑定 COM 互操作，Native AOT（ILC）无法编译——发布该库会产生
  `IL2104` / `IL3053` 警告，且所有 WMI 调用在运行时崩溃。该依赖已**全部替换**为 AOT 兼容的
  [`WmiLight`](https://www.nuget.org/packages/WmiLight) NuGet 包。
- **移除 `net20` 目标框架。** 仅保留现代 .NET 目标（`netstandard2.1`、`net8.0-windows`），
  因为 WmiLight 最低要求 `netstandard2.0`。
- **修复 Native AOT 裁剪警告。** 用真正的 AOT 标注（`DynamicallyAccessedMembers`、
  `UnconditionalSuppressMessage`）替换了无效的 `#pragma warning disable` 抑制（裁剪分析器会
  忽略 pragma），从而消除了 IL2091 / IL2026 / IL3000 等警告。

公共 API 表面（`Cpu`、`Aod`、`WMI` 等）的成员名、参数名与返回语义保持不变；底层唯一的变化是
`WMI` 暴露的 WMI 对象类型（`ManagementScope` / `ManagementObject` / `ManagementBaseObject`）
改为轻量的 AOT 安全包装（`WmiScope` / `WmiInstance`）。`WMI.RunCommand` 方法已被移除，因为
WmiLight 未提供对应的 `byte[]` 入参设置器，且该装配内无调用者。

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
First, reference the DLL in your .NET project and import it.
```c#
using ZenStates.Core;
```
Then initialize it. Cpu is the main module.
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

where svi2 exposes the following fields corresponding to the SVI2 power planes

| field          | type        |
|----------------|-------------|
| coreAddress    | uint        |
| socAddress     | uint        |

### `Cpu.systemInfo`

The module combines `Cpu.info` with additional information about the motherboard

## Projects used
[ryzen_smu (gitlab)](https://gitlab.com/leogx9r/ryzen_smu/)
[ryzen_nb_smu (github)](https://github.com/flygoat/ryzen_nb_smu)
[zenpower (github)](https://github.com/ocerman/zenpower)
[Linux kernel (github)](https://github.com/torvalds/linux)
[AMD's public documentation](https://www.amd.com/en/support/tech-docs)
[Open Hardware Monitor](https://github.com/openhardwaremonitor/openhardwaremonitor)
[WmiLight (nuget)](https://www.nuget.org/packages/WmiLight)

with the help of patrickschur, leogx9r, PJVol, FlyGoat, sbski, 1usmus
— and based on the original work of [irusanov/ZenStates-Core](https://github.com/irusanov/ZenStates-Core).
