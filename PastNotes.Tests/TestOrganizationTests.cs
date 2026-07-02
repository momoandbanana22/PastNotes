namespace PastNotes.Tests;

// TDD: TST-23 - 各テストクラスが正しい名前空間に属しているかを検証
public class TestOrganizationTests
{
    private static System.Type? GetTestType(string name) =>
        System.Reflection.Assembly.GetExecutingAssembly()
            .GetTypes()
            .FirstOrDefault(t => t.Name == name);

    [Fact]
    [Trait("Category", "Unit")]
    public void TimeZoneHelperTests_ShouldBeInPastNotesTestsNamespace()
    {
        var type = GetTestType("TimeZoneHelperTests");
        Assert.NotNull(type);
        Assert.Equal("PastNotes.Tests", type.Namespace);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NoteHtmlGeneratorTests_ShouldBeInPastNotesTestsNamespace()
    {
        var type = GetTestType("NoteHtmlGeneratorTests");
        Assert.NotNull(type);
        Assert.Equal("PastNotes.Tests", type.Namespace);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NoteHtmlGeneratorOutputTests_ShouldBeInPastNotesTestsNamespace()
    {
        var type = GetTestType("NoteHtmlGeneratorOutputTests");
        Assert.NotNull(type);
        Assert.Equal("PastNotes.Tests", type.Namespace);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NoteRepositoryTests_ShouldBeInPastNotesTestsNamespace()
    {
        var type = GetTestType("NoteRepositoryTests");
        Assert.NotNull(type);
        Assert.Equal("PastNotes.Tests", type.Namespace);
    }

    // TDD: TST-39 - MisskeyApiClientTests.cs がTST-23の横展開漏れでnamespace PastNotesのままだった
    [Fact]
    [Trait("Category", "Unit")]
    public void MisskeyApiClientTests_ShouldBeInPastNotesTestsNamespace()
    {
        var type = GetTestType("MisskeyApiClientTests");
        Assert.NotNull(type);
        Assert.Equal("PastNotes.Tests", type.Namespace);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MockHttpMessageHandler_ShouldBeInPastNotesTestsNamespace()
    {
        var type = GetTestType("MockHttpMessageHandler");
        Assert.NotNull(type);
        Assert.Equal("PastNotes.Tests", type.Namespace);
    }
}
