// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class Variables
    {
        [Theory]
        [InlineData("yaya")]
        [InlineData("")]
        public async Task Inject_From_GlobalEv(string input)
        {
            Environment.SetEnvironmentVariable(nameof(Inject_From_GlobalEv), input);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            var rule = """
rules:
  - filter:
      typeKind: AnyFilter
    actions: 
      - typeKind: AddResponseHeaderAction
        headerName: my_name_is_${env.Inject_From_GlobalEv}
        headerValue: injected_from_ev

""";

            var expectedValue = "my_name_is_" + input;

            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.Contains(exchange.ResponseHeader.Headers, a => a.Name.ToString() == expectedValue); 
        }

        [Fact]
        public async Task Inject_From_GlobalEv_Unknown_Var()
        {
            Environment.SetEnvironmentVariable(nameof(Inject_From_GlobalEv), "yaya");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            var rule = """
rules:
  - filter:
      typeKind: AnyFilter
    actions: 
      - typeKind: AddResponseHeaderAction
        headerName: my_name_is_${Inject_FWrong_Varv}
        headerValue: injected_from_ev

""";

            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.Contains(exchange.ResponseHeader.Headers, t => t.Name.ToString() == "my_name_is_");
        }

        [Fact]
        public async Task String_Filter_Extract_A_Value_To_File()
        {
            var randomFileName = $"testartifacts/${nameof(String_Filter_Extract_A_Value_To_File)}{Guid.NewGuid()}.txt";

            var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            var rule = """
rules:
- filter:
    typeKind: hostFilter
    pattern: (?<myvar>[^.]+)\.fluxzy\.io
    operation: regex
  actions: 
  - typeKind: FileAppendAction
    filename: filenamevalue
    text: "${user.myvar}"  
    runScope: ResponseHeaderReceivedFromRemote

""";

            rule = rule.Replace("filenamevalue", randomFileName);

            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);
            var fileContent = File.ReadAllText(randomFileName);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.Equal("sandbox", fileContent);
        }


        [Fact]
        public async Task String_Filter_Extract_A_Value_To_File_Main_Example()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "a_bearer_token");

            if (File.Exists("token-file.txt"))
                File.Delete("token-file.txt");

            var rule = """
rules:
- filter: 
    typeKind: requestHeaderFilter
    headerName: authorization # select only request with authorization header
    operation: regex
    pattern: "Bearer (?<BEARER_TOKEN>.*)" # A named regex instructs fluxzy
                                           # to extract token from authorization
                                           # header into the variable BEARER_TOKEN
  action : 
    # Write the token on file 
    typeKind: FileAppendAction # Append the token to the file
    filename: token-file.txt # save the token to token-file.txt
    text: "${authority.host} --> ${user.BEARER_TOKEN}\r\n"  # user.BEARER_TOKEN retrieves 
                                          # the previously captured variables 
    runScope: RequestHeaderReceivedFromClient  # run the action when the request header 
                                               # is received from client

""";
            
            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.True(File.Exists("token-file.txt"), "File does not exists");
            Assert.Equal("sandbox.fluxzy.io --> "  + "a_bearer_token", File.ReadAllLines("token-file.txt").First());
        }

        [Fact]
        public async Task Request_Header_Extract_A_Value_Comment()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "a_bearer_token");

            var rule = """
rules:
- filter:
    typeKind: requestHeaderFilter
    headerName: Authorization
    pattern: Bearer (?<token>.+)
    operation: regex
  actions: 
  - typeKind: applyCommentAction
    comment: "${user.token}" 
""";

            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.Equal("a_bearer_token", exchange.Comment);
        }

        [Theory]
        [InlineData("authority.host", "KnownAuthority")]
        [InlineData("authority.port", "KnownPort")]
        [InlineData("authority.secure", "Secure")]
        [InlineData("exchange.url", "FullUrl")]
        [InlineData("exchange.method", "Method")]
        [InlineData("exchange.status", "StatusCode")]
        [InlineData("exchange.id", "Id")]
        public async Task Self_Generated_Context_Variables(string variableName, string propertyName)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            var rule = """
rules:
- filter:
    typeKind: Any
  actions: 
  - typeKind: applyComment
    comment: "${variable_name}" 
""";

            rule = rule.Replace("variable_name", variableName);

            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);
            Assert.NotNull(exchange.ResponseHeader);
            Assert.Equal(exchange.ReadProperty(propertyName), exchange.Comment);
        }
    }

    internal static class ReflectionHelper
    {
        public static string? ReadProperty<T>(this T obj, string propertyName)
        {
            var property = typeof(T).GetProperty(propertyName);

            return property?.GetValue(obj)?.ToString();
        }
    }
}
