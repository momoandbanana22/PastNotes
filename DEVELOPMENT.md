# 開発者向けドキュメント

## コンソールアプリケーション

### ビルドと実行

```bash
# ビルド
dotnet build PastNotes.Console/PastNotes.Console.csproj

# 実行
.\PastNotes.Console\bin\Debug\net10.0\PastNotes.Console.exe fetch --days 30
.\PastNotes.Console\bin\Debug\net10.0\PastNotes.Console.exe fetch --start 2024-01-01 --end 2024-01-31
.\PastNotes.Console\bin\Debug\net10.0\PastNotes.Console.exe search <keyword>
.\PastNotes.Console\bin\Debug\net10.0\PastNotes.Console.exe view
```

### テスト

コンソールアプリケーションのテスト:

```bash
dotnet test PastNotes.Console.Tests/PastNotes.Console.Tests.csproj
```

## TDD開発ルール

このプロジェクトではテスト駆動開発（TDD）を採用しています。以下のルールに従って開発を進めてください。

### 基本原則

1. **テストファースト**: 実装コードを書く前に、必ず失敗するテストを先に書く
2. **小さなステップ**: 一度に小さな機能単位でテストと実装を繰り返す
3. **リファクタリング**: テストが通った後、コードの品質を改善する

### テストのガイドライン

- **命名規則**: テストメソッドは `MethodName_ExpectedBehavior_WhenCondition` の形式で命名
- **構造**: 各テストは Arrange（準備）→ Act（実行）→ Assert（検証）の構造に従う
- **独立性**: 各テストは他のテストに依存せず、単独で実行可能
- **可読性**: テストコードはドキュメントとして機能するよう明確に記述

### テストカバレッジ

- 目標カバレッジ: 80%以上
- 重要なビジネスロジック: 100%カバレッジを目指す
- カバレッジレポート: `dotnet test` 実行時に自動生成

### 開発フロー

1. 新しい機能の要件を理解する
2. 失敗するテストを書く
3. テストを実行して失敗を確認する
4. 最小限のコードを書いてテストを通す
5. テストを実行して成功を確認する
6. コードをリファクタリングする
7. テストを実行してリファクタリングによる破壊がないことを確認する
8. ステップ2-7を繰り返す

### ツール

- **テストフレームワーク**: xUnit
- **モックライブラリ**: Moq
- **カバレッジツール**: Coverlet

## API制限事項

Misskey APIの`/api/users/notes`エンドポイントには既知のバグがあります：

- **sinceDateパラメータ**: 何を入れてもそのユーザーの最初からlimit番目までのノートが返ってくる
- **untilDateパラメータ**: 0以外を入れると空の配列が返ってくる

詳細: [GitHub Issue #10679](https://github.com/misskey-dev/misskey/issues/10679)

これらのパラメータはバグがあるため使用せず、以下のアプローチを採用しています：

1. `untilId`パラメータを使用してページネーション
2. すべてのノートを取得した後、クライアント側で日付フィルタリング

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
