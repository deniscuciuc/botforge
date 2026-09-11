using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Invoice;

namespace TeleForge.Payments.Tests;

/// <summary>
/// Invoice payloads are the only thing Telegram hands back at pre-checkout time, so every
/// downstream decision — which validator runs, what gets fulfilled, what gets refunded —
/// is keyed off parsing them correctly.
/// </summary>
public sealed class PayloadSerializerTests
{
    [Theory]
    [InlineData("SUB:premium:30", "SUB")]
    [InlineData("GIFT", "GIFT")]
    [InlineData("ORDER:", "ORDER")]
    [InlineData("a:b:c:d:e", "a")]
    public void GetPrefix_TakesEverythingBeforeTheFirstColon(string raw, string expected)
    {
        Assert.Equal(expected, PayloadSerializer.GetPrefix(raw));
    }

    [Fact]
    public void GetFields_SplitsEverythingAfterThePrefix()
    {
        Assert.Equal(["premium", "30"], PayloadSerializer.GetFields("SUB:premium:30"));
    }

    [Fact]
    public void GetFields_ReturnsEmpty_WhenThereIsNoColon()
    {
        Assert.Empty(PayloadSerializer.GetFields("GIFT"));
    }

    [Fact]
    public void GetFields_ReturnsEmpty_WhenTheColonIsTrailing()
    {
        Assert.Empty(PayloadSerializer.GetFields("ORDER:"));
    }

    [Fact]
    public void GetFields_PreservesEmptySegments()
    {
        // A missing middle field must not silently shift the ones after it.
        Assert.Equal(["a", "", "c"], PayloadSerializer.GetFields("P:a::c"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPrefixAndGetFields_RejectBlankInput(string? raw)
    {
        Assert.ThrowsAny<ArgumentException>(() => PayloadSerializer.GetPrefix(raw!));
        Assert.ThrowsAny<ArgumentException>(() => PayloadSerializer.GetFields(raw!));
    }

    [Fact]
    public void Deserialize_HandsTheFieldsToTheFactory()
    {
        string[]? seen = null;
        var result = PayloadSerializer.Deserialize("SUB:premium:30", fields =>
        {
            seen = fields;
            return new StubPayload();
        });

        Assert.NotNull(result);
        Assert.NotNull(seen);
        Assert.Equal(["premium", "30"], seen);
    }

    [Fact]
    public void Deserialize_RejectsANullFactory()
    {
        Assert.Throws<ArgumentNullException>(
            () => PayloadSerializer.Deserialize<StubPayload>("SUB:x", null!));
    }

    [Fact]
    public void Serialize_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => PayloadSerializer.Serialize(null!));
    }

    [Fact]
    public void SerializeThenParse_RoundTrips()
    {
        var raw = PayloadSerializer.Serialize(new StubPayload());

        Assert.Equal("STUB:premium:30", raw);
        Assert.Equal("STUB", PayloadSerializer.GetPrefix(raw));
        Assert.Equal(["premium", "30"], PayloadSerializer.GetFields(raw));
    }

    [Fact]
    public void APrefixOnlyPayload_SerializesWithoutATrailingColon()
    {
        var raw = PayloadSerializer.Serialize(new PrefixOnlyPayload());

        Assert.Equal("GIFT", raw);
        Assert.Equal("GIFT", PayloadSerializer.GetPrefix(raw));
        Assert.Empty(PayloadSerializer.GetFields(raw));
    }

    [Fact]
    public void Serialize_StaysWithinTelegramsPayloadLimit_ForATypicalPayload()
    {
        // Telegram rejects an invoice payload longer than 128 bytes, and the failure only
        // shows up at send time.
        var raw = PayloadSerializer.Serialize(new StubPayload());

        Assert.True(System.Text.Encoding.UTF8.GetByteCount(raw) <= 128);
    }

    private sealed record StubPayload : TypedPayload
    {
        public override string Prefix => "STUB";

        public override string[] SerializeFields() => ["premium", "30"];
    }

    private sealed record PrefixOnlyPayload : TypedPayload
    {
        public override string Prefix => "GIFT";
    }
}
