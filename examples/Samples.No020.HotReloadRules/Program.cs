// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;

// This example demonstrates how to hot reload proxy rules at runtime without stopping the proxy.
// Hot reload allows you to dynamically update traffic manipulation rules while the proxy is running.

Console.WriteLine("=== Fluxzy Hot Reload Example ===\n");

// Create proxy with initial rules
var setting = FluxzySetting.CreateDefault(IPAddress.Loopback, 44344);

// Add initial rule: Add a response header
setting.AddAlterationRules(
    new AddResponseHeaderAction("X-Rule-Version", "v1-initial"),
    AnyFilter.Default
);

Console.WriteLine("Starting proxy with initial rules...");
await using var proxy = new Proxy(setting);
var endpoints = proxy.Run();

Console.WriteLine($"Proxy listening on: {endpoints.First().Address}:{endpoints.First().Port}");
Console.WriteLine("Initial rules:");
Console.WriteLine("  - Add response header: X-Rule-Version = v1-initial\n");

// Simulate some time passing with the initial rules
Console.WriteLine("Press ENTER to update rules to version 2...");
Console.ReadLine();

// HOT RELOAD #1: Update rules using configuration action (fluent API)
Console.WriteLine("\n[Hot Reload] Updating rules to version 2...");
proxy.UpdateRules(s => {
    s.AddAlterationRules(
        new AddResponseHeaderAction("X-Rule-Version", "v2-updated"),
        AnyFilter.Default
    );
    s.AddAlterationRules(
        new AddResponseHeaderAction("X-Update-Timestamp", DateTime.UtcNow.ToString("O")),
        AnyFilter.Default
    );
});

Console.WriteLine("Rules updated! New rules:");
Console.WriteLine("  - Add response header: X-Rule-Version = v2-updated");
Console.WriteLine("  - Add response header: X-Update-Timestamp = (current time)\n");

// Get and display active rules
var activeRules = proxy.GetActiveRules();
Console.WriteLine($"Active rule count: {activeRules.Count}");

Console.WriteLine("\nPress ENTER to update rules to version 3...");
Console.ReadLine();

// HOT RELOAD #2: Update rules using direct rule list
Console.WriteLine("\n[Hot Reload] Updating rules to version 3...");
var newRules = new List<Fluxzy.Rules.Rule>
{
    new Fluxzy.Rules.Rule(
        new AddResponseHeaderAction("X-Rule-Version", "v3-final"),
        AnyFilter.Default
    ),
    new Fluxzy.Rules.Rule(
        new AddRequestHeaderAction("X-Request-Modified", "true"),
        AnyFilter.Default
    )
};

proxy.UpdateRules(newRules);

Console.WriteLine("Rules updated! New rules:");
Console.WriteLine("  - Add response header: X-Rule-Version = v3-final");
Console.WriteLine("  - Add request header: X-Request-Modified = true\n");

activeRules = proxy.GetActiveRules();
Console.WriteLine($"Active rule count: {activeRules.Count}");

Console.WriteLine("\nPress ENTER to clear all alteration rules...");
Console.ReadLine();

// HOT RELOAD #3: Clear all alteration rules (keeping only fixed rules)
Console.WriteLine("\n[Hot Reload] Clearing all alteration rules...");
proxy.UpdateRules(new List<Fluxzy.Rules.Rule>());

Console.WriteLine("All alteration rules cleared!");
activeRules = proxy.GetActiveRules();
Console.WriteLine($"Active rule count: {activeRules.Count} (only fixed rules remain)\n");

Console.WriteLine("Press ENTER to stop the proxy and exit...");
Console.ReadLine();

Console.WriteLine("\nStopping proxy...");
// Proxy will be disposed automatically (await using)

Console.WriteLine("Done!");

/*
 * HOT RELOAD CAPABILITIES:
 *
 * 1. UpdateRules(IEnumerable<Rule> rules)
 *    - Direct rule list replacement
 *    - Useful when rules are generated programmatically
 *
 * 2. UpdateRules(Action<FluxzySetting> configureRules)
 *    - Fluent configuration API
 *    - Familiar syntax for Fluxzy users
 *
 * 3. GetActiveRules()
 *    - Retrieve current alteration rules
 *    - Excludes fixed rules (SSL skip, CA mount, etc.)
 *
 * THREAD SAFETY:
 * - Hot reload is fully thread-safe
 * - In-flight exchanges complete with their original rules
 * - New exchanges use the updated rules
 * - No proxy downtime during rule updates
 *
 * USE CASES:
 * - A/B testing different traffic manipulation strategies
 * - Dynamic rule adjustment based on monitoring
 * - Debugging and troubleshooting without proxy restart
 * - Gradual rollout of new interception logic
 */
