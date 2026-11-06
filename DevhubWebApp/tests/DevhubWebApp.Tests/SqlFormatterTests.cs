using DevhubWebApp.Utilities;

namespace DevhubWebApp.Tests;

public sealed class SqlFormatterTests
{
    [Theory]
    [InlineData("select id,name from users where active=1", "SELECT", "FROM", "WHERE")]
    [InlineData("insert into users(id,name) values(1,'Ada')", "INSERT", "INTO", "VALUES")]
    [InlineData("update users set name='Ada' where id=1", "UPDATE", "SET", "WHERE")]
    [InlineData("delete from users where id=1", "DELETE", "FROM", "WHERE")]
    [InlineData("select u.id from users u left join teams t on t.id=u.team_id", "SELECT", "LEFT JOIN", "ON")]
    [InlineData("select team,count(*) from users group by team having count(*)>1", "GROUP BY", "HAVING", "COUNT")]
    public void Format_HandlesCommonStatements(string sql, params string[] expected)
    {
        var result = SqlFormatter.Format(sql);
        Assert.All(expected, value => Assert.Contains(value, result, StringComparison.Ordinal));
    }

    [Fact]
    public void Format_PreservesStringsAndCommentsExactly()
    {
        const string input = "select 'from where O''Brien' as text /* select FROM */ from users -- don't change SELECT";
        var result = SqlFormatter.Format(input);
        Assert.Contains("'from where O''Brien'", result, StringComparison.Ordinal);
        Assert.Contains("/* select FROM */", result, StringComparison.Ordinal);
        Assert.Contains("-- don't change SELECT", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_IndentsNestedQueryAndSupportsMultilineInput()
    {
        var result = SqlFormatter.Format("select *\nfrom (select id from users where active=1) u");
        Assert.Contains("    SELECT", result, StringComparison.Ordinal);
        Assert.Contains(Environment.NewLine, result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("select * from users where id in (select id from admins", "closing parentheses")]
    [InlineData("select 'broken", "Unterminated")]
    [InlineData("select * from users /* broken", "Unterminated")]
    [InlineData("this is not sql", "supported SQL statement")]
    public void Validate_ReportsStructuralErrors(string sql, string expected)
        => Assert.Contains(expected, SqlFormatter.Validate(sql), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(SqlFormatter.Format("  "));
        Assert.Empty(SqlFormatter.Validate(string.Empty));
    }

    [Fact]
    public void Format_DoesNotInsertSpaceIntoQualifiedName()
        => Assert.Contains("users.id", SqlFormatter.Format("select users.id from users"), StringComparison.Ordinal);
}
