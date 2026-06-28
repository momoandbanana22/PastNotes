namespace PastNotes.Tests;

public class DotEnvLoaderTests
{
    // TDD: FEAT-4 - .envファイル読み込み
    [Fact]
    [Trait("Category", "Unit")]
    public void Load_WhenFileHasKeyValuePairs_SetsEnvironmentVariables()
    {
        // Arrange
        var filePath = $"test_{Guid.NewGuid()}.env";
        File.WriteAllText(filePath, "DOTENV_TEST_FOO=hello\nDOTENV_TEST_BAR=world\n");

        try
        {
            // Act
            DotEnvLoader.Load(filePath);

            // Assert
            Assert.Equal("hello", Environment.GetEnvironmentVariable("DOTENV_TEST_FOO"));
            Assert.Equal("world", Environment.GetEnvironmentVariable("DOTENV_TEST_BAR"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTENV_TEST_FOO", null);
            Environment.SetEnvironmentVariable("DOTENV_TEST_BAR", null);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_WhenFileHasComments_IgnoresCommentLines()
    {
        // Arrange
        var filePath = $"test_{Guid.NewGuid()}.env";
        File.WriteAllText(filePath, "# This is a comment\nDOTENV_TEST_VAL=ok\n");

        try
        {
            DotEnvLoader.Load(filePath);
            Assert.Equal("ok", Environment.GetEnvironmentVariable("DOTENV_TEST_VAL"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTENV_TEST_VAL", null);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_WhenFileDoesNotExist_DoesNotThrow()
    {
        // Act & Assert: ファイルがなくても例外を投げないこと
        var exception = Record.Exception(() => DotEnvLoader.Load("non_existent_file.env"));
        Assert.Null(exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_WhenEnvVarAlreadySet_DoesNotOverwrite()
    {
        // 既存の環境変数は上書きしない（CLIや実環境の設定を優先）
        var filePath = $"test_{Guid.NewGuid()}.env";
        File.WriteAllText(filePath, "DOTENV_TEST_EXISTING=from_file\n");
        Environment.SetEnvironmentVariable("DOTENV_TEST_EXISTING", "from_env");

        try
        {
            DotEnvLoader.Load(filePath);
            Assert.Equal("from_env", Environment.GetEnvironmentVariable("DOTENV_TEST_EXISTING"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTENV_TEST_EXISTING", null);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
