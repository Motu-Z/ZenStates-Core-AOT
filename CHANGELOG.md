# Changelog

All notable changes to this fork are documented here.
This project follows the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

## [Unreleased]

### Changed
- Replaced the `System.Management` (WMI) dependency with the AOT-safe [`WmiLight`](https://www.nuget.org/packages/WmiLight) 7.3.0 package. `System.Management` is built on late-bound COM interop that Native AOT (ILC) cannot compile (`IL2104` / `IL3053`).

### Removed
- Removed the `net20` target framework; only `netstandard2.1` and `net8.0-windows` remain.
- Removed the dead `WMI.RunCommand` API (no in-repo callers; WmiLight exposes no `byte[]` parameter setter).

### Fixed
- Fixed the Native AOT trimming warnings (`IL2091`, `IL2026`, `IL3000`) by replacing ineffective `#pragma` suppressions with real AOT annotations (`DynamicallyAccessedMembers`, `UnconditionalSuppressMessage`).
