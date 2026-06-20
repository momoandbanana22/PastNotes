# 開発者向けドキュメント

## テスト実行方法

### ユニットテスト

ユニットテストは環境変数なしで実行可能です:

```bash
dotnet test
```

### 統合テスト

統合テストは実際のMisskey.io APIを使用するため、環境変数の設定が必要です。

#### 方法1: 環境変数を設定して実行

```powershell
# 環境変数を設定
$env:MISSKEY_INSTANCE_URL = "https://misskey.io"
$env:MISSKEY_API_TOKEN = "your-api-token"

# テスト実行
dotnet test
```

#### 方法2: PowerShellスクリプトを使用

```powershell
.\run-integration-tests.ps1 -InstanceUrl "https://misskey.io" -ApiToken "your-api-token"
```

**注意:**
- PowerShellスクリプトの実行が無効になっている場合、以下のコマンドで有効にできます:
  ```powershell
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
  ```
- APIトークンはMisskey.ioの設定から取得してください
- 統合テストを実行しない場合、4つの統合テストが環境変数不足で失敗します（これは正常な動作です）

### カバレッジレポート

カバレッジレポートを生成するには:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

レポートは `coverage/coverage.cobertura.xml` に生成されます。
