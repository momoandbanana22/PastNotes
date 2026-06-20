# 開発者向けドキュメント

## テスト実行方法

### テストカテゴリ

このプロジェクトでは、xUnitのTraitを使用してテストをカテゴリ分けしています:

- **Unit**: ユニットテスト（モックを使用、環境変数不要）
- **Integration**: 統合テスト（実際のAPIを使用、環境変数必要）

### ユニットテストのみ実行

```bash
dotnet test --filter "Category=Unit"
```

### 統合テストのみ実行

統合テストは実際のMisskey.io APIを使用するため、環境変数の設定が必要です。

#### 方法1: 環境変数を設定して実行

```powershell
# 環境変数を設定
$env:MISSKEY_INSTANCE_URL = "https://misskey.io"
$env:MISSKEY_API_TOKEN = "your-api-token"

# 統合テストのみ実行
dotnet test --filter "Category=Integration"
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

### すべてのテストを実行

```bash
dotnet test
```

環境変数が設定されていない場合、統合テストは環境変数不足で失敗します。

### カバレッジレポート

カバレッジレポートを生成するには:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

レポートは `coverage/coverage.cobertura.xml` に生成されます。
