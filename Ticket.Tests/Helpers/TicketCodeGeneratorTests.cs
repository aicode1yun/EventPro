using Ticket.Helpers;

namespace Ticket.Tests.Helpers;

public class TicketCodeGeneratorTests
{
    [Fact]
    public void GenerateCode_ReturnsFormattedCode()
    {
        var code = TicketCodeGenerator.GenerateCode();
        Assert.StartsWith("TKT-", code);
        Assert.Equal(10, code.Length);
    }

    [Fact]
    public void GenerateCode_ContainsOnlyValidChars()
    {
        var code = TicketCodeGenerator.GenerateCode();
        var suffix = code[4..];
        Assert.Matches("^[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$", suffix);
    }

    [Fact]
    public void GenerateCode_ProducesUniqueCodes()
    {
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var code = TicketCodeGenerator.GenerateCode();
            Assert.True(codes.Add(code), $"Duplicate code generated: {code}");
        }
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = TicketCodeGenerator.GenerateToken();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_Returns32CharHex()
    {
        var token = TicketCodeGenerator.GenerateToken();
        Assert.Equal(32, token.Length);
        Assert.Matches("^[0-9a-f]{32}$", token);
    }

    [Fact]
    public void GenerateToken_ProducesUniqueTokens()
    {
        var tokens = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var token = TicketCodeGenerator.GenerateToken();
            Assert.True(tokens.Add(token), $"Duplicate token generated: {token}");
        }
    }
}
