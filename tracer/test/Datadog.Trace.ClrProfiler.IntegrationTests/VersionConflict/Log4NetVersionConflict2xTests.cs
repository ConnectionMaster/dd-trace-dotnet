// <copyright file="Log4NetVersionConflict2xTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if !NETCOREAPP2_1
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.VersionConflict
{
    public class Log4NetVersionConflict2xTests : LogsInjectionTestBase
    {
        private readonly LogFileTest[] _nlog205LogFileTests =
            {
                new LogFileTest()
                {
                    FileName = "log-textFile.log",
                    RegexFormat = @"{0}: {1}",
                    UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                    PropertiesUseSerilogNaming = false
                },
                new LogFileTest()
                {
                    FileName = "log-jsonFile.log",
                    RegexFormat = @"""{0}"":{1}",
                    UnTracedLogTypes = UnTracedLogTypes.EnvServiceTracingPropertiesOnly,
                    PropertiesUseSerilogNaming = false
                }
            };

        public Log4NetVersionConflict2xTests(ITestOutputHelper output)
            : base(output, "LogsInjection.Log4Net.VersionConflict.2x")
        {
            SetServiceVersion("1.0.0");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public async Task InjectsLogsWhenEnabled()
        {
            SetEnvironmentVariable("DD_LOGS_INJECTION", "true");

            var expectedCorrelatedTraceCount = 1;
            var expectedCorrelatedSpanCount = 8;

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = MockTracerAgent.Create(Output, agentPort))
            using (await RunSampleAndWaitForExit(agent))
            {
                var spans = await agent.WaitForSpansAsync(1, 2500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");

                ValidateLogCorrelation(spans, _nlog205LogFileTests, expectedCorrelatedTraceCount, expectedCorrelatedSpanCount);
            }
        }
    }
}
#endif
