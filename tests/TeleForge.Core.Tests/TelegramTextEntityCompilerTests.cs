using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleForge.Core.Tests;

public class TelegramTextEntityCompilerTests
{
    [Fact]
    public void CompileNullable_HtmlWithCustomEmojiAndBold_ProducesPlainTextAndEntities()
    {
        var result = TelegramTextEntityCompiler.CompileNullable(
            "<b>Premium</b> <tg-emoji emoji-id=\"5395601891781224729\">⭐</tg-emoji>",
            TelegramParseMode.Html);

        Assert.NotNull(result);
        Assert.Equal("Premium ⭐", result!.Text);

        var entities = Assert.IsAssignableFrom<IReadOnlyList<MessageEntity>>(result.Entities!);
        Assert.Equal(2, entities.Count);

        Assert.Collection(entities,
            bold =>
            {
                Assert.Equal(MessageEntityType.Bold, bold.Type);
                Assert.Equal(0, bold.Offset);
                Assert.Equal("Premium".Length, bold.Length);
            },
            emoji =>
            {
                Assert.Equal(MessageEntityType.CustomEmoji, emoji.Type);
                Assert.Equal("Premium ".Length, emoji.Offset);
                Assert.Equal(1, emoji.Length);
                Assert.Equal("5395601891781224729", emoji.CustomEmojiId);
            });
    }

    [Fact]
    public void CompileNullable_NoneWithSelfClosingCustomEmoji_UsesFallbackAttribute()
    {
        var result = TelegramTextEntityCompiler.CompileNullable(
            "Hello <tg-emoji emoji-id=\"42\" fallback=\"⭐\"/>",
            TelegramParseMode.None);

        Assert.NotNull(result);
        Assert.Equal("Hello ⭐", result!.Text);

        var entity = Assert.Single(result.Entities!);
        Assert.Equal(MessageEntityType.CustomEmoji, entity.Type);
        Assert.Equal(6, entity.Offset);
        Assert.Equal(1, entity.Length);
        Assert.Equal("42", entity.CustomEmojiId);
    }
}
