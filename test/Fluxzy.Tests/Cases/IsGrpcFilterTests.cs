// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.GrpcServices;
using Grpc.Core;
using Xunit;

namespace Fluxzy.Tests.Cases;

public class IsGrpcFilterTests
{
    [Fact]
    public async Task IsGrpcFilter_Matches_GrpcTraffic()
    {
        // Arrange — add a rule that tags gRPC exchanges with a response header
        await using var setup = await GrpcProxiedSetup.Create(setting =>
        {
            setting.AddAlterationRules(
                new AddResponseHeaderAction("x-grpc-detected", "true"),
                new IsGrpcFilter());
        });

        var client = new Greeter.GreeterClient(setup.Channel);

        // Act
        var call = client.SayHelloAsync(new HelloRequest { Name = "World" });

        // Read response headers (initial HTTP/2 HEADERS frame)
        var responseHeaders = await call.ResponseHeadersAsync;
        var reply = await call.ResponseAsync;

        // Assert — the gRPC call should have been matched
        Assert.Equal("Hello World", reply.Message);

        var detected = responseHeaders.FirstOrDefault(t => t.Key == "x-grpc-detected");
        Assert.NotNull(detected);
        Assert.Equal("true", detected.Value);
    }

    [Fact]
    public async Task IsGrpcFilter_Does_Not_Match_NonGrpcTraffic()
    {
        // Arrange — use the inverted filter: should only tag non-gRPC
        await using var setup = await GrpcProxiedSetup.Create(setting =>
        {
            setting.AddAlterationRules(
                new AddResponseHeaderAction("x-grpc-detected", "true"),
                new IsGrpcFilter { Inverted = true });
        });

        var client = new Greeter.GreeterClient(setup.Channel);

        // Act — gRPC call should NOT be tagged when filter is inverted
        var call = client.SayHelloAsync(new HelloRequest { Name = "World" });

        var responseHeaders = await call.ResponseHeadersAsync;
        var reply = await call.ResponseAsync;

        // Assert
        Assert.Equal("Hello World", reply.Message);

        var detected = responseHeaders.FirstOrDefault(t => t.Key == "x-grpc-detected");
        Assert.Null(detected);
    }

    [Fact]
    public async Task IsGrpcFilter_Works_With_Streaming()
    {
        // Arrange
        await using var setup = await GrpcProxiedSetup.Create(setting =>
        {
            setting.AddAlterationRules(
                new AddResponseHeaderAction("x-grpc-detected", "true"),
                new IsGrpcFilter());
        });

        var client = new Greeter.GreeterClient(setup.Channel);

        // Act — server streaming should also be matched
        using var call = client.SayHelloServerStream(new HelloRequest { Name = "Stream" });

        var responseHeaders = await call.ResponseHeadersAsync;

        var count = 0;

        await foreach (var reply in call.ResponseStream.ReadAllAsync())
        {
            count++;
        }

        // Assert
        Assert.Equal(5, count);

        var detected = responseHeaders.FirstOrDefault(t => t.Key == "x-grpc-detected");
        Assert.NotNull(detected);
        Assert.Equal("true", detected.Value);
    }

    [Fact]
    public async Task IsGrpcFilter_YamlRule_Matches_GrpcTraffic()
    {
        // Arrange — verify the filter works via YAML rule configuration
        var yamlContent = """
            rules:
            - filter:
                typeKind: isGrpcFilter
              action:
                typeKind: AddResponseHeaderAction
                headerName: x-grpc-yaml
                headerValue: matched
            """;

        var parser = new RuleConfigParser();
        var ruleSet = parser.TryGetRuleSetFromYaml(yamlContent, out var errors);

        Assert.NotNull(ruleSet);

        await using var setup = await GrpcProxiedSetup.Create(setting =>
        {
            foreach (var rule in ruleSet.Rules)
            {
                foreach (var action in rule.GetAllActions())
                {
                    setting.AddAlterationRules(action, rule.Filter);
                }
            }
        });

        var client = new Greeter.GreeterClient(setup.Channel);

        // Act
        var call = client.SayHelloAsync(new HelloRequest { Name = "YamlTest" });

        var responseHeaders = await call.ResponseHeadersAsync;
        var reply = await call.ResponseAsync;

        // Assert
        Assert.Equal("Hello YamlTest", reply.Message);

        var detected = responseHeaders.FirstOrDefault(t => t.Key == "x-grpc-yaml");
        Assert.NotNull(detected);
        Assert.Equal("matched", detected.Value);
    }
}
