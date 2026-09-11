namespace TeleForge.Templates.Tests;

public class ConditionalEvaluatorTests
{
    private readonly ConditionalEvaluator _evaluator = new();

    [Fact]
    public void EvaluateCondition_TruthyValue_ReturnsTrue()
    {
        var parameters = new Dictionary<string, string> { ["IsAdmin"] = "true" };
        Assert.True(_evaluator.EvaluateCondition("IsAdmin", parameters));
    }

    [Fact]
    public void EvaluateCondition_FalsyValue_ReturnsFalse()
    {
        var parameters = new Dictionary<string, string> { ["IsAdmin"] = "false" };
        Assert.False(_evaluator.EvaluateCondition("IsAdmin", parameters));
    }

    [Fact]
    public void EvaluateCondition_EmptyValue_ReturnsFalse()
    {
        var parameters = new Dictionary<string, string> { ["Name"] = "" };
        Assert.False(_evaluator.EvaluateCondition("Name", parameters));
    }

    [Fact]
    public void EvaluateCondition_ZeroValue_ReturnsFalse()
    {
        var parameters = new Dictionary<string, string> { ["Count"] = "0" };
        Assert.False(_evaluator.EvaluateCondition("Count", parameters));
    }

    [Fact]
    public void EvaluateCondition_MissingKey_ReturnsFalse()
    {
        var parameters = new Dictionary<string, string>();
        Assert.False(_evaluator.EvaluateCondition("Missing", parameters));
    }

    [Fact]
    public void EvaluateCondition_Not_TruthyBecomeFalse()
    {
        var parameters = new Dictionary<string, string> { ["Active"] = "true" };
        Assert.False(_evaluator.EvaluateCondition("!Active", parameters));
    }

    [Fact]
    public void EvaluateCondition_Not_FalsyBecomeTrue()
    {
        var parameters = new Dictionary<string, string> { ["Active"] = "false" };
        Assert.True(_evaluator.EvaluateCondition("!Active", parameters));
    }

    [Fact]
    public void EvaluateCondition_Exists_Present()
    {
        var parameters = new Dictionary<string, string> { ["Email"] = "test@test.com" };
        Assert.True(_evaluator.EvaluateCondition("Email exists", parameters));
    }

    [Fact]
    public void EvaluateCondition_Exists_Missing()
    {
        var parameters = new Dictionary<string, string>();
        Assert.False(_evaluator.EvaluateCondition("Email exists", parameters));
    }

    [Fact]
    public void EvaluateCondition_NotExists()
    {
        var parameters = new Dictionary<string, string>();
        Assert.True(_evaluator.EvaluateCondition("Email !exists", parameters));
    }

    [Fact]
    public void EvaluateCondition_Equality()
    {
        var parameters = new Dictionary<string, string> { ["Role"] = "admin" };
        Assert.True(_evaluator.EvaluateCondition("Role == admin", parameters));
        Assert.True(_evaluator.EvaluateCondition("Role = admin", parameters));
    }

    [Fact]
    public void EvaluateCondition_Inequality()
    {
        var parameters = new Dictionary<string, string> { ["Role"] = "user" };
        Assert.True(_evaluator.EvaluateCondition("Role != admin", parameters));
    }

    [Fact]
    public void EvaluateCondition_NumericGreaterThan()
    {
        var parameters = new Dictionary<string, string> { ["Score"] = "100" };
        Assert.True(_evaluator.EvaluateCondition("Score > 50", parameters));
        Assert.False(_evaluator.EvaluateCondition("Score > 200", parameters));
    }

    [Fact]
    public void EvaluateCondition_NumericLessThan()
    {
        var parameters = new Dictionary<string, string> { ["Score"] = "30" };
        Assert.True(_evaluator.EvaluateCondition("Score < 50", parameters));
    }

    [Fact]
    public void EvaluateCondition_In()
    {
        var parameters = new Dictionary<string, string> { ["Lang"] = "en" };
        Assert.True(_evaluator.EvaluateCondition("Lang in en,ru,uk", parameters));
        Assert.False(_evaluator.EvaluateCondition("Lang in ru,uk", parameters));
    }

    [Fact]
    public void EvaluateCondition_Contains()
    {
        var parameters = new Dictionary<string, string> { ["Name"] = "Hello World" };
        Assert.True(_evaluator.EvaluateCondition("Name contains World", parameters));
    }

    [Fact]
    public void EvaluateCondition_And()
    {
        var parameters = new Dictionary<string, string> { ["A"] = "true", ["B"] = "true" };
        Assert.True(_evaluator.EvaluateCondition("A && B", parameters));

        parameters["B"] = "false";
        Assert.False(_evaluator.EvaluateCondition("A && B", parameters));
    }

    [Fact]
    public void EvaluateCondition_Or()
    {
        var parameters = new Dictionary<string, string> { ["A"] = "false", ["B"] = "true" };
        Assert.True(_evaluator.EvaluateCondition("A || B", parameters));

        parameters["B"] = "false";
        Assert.False(_evaluator.EvaluateCondition("A || B", parameters));
    }

    [Fact]
    public void Process_IfTrue_ShowsTrueBlock()
    {
        var parameters = new Dictionary<string, string> { ["IsAdmin"] = "true" };
        var result = _evaluator.Process("{#if IsAdmin}Admin panel{/if}", parameters);

        Assert.Equal("Admin panel", result);
    }

    [Fact]
    public void Process_IfFalse_HidesBlock()
    {
        var parameters = new Dictionary<string, string> { ["IsAdmin"] = "false" };
        var result = _evaluator.Process("{#if IsAdmin}Admin panel{/if}", parameters);

        Assert.Equal("", result);
    }

    [Fact]
    public void Process_IfElse_ShowsCorrectBlock()
    {
        var parameters = new Dictionary<string, string> { ["IsAdmin"] = "false" };
        var result = _evaluator.Process("{#if IsAdmin}Admin{#else}User{/if}", parameters);

        Assert.Equal("User", result);
    }

    [Fact]
    public void Process_EmptyText_ReturnsEmpty()
    {
        var result = _evaluator.Process("", new Dictionary<string, string>());
        Assert.Equal("", result);
    }

    [Fact]
    public void Process_NullText_ReturnsNull()
    {
        var result = _evaluator.Process(null!, new Dictionary<string, string>());
        Assert.Null(result);
    }
}
