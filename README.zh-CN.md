# ZenStates-Core-AOT

[English](README.md)

> 这是 [`irusanov/ZenStates-Core`](https://github.com/irusanov/ZenStates-Core) 的
> **Native AOT 兼容**维护分支，为上层主项目 **Zentwik** 提供 AOT-ready 的底层基础。

> [!IMPORTANT]
> 本分支为 Native AOT 兼容版，WMI 相关功能（如 `AOD.GetWmiFunctions`）必须在管理员权限下运行。

## Fork 与 AOT 变更

本仓库是 irusanov 原版 ZenStates-Core 的维护分支，主要目的是为上层主项目 **Zentwik** 提供
**Native AOT 兼容**的底层支持，消除一切阻碍该库被 Native AOT（.NET ILC）应用消费的因素。

相对上游的核心变更：

- **移除 `System.Management`（WMI）依赖。** 原代码通过 WMI 进行 SMU 函数发现与 DRAM 时序探测。
  `System.Management` 基于晚绑定 COM 互操作，Native AOT（ILC）无法编译——发布该库会产生
  `IL2104` / `IL3053` 警告，且所有 WMI 调用都会在运行时崩溃。该依赖已**全部替换**为 AOT 兼容的
  [`WmiLight`](https://www.nuget.org/packages/WmiLight) NuGet 包。
- **移除 `net20` 目标框架。** 仅保留现代 .NET 目标（`netstandard2.1`、`net8.0-windows`），
  因为 WmiLight 最低要求 `netstandard2.0`。
- **修复 Native AOT 裁剪警告。** 用真正的 AOT 标注（`DynamicallyAccessedMembers`、
  `UnconditionalSuppressMessage`）替换了无效的 `#pragma warning disable` 抑制（裁剪分析器会
  忽略 pragma），从而消除了 IL2091 / IL2026 / IL3000 等警告。

公共 API 表面（`Cpu`、`Aod`、`WMI` 等）的成员名、参数名与返回语义保持不变；底层唯一的变化是，
`WMI` 暴露的 WMI 对象类型（`ManagementScope` / `ManagementObject` / `ManagementBaseObject`）
改为轻量的 AOT 安全包装（`WmiScope` / `WmiInstance`）。`WMI.RunCommand` 方法已被移除，因为
WmiLight 未提供对应的 `byte[]` 入参设置器，且本程序集中无调用者。

## CPU 支持

支持以下 AMD Zen 系列代号：

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

## 使用方法

首先在 .NET 项目中引用 DLL 并导入：

```c#
using ZenStates.Core;
```

然后初始化。`Cpu` 是主模块：

```c#
private readonly Cpu cpu = new Cpu();
```

## 模块

### `Cpu.info`

| 字段           | 类型        |
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

其中 `svi2` 暴露以下字段，对应 SVI2 电源平面：

| 字段        | 类型 |
|-------------|------|
| coreAddress | uint |
| socAddress  | uint |

### `Cpu.systemInfo`

该模块将 `Cpu.info` 与主板附加信息组合在一起。

## 使用的项目

- [ryzen_smu (gitlab)](https://gitlab.com/leogx9r/ryzen_smu/)
- [ryzen_nb_smu (github)](https://github.com/flygoat/ryzen_nb_smu)
- [zenpower (github)](https://github.com/ocerman/zenpower)
- [Linux kernel (github)](https://github.com/torvalds/linux)
- [AMD 公开文档](https://www.amd.com/en/support/tech-docs)
- [Open Hardware Monitor](https://github.com/openhardwaremonitor/openhardwaremonitor)
- [WmiLight (nuget)](https://www.nuget.org/packages/WmiLight)

感谢 patrickschur、leogx9r、PJVol、FlyGoat、sbski、1usmus 的帮助，
并基于 [irusanov/ZenStates-Core](https://github.com/irusanov/ZenStates-Core) 的原始工作。
