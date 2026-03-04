using Briefing.Host.Model;
using Xunit;
using System.Collections.Generic;
using Shouldly;

namespace Briefing.UnitTests;

public class ReportTests
{
    [Fact]
    public void ResponseCodesSummary_ToString_FormatsCorrectly()
    {
        var codes = new Dictionary<int, int>
        {
            [200] = 10,
            [404] = 5
        };
        var summary = new ResponseCodesSummary { Codes = codes };

        var result = summary.ToString();

        result.ShouldContain("HTTP 200 - 10x");
        result.ShouldContain("HTTP 404 - 5x");
    }

    [Fact]
    public void EnvironmentReport_ToString_IncludesDetailsForListedCodes()
    {
        var report = new EnvironmentReport
        {
            Name = "TestEnv",
            ExceptionCount = 5,
            ResponsesSummary = new ResponseCodesSummary { Codes = new Dictionary<int, int> { [500] = 1 } },
            TopExceptions = [],
            TopFailedRequests = [
                new FailedRequest { ResponseCode = 500, App = "TestApp", Url = "http://test.com", Count = 1 },
                new FailedRequest { ResponseCode = 404, App = "TestApp", Url = "http://test.com/404", Count = 2 }
            ],
            ListedStatusCodesDetails = [500]
        };

        var result = report.ToString();

        result.ShouldContain("500 - 1x - TestApp - http://test.com");
        result.ShouldNotContain("404 - 2x - TestApp - http://test.com/404");
    }
}
