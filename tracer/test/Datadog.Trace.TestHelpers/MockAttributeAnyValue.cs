// <copyright file="MockAttributeAnyValue.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using MessagePack;

namespace Datadog.Trace.TestHelpers
{
    [MessagePackObject]
    public class MockAttributeAnyValue
    {
        [Key("type")]
        public int Type { get; set; }

        [Key("string_value")]
        public string StringValue { get; set; }

        [Key("bool_value")]
        public bool? BoolValue { get; set; }

        [Key("int_value")]
        public long? IntValue { get; set; }

        [Key("double_value")]
        public double? DoubleValue { get; set; }

        [Key("array_value")]
        public MockAttributeArray ArrayValue { get; set; }
    }
}
