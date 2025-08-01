// <copyright file="HotChocolateTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#if NET5_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class HotChocolateSchemaV0Tests : HotChocolateTests
    {
        public HotChocolateSchemaV0Tests(ITestOutputHelper output)
            : base(output, metadataSchemaVersion: "v0")
        {
        }
    }

    public class HotChocolateSchemaV1Tests : HotChocolateTests
    {
        public HotChocolateSchemaV1Tests(ITestOutputHelper output)
            : base(output, metadataSchemaVersion: "v1")
        {
        }
    }

    public abstract class HotChocolateTests : HotChocolateTestsBase
    {
        public HotChocolateTests(ITestOutputHelper output, string metadataSchemaVersion)
            : base("HotChocolate", output, nameof(HotChocolateTests), metadataSchemaVersion)
        {
        }

        [SkippableTheory]
        [MemberData(nameof(PackageVersions.HotChocolate), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        public async Task SubmitsTracesHttp(string packageVersion)
            => await RunSubmitsTraces(packageVersion);

        [SkippableTheory]
        [MemberData(nameof(PackageVersions.HotChocolate), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        public async Task SubmitsTracesWebsockets(string packageVersion)
            => await RunSubmitsTraces(packageVersion, true);
    }

    [UsesVerify]
    public abstract class HotChocolateTestsBase : TracingIntegrationTest
    {
        private const string ServiceVersion = "1.0.0";

        private readonly string _testName;
        private readonly string _metadataSchemaVersion;
        private readonly Regex _timeUnixNanoRegex = new(@"time_unix_nano"":([0-9]{10}[0-9]+)");
        private readonly Regex _stacktraceRegex = new(@"""stacktrace"":""   at Samples\.HotChocolate\.Query\.ThrowException\([^""]*""");

        protected HotChocolateTestsBase(string sampleAppName, ITestOutputHelper output, string testName, string metadataSchemaVersion)
            : base(sampleAppName, output)
        {
            SetServiceVersion(ServiceVersion);
            SetEnvironmentVariable("DD_TRACE_SPAN_ATTRIBUTE_SCHEMA", metadataSchemaVersion);
            SetEnvironmentVariable("DD_TRACE_GRAPHQL_ERROR_EXTENSIONS", "bool,int,float,str,other,sbyte,byte,short,ushort,uint,long,ulong,decimal,double,char");

            _testName = testName;
            _metadataSchemaVersion = metadataSchemaVersion;
        }

        public override Result ValidateIntegrationSpan(MockSpan span, string metadataSchemaVersion) => span.IsHotChocolate(metadataSchemaVersion);

        protected async Task RunSubmitsTraces(string packageVersion = "", bool usingWebsockets = false)
        {
            using var fixture = new AspNetCoreTestFixture();
            SetInstrumentationVerification();

            var isExternalSpan = _metadataSchemaVersion == "v0";
            var clientSpanServiceName = isExternalSpan ? $"{EnvironmentHelper.FullSampleName}-graphql" : EnvironmentHelper.FullSampleName;

            var agentConfiguration = new MockTracerAgent.AgentConfiguration
            {
                SpanEvents = false
            };

            await fixture.TryStartApp(this, packageVersion: packageVersion, agentConfiguration: agentConfiguration);
            var testStart = DateTime.UtcNow;
            var expectedSpans = await SubmitRequests(fixture.HttpPort, usingWebsockets);

            var spans = await fixture.Agent.WaitForSpansAsync(count: expectedSpans, minDateTime: testStart, returnAllOperations: true);

            var graphQLSpans = spans.Where(span => span.Type == "graphql");
            ValidateIntegrationSpans(graphQLSpans, metadataSchemaVersion: "v0", expectedServiceName: clientSpanServiceName, isExternalSpan);

            var settings = VerifyHelper.GetSpanVerifierSettings();
            settings.AddRegexScrubber(_timeUnixNanoRegex, @"time_unix_nano"":<DateTimeOffset.Now>");
            settings.AddRegexScrubber(_stacktraceRegex, @"""stacktrace"":""   at Samples.HotChocolate.Query.ThrowException() in Query.cs:line 00""");

            var versionSuffix = GetSuffix(packageVersion);

            await VerifyHelper.VerifySpans(spans, settings)
                              .UseFileName($"{_testName}{(usingWebsockets ? "Websockets" : string.Empty)}.SubmitsTraces.Schema{_metadataSchemaVersion.ToUpper()}{versionSuffix}")
                              .DisableRequireUniquePrefix(); // all package versions should be the same

            VerifyInstrumentation(fixture.Process);
        }

        private async Task<int> SubmitRequests(int aspNetCorePort, bool usingWebsockets)
        {
            var expectedGraphQlExecuteSpanCount = 0;
            var expectedAspNetcoreRequestSpanCount = 0;

            if (usingWebsockets)
            {
                await SubmitWebsocketRequests();
            }
            else
            {
                SubmitHttpRequests();
            }

            void SubmitHttpRequests()
            {
                // SUCCESS: query using GET
                SubmitGraphqlRequest(url: "/graphql?query=" + WebUtility.UrlEncode("query{book{title author{name}}}"), httpMethod: "GET", graphQlRequestBody: null);

                // SUCCESS: query using POST (default)
                SubmitGraphqlRequest(url: "/graphql", httpMethod: "POST", graphQlRequestBody: @"{""query"":""query Book{book{title author{name}}}""}");

                // SUCCESS: mutation
                SubmitGraphqlRequest(url: "/graphql", httpMethod: "POST", graphQlRequestBody: @"{""query"":""mutation m{addBook(book:{title:\""New Book\""}){book{title}}}""}");

                // FAILURE: query fails 'validate' step
                SubmitGraphqlRequest(url: "/graphql", httpMethod: "POST", graphQlRequestBody: @"{""query"":""{boook{title author{name}}}""}");

                // FAILURE: query fails 'execute' step
                SubmitGraphqlRequest(url: "/graphql", httpMethod: "POST", graphQlRequestBody: @"{""query"": ""query ErrorQuery{throwException}""}");
            }

            async Task SubmitWebsocketRequests()
            {
                // SUCCESS: query using Websocket
                await SubmitGraphqlWebsocketRequest(url: "/graphql", httpMethod: null, graphQlRequestBody: @"{""type"": ""start"",""id"": ""1"",""payload"": {""query"": ""query Book{book{title author{name}}}"",""variables"": {}}}");

                // SUCCESS: mutation using Websocket
                await SubmitGraphqlWebsocketRequest(url: "/graphql", httpMethod: null, graphQlRequestBody: @"{""type"": ""start"",""id"": ""1"",""payload"": {""query"": ""mutation m{addBook(book:{title:\""New Book\""}){book{title}}}"",""variables"": {}}}");

                // FAILURE: query fails 'execute' step using Websocket
                await SubmitGraphqlWebsocketRequest(url: "/graphql", httpMethod: null, graphQlRequestBody: @"{""type"": ""start"",""id"": ""1"",""payload"": {""query"": ""query ErrorQuery{throwException}"",""variables"": {}}}");

                // FAILURE: query fails 'validate' step using Websocket
                await SubmitGraphqlWebsocketRequest(url: "/graphql", httpMethod: null, graphQlRequestBody: @"{""type"": ""start"",""id"": ""1"",""payload"": {""query"": ""{boook{title author{name}}}"",""variables"": {}}}");
            }

            return expectedGraphQlExecuteSpanCount + expectedAspNetcoreRequestSpanCount;

            void SubmitGraphqlRequest(
                string url,
                string httpMethod,
                string graphQlRequestBody)
            {
                expectedGraphQlExecuteSpanCount++;
                expectedAspNetcoreRequestSpanCount++;

                GraphQLCommon.SubmitRequest(
                    Output,
                    aspNetCorePort,
                    new GraphQLCommon.RequestInfo { Url = url, HttpMethod = httpMethod, RequestBody = graphQlRequestBody, });
            }

            async Task SubmitGraphqlWebsocketRequest(
                string url,
                string httpMethod,
                string graphQlRequestBody)
            {
                expectedGraphQlExecuteSpanCount++;
                expectedAspNetcoreRequestSpanCount++;

                await GraphQLCommon.SubmitWebsocketRequest(
                    Output,
                    aspNetCorePort,
                    new GraphQLCommon.RequestInfo() { Url = url, HttpMethod = httpMethod, RequestBody = graphQlRequestBody, });
            }
        }

        private string GetSuffix(string packageVersion)
        {
            // An empty package version means we're in the sample,
            // which targets the latest version of the package.
            if (string.IsNullOrEmpty(packageVersion) || new Version(packageVersion) >= new Version("13.1.0"))
            {
                return string.Empty;
            }

            return ".Pre_13_1_0";
        }
    }
}

#endif
