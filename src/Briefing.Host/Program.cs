using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Briefing.Host.Checker;
using Briefing.Host.Config;
using Briefing.Host.Model;

namespace Briefing.Host;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var config = GetCheckerConfig(args);
        var checker = new TelemetryChecker(config);
        var report = await checker.GetReportAsync(from: DateTime.Now.AddDays(-1), to: DateTime.Now);

        Console.WriteLine(report);
    }

    private static CheckerConfiguration GetCheckerConfig(string[] args)
    {
        var workspaceId = Guid.Parse(args[0]);
        var environments = new List<CheckerEnvironmentConfiguration>();

        for (var a = 1; a < args.Length; a++)
        {
            var parts = args[a].Split('=');
            
            environments.Add(new CheckerEnvironmentConfiguration
            {
                Name = parts[0],
                CollectorId = Guid.Parse(parts[1])
            });
        }

        var config = new CheckerConfiguration
        {
            WorkspaceId = workspaceId,
            TopExceptionCount = 10,
            CheckedResponseCodes = [500, 400, 429, 503],
            Environments = environments
        };

        return config;
    }
}