// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy;

// This example demonstrates how to use CaptureSessionAction and ApplySessionAction
// to capture browser session data (cookies, authorization headers) and replay them
// via curl or other HTTP clients.
//
// Use case: Access authenticated API endpoints that are normally only accessible
// through a browser session.
//
// Rules are defined using YAML configuration format.
// See https://www.fluxzy.io/rule/syntax for full documentation.

Console.WriteLine("=== Fluxzy Session Capture & Replay Example ===\n");

var proxyPort = 44344;

// Create proxy settings
var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, proxyPort);

// Define rules using YAML configuration
// Rule 1: CaptureSessionAction - Captures cookies and specified headers from responses
// Rule 2: ApplySessionAction - Applies captured session data to subsequent requests
var yamlRules = """
    rules:
      # Capture session data from all responses
      # This stores cookies (Set-Cookie) and specified headers per domain
      - filter:
          typeKind: AnyFilter
        action:
          typeKind: CaptureSessionAction
          captureCookies: true
          captureHeaders:
            - Authorization      # Bearer tokens, Basic auth, etc.
            - X-CSRF-Token       # CSRF protection tokens
            - X-Auth-Token       # Custom auth tokens

      # Apply captured session data only to curl requests
      # This adds stored cookies and headers to requests for matching domains
      - filter:
          typeKind: RequestHeaderFilter
          headerName: User-Agent
          pattern: curl
          operation: Contains
        action:
          typeKind: ApplySessionAction
          applyCookies: true
          applyHeaders: true
          mergeWithExisting: true  # Merge with existing cookies rather than replacing
    """;

// Load rules from YAML content
setting.AddAlterationRules(yamlRules);

Console.WriteLine("YAML Rules Configuration:");
Console.WriteLine(new string('-', 40));
Console.WriteLine(yamlRules);
Console.WriteLine(new string('-', 40));
Console.WriteLine();

Console.WriteLine("Starting proxy with session capture and replay rules...\n");

await using var proxy = new Proxy(setting);
var endpoints = proxy.Run();

var endpoint = endpoints.First();
Console.WriteLine($"Proxy listening on: http://{endpoint.Address}:{endpoint.Port}\n");

Console.WriteLine("=== How to use this example ===\n");

Console.WriteLine("STEP 1: Configure your browser to use the proxy");
Console.WriteLine($"        Proxy address: {endpoint.Address}");
Console.WriteLine($"        Proxy port:    {endpoint.Port}");
Console.WriteLine();

Console.WriteLine("STEP 2: Trust the Fluxzy root certificate (if not already done)");
Console.WriteLine("        Run: fluxzy cert export -f pem");
Console.WriteLine("        Then import the certificate into your browser/OS trust store");
Console.WriteLine();

Console.WriteLine("STEP 3: Navigate to your target website and authenticate");
Console.WriteLine("        Example: Log in to https://example.com");
Console.WriteLine("        The proxy will automatically capture session cookies and headers");
Console.WriteLine();

Console.WriteLine("STEP 4: Make requests via curl through the proxy");
Console.WriteLine("        The captured session data will be automatically applied\n");

Console.WriteLine("Example curl commands (replace with your target URLs):\n");
Console.WriteLine($"  curl -x http://127.0.0.1:{proxyPort} -k https://example.com/api/user");
Console.WriteLine($"  curl -x http://127.0.0.1:{proxyPort} -k https://example.com/api/protected-resource\n");

Console.WriteLine("The -k flag skips certificate verification for simplicity.");
Console.WriteLine("For production use, import the Fluxzy root certificate instead.\n");

Console.WriteLine("=== Session data flow ===\n");
Console.WriteLine("Browser Request  --> [ Proxy ] --> Remote Server");
Console.WriteLine("                       |");
Console.WriteLine("                       v");
Console.WriteLine("               CaptureSessionAction");
Console.WriteLine("               (stores cookies & headers)");
Console.WriteLine("                       |");
Console.WriteLine("                       v");
Console.WriteLine("curl Request     --> [ Proxy ] --> Remote Server");
Console.WriteLine("                       |");
Console.WriteLine("                       v");
Console.WriteLine("               ApplySessionAction");
Console.WriteLine("               (applies stored session data)\n");

Console.WriteLine(new string('-', 60));
Console.WriteLine("Press ENTER to stop the proxy and exit...");
Console.WriteLine(new string('-', 60));
Console.ReadLine();

Console.WriteLine("\nStopping proxy...");
Console.WriteLine("Done!");
