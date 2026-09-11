namespace TeleForge.Templates.Tests;

public class LoopProcessorTests
{
    private readonly LoopProcessor _processor = new();

    [Fact]
    public void Process_NullLoopData_ReturnsSameText()
    {
        var result = _processor.Process("Hello world", null);
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void Process_EmptyLoopData_ReturnsSameText()
    {
        var result = _processor.Process("Hello world",
            new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>());
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void Process_SimpleLoop_ExpandsItems()
    {
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["items"] = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { ["Name"] = "Apple" },
                new Dictionary<string, string> { ["Name"] = "Banana" },
                new Dictionary<string, string> { ["Name"] = "Cherry" }
            }
        };

        var result = _processor.Process("{#each items}{Name}\n{/each}", loopData);

        Assert.Contains("Apple", result);
        Assert.Contains("Banana", result);
        Assert.Contains("Cherry", result);
    }

    [Fact]
    public void Process_IndexVariable_IsOneBased()
    {
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["items"] = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { ["X"] = "a" },
                new Dictionary<string, string> { ["X"] = "b" }
            }
        };

        var result = _processor.Process("{#each items}{Index}. {X}\n{/each}", loopData);

        Assert.Contains("1. a", result);
        Assert.Contains("2. b", result);
    }

    [Fact]
    public void Process_Index0Variable_IsZeroBased()
    {
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["items"] = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { ["X"] = "first" }
            }
        };

        var result = _processor.Process("{#each items}[{Index0}]{X}{/each}", loopData);

        Assert.Contains("[0]first", result);
    }

    [Fact]
    public void Process_CountVariable()
    {
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["items"] = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { ["X"] = "a" },
                new Dictionary<string, string> { ["X"] = "b" },
                new Dictionary<string, string> { ["X"] = "c" }
            }
        };

        var result = _processor.Process("{#each items}Count:{Count}{/each}", loopData);

        Assert.Contains("Count:3", result);
    }

    [Fact]
    public void Process_IsFirstIsLast()
    {
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["items"] = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { ["V"] = "1" },
                new Dictionary<string, string> { ["V"] = "2" }
            }
        };

        var result = _processor.Process("{#each items}first={IsFirst} last={IsLast} {/each}", loopData);

        Assert.Contains("first=true last=false", result);
        Assert.Contains("first=false last=true", result);
    }

    [Fact]
    public void Process_MissingCollection_LeavesTextUnchanged()
    {
        // Empty loopData causes early return without processing
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>();

        var result = _processor.Process("before{#each missing}item{/each}after", loopData);

        Assert.Equal("before{#each missing}item{/each}after", result);
    }

    [Fact]
    public void Process_MissingCollectionAmongOthers_RemovesBlock()
    {
        var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["existing"] = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { ["X"] = "val" }
            }
        };

        var result = _processor.Process("before{#each missing}item{/each}after", loopData);

        Assert.Equal("beforeafter", result);
    }

    [Fact]
    public void Process_NoEachBlocks_ReturnsSameText()
    {
        var result = _processor.Process("No loops here",
            new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
            {
                ["items"] = new List<IDictionary<string, string>>()
            });

        Assert.Equal("No loops here", result);
    }
}
