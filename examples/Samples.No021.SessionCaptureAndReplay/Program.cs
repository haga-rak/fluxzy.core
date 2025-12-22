// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak
using System.Net;
using Fluxzy;

// Captures browser session data (cookies, headers) and replays them via curl
// Use case: Access authenticated API endpoints through proxy session replay

Console.WriteLine("=== Fluxzy Session Capture & Replay Example ===\n");

var proxyPort = 44344;
var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, proxyPort);

// YAML rules: capture session data from responses, apply to curl requests
var yamlRules = """
    rules:
      # Capture cookies and auth headers from all responses
      - filter:
          typeKind: AnyFilter
        action:
          typeKind: CaptureSessionAction
          captureCookies: true
          captureHeaders:
            - Authorization
            - X-CSRF-Token
            - X-Auth-Token
      # Apply captured session to curl requests
      - filter:
          typeKind: RequestHeaderFilter
          headerName: User-Agent
          pattern: curl
          operation: Contains
        action:
          typeKind: ApplySessionAction
          applyCookies: true
          applyHeaders: true
          mergeWithExisting: true
    """;

setting.AddAlterationRules(yamlRules);

Console.WriteLine("YAML Rules Configuration:");
Console.WriteLine(new string('-', 40));
Console.WriteLine(yamlRules);
Console.WriteLine(new string('-', 40));
Console.WriteLine();

await using var proxy = new Proxy(setting);
var endpoints = proxy.Run();
var endpoint = endpoints.First();

Console.WriteLine($"Proxy listening on: http://{endpoint.Address}:{endpoint.Port}\n");
Console.WriteLine("=== How to use ===\n");
Console.WriteLine($"1. Configure browser proxy: {endpoint.Address}:{endpoint.Port}");
Console.WriteLine("2. Trust Fluxzy certificate: fluxzy cert export -f pem");
Console.WriteLine("3. Navigate and authenticate in browser");
Console.WriteLine("4. Use curl through proxy:\n");
Console.WriteLine($"   curl -x http://127.0.0.1:{proxyPort} -k https://example.com/api/user\n");
Console.WriteLine(new string('-', 60));
Console.WriteLine("Press ENTER to stop...");
Console.WriteLine(new string('-', 60));

Console.ReadLine();
Console.WriteLine("\nStopping proxy...");
Console.WriteLine("Done!");