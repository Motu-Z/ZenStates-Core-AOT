using System;
using System.Collections.Generic;
using ZenStates.Core;
using ZenStates.Core.Hardware;
using ZenStates.Core.Hardware.Aod;

internal static class Program
{
    private static int failed;
    private static int skipped;

    private static void Check(string name, bool ok, string detail)
    {
        if (ok)
        {
            Console.WriteLine("  PASS  " + name + " :: " + detail);
            return;
        }

        failed++;
        Console.WriteLine("  FAIL  " + name + " :: " + detail);
    }

    private static void Skip(string name, string reason)
    {
        skipped++;
        Console.WriteLine("  SKIP  " + name + " :: " + reason);
    }

    private static void Run(string name, Func<string> body)
    {
        try
        {
            Check(name, true, body());
        }
        catch (Exception ex)
        {
            Check(name, false, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static int Main()
    {
        Console.WriteLine("== ZenStates.Core smoke test ==");

        if (!ZenStates.Core.PawnIo.PawnIo.IsInstalled)
        {
            Skip("Cpu..ctor", "PawnIO driver not installed");
        }
        else
        {
            try
            {
                using (Cpu cpu = new Cpu())
                    Check("Cpu..ctor", true, "cpuName=" + cpu.info.cpuName);
            }
            catch (Exception ex) when (
                ex is ApplicationException ||
                ex is UnauthorizedAccessException ||
                ex is System.ComponentModel.Win32Exception)
            {
                Skip("Cpu..ctor", "prerequisite missing: " + ex.Message);
            }
            catch (Exception ex)
            {
                Check("Cpu..ctor", false, ex.GetType().Name + ": " + ex.Message);
            }
        }

        Run("WMI.Connect", () =>
        {
            using (WmiScope scope = WMI.Connect(@"root\cimv2"))
                return "IsConnected=" + scope.IsConnected + " path=" + scope.ScopePath;
        });

        Run("WMI.Query", () =>
        {
            using (WmiInstance processor = WMI.Query(@"root\cimv2", "Win32_Processor"))
            {
                string name = processor == null ? null : processor["Name"] as string;
                if (string.IsNullOrEmpty(name))
                    throw new Exception("Win32_Processor.Name is empty");

                return "Name=" + name;
            }
        });

        Run("WMI.GetInstanceName", () =>
        {
            string instance = WMI.GetInstanceName(@"root\wmi", "AMD_ACPI");
            return "AMD_ACPI InstanceName='" + instance + "' (empty off-AMD or without admin is expected)";
        });

        Run("WMI.GetWmiNamespaces", () =>
        {
            List<string> namespaces = WMI.GetWmiNamespaces("root");
            if (namespaces.Count == 0)
                throw new Exception("no namespaces returned");

            return "count=" + namespaces.Count;
        });

        Run("WMI.GetClassNamesWithinWmiNamespace", () =>
        {
            List<string> classes = WMI.GetClassNamesWithinWmiNamespace(@"root\cimv2");
            if (classes.Count <= 100 || !classes.Contains("Win32_Processor"))
                throw new Exception("unexpected class set, count=" + classes.Count);

            return "count=" + classes.Count;
        });

        Run("WMI.InvokeMethod", () =>
        {
            using (WmiInstance process = WMI.Query(@"root\cimv2", "Win32_Process WHERE ProcessId = " + Environment.ProcessId))
            {
                if (process == null)
                    throw new Exception("own process instance not found");

                using (WmiInstance outParams = WMI.InvokeMethod(process, "GetOwner", null, 0))
                {
                    string user = outParams == null ? null : outParams["User"] as string;
                    if (user == null)
                        throw new Exception("GetOwner returned no User");

                    WMI.InvokeMethodAndGetValue(process, "GetOwner", "User", null, 0);
                    return "User=" + user;
                }
            }
        });

        Run("WMI.TryGetProperty", () =>
        {
            using (WmiInstance os = WMI.Query(@"root\cimv2", "Win32_OperatingSystem"))
            {
                object caption = WMI.TryGetProperty(os, "Caption");
                object missing = WMI.TryGetProperty(os, "NoSuchProperty_xyz");

                if (caption == null)
                    throw new Exception("Caption is null");
                if (missing != null)
                    throw new Exception("unknown property did not degrade to null");

                return "Caption=" + caption;
            }
        });

        Run("AOD.GetWmiFunctions", () =>
        {
            Dictionary<string, uint> functions = AOD.GetWmiFunctions();
            if (functions == null)
                throw new Exception("null dictionary");

            return "functions=" + functions.Count + " hasRMP=" + functions.ContainsKey("Set RMP Profile")
                + " (0 off-AMD or without admin is expected)";
        });

        Console.WriteLine(failed == 0
            ? "== ALL PASS" + (skipped > 0 ? " (" + skipped + " skipped)" : "") + " =="
            : "== FAILURES: " + failed + " ==");

        return failed == 0 ? 0 : 1;
    }
}
