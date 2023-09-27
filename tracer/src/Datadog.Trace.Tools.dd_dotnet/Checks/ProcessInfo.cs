// <copyright file="ProcessInfo.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Datadog.Trace.Tools.dd_dotnet.Checks;

internal class ProcessInfo
{
    public ProcessInfo(Process process, string? baseDirectory = null, IReadOnlyDictionary<string, string>? appSettings = null)
    {
        Name = process.ProcessName;
        Id = process.Id;
        EnvironmentVariables = ProcessEnvironment.ReadVariables(process);
        MainModule = process.MainModule?.FileName;

        Modules = ProcessEnvironment.ReadModules(process);

        DotnetRuntime = DetectRuntime(Modules);
        Configuration = ExtractConfigurationSource(baseDirectory, appSettings);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessInfo"/> class to be used for unit tests.
    /// </summary>
    internal ProcessInfo(string name, int id, IReadOnlyDictionary<string, string> environmentVariables, string mainModule, string[] modules)
    {
        Name = name;
        Id = id;
        EnvironmentVariables = environmentVariables;
        MainModule = mainModule;
        Modules = modules;

        DotnetRuntime = DetectRuntime(Modules);
        Configuration = ExtractConfigurationSource(null, null);
    }

    [Flags]
    public enum Runtime
    {
        Unknown = 0,
        NetFx = 1,
        NetCore = 2,
        Mixed = NetFx | NetCore
    }

    public string Name { get; }

    public int Id { get; }

    public string[] Modules { get; }

    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    public string? MainModule { get; }

    public Runtime DotnetRuntime { get; }

    public IConfigurationSource? Configuration { get; }

    public static ProcessInfo? GetProcessInfo(int pid, string? baseDirectory = null, IReadOnlyDictionary<string, string>? appSettings = null)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return new ProcessInfo(process, baseDirectory, appSettings);
        }
        catch (Exception ex)
        {
            Utils.WriteError("Error while trying to fetch process information: " + ex);
            return null;
        }
    }

    public IReadOnlyList<int> GetChildProcesses()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Array.Empty<int>();
        }

        var pids = new List<int>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var handle = process.Handle;

                int queryStatus = Windows.NativeMethods.NtQueryInformationProcess(handle, 0, out var pbi, Marshal.SizeOf<Windows.NativeMethods.PROCESS_BASIC_INFORMATION>(), out _);

                if (queryStatus == 0 && pbi.InheritedFromUniqueProcessId == Id)
                {
                    pids.Add(process.Id);
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return pids;
    }

    private static Runtime DetectRuntime(string[] modules)
    {
        var result = Runtime.Unknown;

        foreach (var module in modules)
        {
            var fileName = Path.GetFileName(module);

            if (fileName.Equals("clr.dll", StringComparison.OrdinalIgnoreCase))
            {
                result |= Runtime.NetFx;
            }
            else if (fileName.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase)
                  || fileName.Equals("libcoreclr.so", StringComparison.OrdinalIgnoreCase))
            {
                result |= Runtime.NetCore;
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string>? LoadApplicationConfig(string? mainModule)
    {
        if (mainModule == null)
        {
            return null;
        }

        var folder = Path.GetDirectoryName(mainModule);

        if (folder == null)
        {
            return null;
        }

        var configFileName = Path.GetFileName(mainModule) + ".config";
        var configPath = Path.Combine(folder, configFileName);

        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var document = XDocument.Load(configPath);

            var appSettings = document.Element("configuration")?.Element("appSettings");

            if (appSettings == null)
            {
                return null;
            }

            var settings = new Dictionary<string, string>();

            foreach (var setting in appSettings.Elements())
            {
                var key = setting.Attribute("key")?.Value;
                var value = setting.Attribute("value")?.Value;

                if (key != null)
                {
                    settings[key] = value ?? string.Empty;
                }
            }

            return settings;
        }
        catch (Exception ex)
        {
            Utils.WriteWarning($"An error occured while parsing the configuration file {configPath}: {ex.Message}");
            return null;
        }
    }

    private IConfigurationSource ExtractConfigurationSource(string? baseDirectory, IReadOnlyDictionary<string, string>? appSettings)
    {
        baseDirectory ??= Path.GetDirectoryName(MainModule);

        var configurationSource = new CompositeConfigurationSource();

        configurationSource.Add(new DictionaryConfigurationSource(EnvironmentVariables));

        if (appSettings != null)
        {
            configurationSource.Add(new DictionaryConfigurationSource(appSettings));
        }
        else if (DotnetRuntime.HasFlag(Runtime.NetFx))
        {
            var appConfigSource = LoadApplicationConfig(MainModule);

            if (appConfigSource != null)
            {
                configurationSource.Add(new DictionaryConfigurationSource(appConfigSource));
            }
        }

        var jsonConfigurationSource = JsonConfigurationSource.TryLoadJsonConfigurationFile(configurationSource, baseDirectory);

        if (jsonConfigurationSource != null)
        {
            configurationSource.Add(jsonConfigurationSource);
        }

        return configurationSource;
    }
}