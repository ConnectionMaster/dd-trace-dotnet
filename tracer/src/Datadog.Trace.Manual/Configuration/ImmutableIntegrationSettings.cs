﻿// <copyright file="ImmutableIntegrationSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System.Runtime.CompilerServices;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Configuration;

/// <summary>
/// Contains integration-specific settings.
/// </summary>
public sealed class ImmutableIntegrationSettings
{
    internal ImmutableIntegrationSettings(string name, bool? enabled, bool? analyticsEnabled, double analyticsSampleRate)
    {
        IntegrationName = name;
        Enabled = enabled;
        AnalyticsEnabled = analyticsEnabled;
        AnalyticsSampleRate = analyticsSampleRate;
    }

    internal ImmutableIntegrationSettings(string name)
    {
        IntegrationName = name;
    }

    /// <summary>
    /// Gets the name of the integration. Used to retrieve integration-specific settings.
    /// </summary>
    public string IntegrationName
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get;
    }

    /// <summary>
    /// Gets a value indicating whether
    /// this integration is enabled.
    /// </summary>
    public bool? Enabled
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get;
    }

    /// <summary>
    /// Gets a value indicating whether
    /// Analytics are enabled for this integration.
    /// </summary>
    public bool? AnalyticsEnabled
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get;
    }

    /// <summary>
    /// Gets a value between 0 and 1 (inclusive)
    /// that determines the sampling rate for this integration.
    /// </summary>
    public double AnalyticsSampleRate
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get;
    }
}
