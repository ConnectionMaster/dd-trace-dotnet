﻿// <auto-generated/>
#nullable enable

using Datadog.Trace.Processors;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.Tagging
{
    partial class AzureFunctionsTags
    {
        // SpanKindBytes = System.Text.Encoding.UTF8.GetBytes("span.kind");
        private static readonly byte[] SpanKindBytes = new byte[] { 115, 112, 97, 110, 46, 107, 105, 110, 100 };
        // InstrumentationNameBytes = System.Text.Encoding.UTF8.GetBytes("component");
        private static readonly byte[] InstrumentationNameBytes = new byte[] { 99, 111, 109, 112, 111, 110, 101, 110, 116 };
        // ShortNameBytes = System.Text.Encoding.UTF8.GetBytes("aas.function.name");
        private static readonly byte[] ShortNameBytes = new byte[] { 97, 97, 115, 46, 102, 117, 110, 99, 116, 105, 111, 110, 46, 110, 97, 109, 101 };
        // FullNameBytes = System.Text.Encoding.UTF8.GetBytes("aas.function.method");
        private static readonly byte[] FullNameBytes = new byte[] { 97, 97, 115, 46, 102, 117, 110, 99, 116, 105, 111, 110, 46, 109, 101, 116, 104, 111, 100 };
        // BindingSourceBytes = System.Text.Encoding.UTF8.GetBytes("aas.function.binding");
        private static readonly byte[] BindingSourceBytes = new byte[] { 97, 97, 115, 46, 102, 117, 110, 99, 116, 105, 111, 110, 46, 98, 105, 110, 100, 105, 110, 103 };
        // TriggerTypeBytes = System.Text.Encoding.UTF8.GetBytes("aas.function.trigger");
        private static readonly byte[] TriggerTypeBytes = new byte[] { 97, 97, 115, 46, 102, 117, 110, 99, 116, 105, 111, 110, 46, 116, 114, 105, 103, 103, 101, 114 };

        public override string? GetTag(string key)
        {
            return key switch
            {
                "span.kind" => SpanKind,
                "component" => InstrumentationName,
                "aas.function.name" => ShortName,
                "aas.function.method" => FullName,
                "aas.function.binding" => BindingSource,
                "aas.function.trigger" => TriggerType,
                _ => base.GetTag(key),
            };
        }

        public override void SetTag(string key, string value)
        {
            switch(key)
            {
                case "aas.function.name": 
                    ShortName = value;
                    break;
                case "aas.function.method": 
                    FullName = value;
                    break;
                case "aas.function.binding": 
                    BindingSource = value;
                    break;
                case "aas.function.trigger": 
                    TriggerType = value;
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        public override void EnumerateTags<TProcessor>(ref TProcessor processor)
        {
            if (SpanKind is not null)
            {
                processor.Process(new TagItem<string>("span.kind", SpanKind, SpanKindBytes));
            }

            if (InstrumentationName is not null)
            {
                processor.Process(new TagItem<string>("component", InstrumentationName, InstrumentationNameBytes));
            }

            if (ShortName is not null)
            {
                processor.Process(new TagItem<string>("aas.function.name", ShortName, ShortNameBytes));
            }

            if (FullName is not null)
            {
                processor.Process(new TagItem<string>("aas.function.method", FullName, FullNameBytes));
            }

            if (BindingSource is not null)
            {
                processor.Process(new TagItem<string>("aas.function.binding", BindingSource, BindingSourceBytes));
            }

            if (TriggerType is not null)
            {
                processor.Process(new TagItem<string>("aas.function.trigger", TriggerType, TriggerTypeBytes));
            }

            base.EnumerateTags(ref processor);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (SpanKind is not null)
            {
                sb.Append("span.kind (tag):")
                  .Append(SpanKind)
                  .Append(',');
            }

            if (InstrumentationName is not null)
            {
                sb.Append("component (tag):")
                  .Append(InstrumentationName)
                  .Append(',');
            }

            if (ShortName is not null)
            {
                sb.Append("aas.function.name (tag):")
                  .Append(ShortName)
                  .Append(',');
            }

            if (FullName is not null)
            {
                sb.Append("aas.function.method (tag):")
                  .Append(FullName)
                  .Append(',');
            }

            if (BindingSource is not null)
            {
                sb.Append("aas.function.binding (tag):")
                  .Append(BindingSource)
                  .Append(',');
            }

            if (TriggerType is not null)
            {
                sb.Append("aas.function.trigger (tag):")
                  .Append(TriggerType)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
    }
}