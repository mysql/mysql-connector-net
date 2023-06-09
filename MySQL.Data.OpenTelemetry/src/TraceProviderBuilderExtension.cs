using System;
using OpenTelemetry.Trace;

/// <summary>
/// Extension method for setting up Connector/Net OpenTelemetry tracing.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddConnectorNet(
        this TracerProviderBuilder builder)
        => builder.AddSource("connector-net");
}