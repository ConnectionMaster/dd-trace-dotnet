// <copyright file="IChannel.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;

namespace Datadog.Trace.Ci.Ipc;

internal interface IChannel : IDisposable
{
    int BufferBodySize { get; }

    IChannelReader GetReader();

    IChannelWriter GetWriter();
}