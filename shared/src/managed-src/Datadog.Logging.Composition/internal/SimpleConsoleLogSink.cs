// <copyright file="SimpleConsoleLogSink.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Logging.Emission;

namespace Datadog.Logging.Composition
{
    internal sealed class SimpleConsoleLogSink : ILogSink
    {
        public static readonly SimpleConsoleLogSink SingeltonInstance = new SimpleConsoleLogSink();

        public bool TryLogError(LogSourceInfo logSourceInfo, string message, Exception exception, IEnumerable<object> dataNamesAndValues)
        {
            try
            {
                SimpleConsoleSink.Error(
                    logSourceInfo.LogSourceNamePart1,
                    logSourceInfo.LogSourceNamePart2,
                    logSourceInfo.CallLineNumber,
                    logSourceInfo.CallMemberName,
                    logSourceInfo.CallFileName,
                    logSourceInfo.AssemblyName,
                    message,
                    exception,
                    dataNamesAndValues);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryLogInfo(LogSourceInfo logSourceInfo, string message, IEnumerable<object> dataNamesAndValues)
        {
            try
            {
                SimpleConsoleSink.Info(
                    logSourceInfo.LogSourceNamePart1,
                    logSourceInfo.LogSourceNamePart2,
                    logSourceInfo.CallLineNumber,
                    logSourceInfo.CallMemberName,
                    logSourceInfo.CallFileName,
                    logSourceInfo.AssemblyName,
                    message,
                    dataNamesAndValues);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryLogDebug(LogSourceInfo logSourceInfo, string message, IEnumerable<object> dataNamesAndValues)
        {
            try
            {
                SimpleConsoleSink.Debug(
                    logSourceInfo.LogSourceNamePart1,
                    logSourceInfo.LogSourceNamePart2,
                    logSourceInfo.CallLineNumber,
                    logSourceInfo.CallMemberName,
                    logSourceInfo.CallFileName,
                    logSourceInfo.AssemblyName,
                    message,
                    dataNamesAndValues);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}