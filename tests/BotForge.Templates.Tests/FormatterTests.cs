using System.Globalization;

namespace BotForge.Templates.Tests;

public class FormatterTests
{
    private static FormatterPipeline CreatePipeline()
    {
        return new FormatterPipeline(Array.Empty<ITemplateFormatter>());
    }

    [Fact]
    public void Process_NumberFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Amount"] = "1234567" };

        var result = pipeline.Process("{Amount | number}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("1,234,567", result);
    }

    [Fact]
    public void Process_PercentFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Ratio"] = "0.75" };

        var result = pipeline.Process("{Ratio | percent}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("75.0 %", result);
    }

    [Fact]
    public void Process_TruncateFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Text"] = "Hello World, this is a long text" };

        var result = pipeline.Process("{Text | truncate:10}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("Hello W...", result);
    }

    [Fact]
    public void Process_UpperFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Name"] = "alice" };

        var result = pipeline.Process("{Name | upper}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("ALICE", result);
    }

    [Fact]
    public void Process_LowerFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Name"] = "ALICE" };

        var result = pipeline.Process("{Name | lower}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("alice", result);
    }

    [Fact]
    public void Process_DateFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Date"] = "2024-03-15T10:30:00Z" };

        var result = pipeline.Process("{Date | date}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("15.03.2024", result);
    }

    [Fact]
    public void Process_DateFormatter_CustomFormat()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Date"] = "2024-03-15T10:30:00Z" };

        var result = pipeline.Process("{Date | date:yyyy-MM-dd}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("2024-03-15", result);
    }

    [Fact]
    public void Process_ProgressBarFormatter()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Progress"] = "0.5" };

        var result = pipeline.Process("{Progress | progressbar}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("[█████░░░░░]", result);
    }

    [Fact]
    public void Process_ProgressBarFormatter_CustomWidth()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Progress"] = "0.5" };

        var result = pipeline.Process("{Progress | progressbar:4}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("[██░░]", result);
    }

    [Fact]
    public void Process_NoFormatterTokens_ReturnsSameText()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["X"] = "hello" };

        var result = pipeline.Process("No formatters here", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("No formatters here", result);
    }

    [Fact]
    public void Process_MissingParameter_KeepsOriginal()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string>();

        var result = pipeline.Process("{Missing | upper}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("{Missing | upper}", result);
    }

    [Fact]
    public void Process_UnknownFormatter_KeepsOriginal()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["X"] = "val" };

        var result = pipeline.Process("{X | nonexistent}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("{X | nonexistent}", result);
    }

    [Fact]
    public void Process_DurationFormatter_FromTimeSpan()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Time"] = "01:01:01" };

        var result = pipeline.Process("{Time | duration}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("1ч 1м", result);
    }

    [Fact]
    public void Process_PluralFormatter_SingleForm()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Count"] = "1" };

        var result = pipeline.Process("{Count | plural:товар,товара,товаров}", parameters,
            CultureInfo.InvariantCulture);

        Assert.Equal("1 товар", result);
    }

    [Fact]
    public void Process_PluralFormatter_FewForm()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Count"] = "3" };

        var result = pipeline.Process("{Count | plural:товар,товара,товаров}", parameters,
            CultureInfo.InvariantCulture);

        Assert.Equal("3 товара", result);
    }

    [Fact]
    public void Process_PluralFormatter_ManyForm()
    {
        var pipeline = CreatePipeline();
        var parameters = new Dictionary<string, string> { ["Count"] = "5" };

        var result = pipeline.Process("{Count | plural:товар,товара,товаров}", parameters,
            CultureInfo.InvariantCulture);

        Assert.Equal("5 товаров", result);
    }

    [Fact]
    public void CustomFormatter_IsUsed()
    {
        var custom = new CustomTestFormatter();
        var pipeline = new FormatterPipeline([custom]);
        var parameters = new Dictionary<string, string> { ["X"] = "hello" };

        var result = pipeline.Process("{X | reverse}", parameters, CultureInfo.InvariantCulture);

        Assert.Equal("olleh", result);
    }

    private sealed class CustomTestFormatter : ITemplateFormatter
    {
        public string Name => "reverse";

        public string Format(string value, string? args, CultureInfo culture)
        {
            return new string(value.Reverse().ToArray());
        }
    }
}
