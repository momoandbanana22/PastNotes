# 開発者向けドキュメント

## コンソールアプリケーション

### 認証情報の設定

開発時の認証情報はプロジェクトルートに `.env` ファイルを作成するのが便利です（`.gitignore` で除外済み）。

```
MISSKEY_INSTANCE_URL=https://misskey.io
MISSKEY_API_TOKEN=your-api-token
```

優先順位: CLI引数 `--token` > 環境変数 `MISSKEY_API_TOKEN` > `.env` ファイル

### ビルドと実行

```bash
# ビルド
dotnet build PastNotes.Console/PastNotes.Console.csproj

# 実行（dotnet run 経由）
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --start 2024-01-01 --end 2024-01-31
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30 --append
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30 --token your-token --instance-url https://misskey.io
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30 --max-retries 5
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- search <keyword>
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- search <keyword> --start 2024-01-01 --end 2024-01-31
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view --show-id --start 2024-01-01 --end 2024-01-31
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view-html --open
```

### テスト

コンソールアプリケーションのテスト:

```bash
dotnet test PastNotes.Console.Tests/PastNotes.Console.Tests.csproj
```

### 配布用ビルド（publish）

エンドユーザー向けに .NET SDK 不要で実行できる自己完結型の単一 .exe をビルドする手順です。

```powershell
dotnet publish PastNotes.Console/PastNotes.Console.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish
```

`.\publish\PastNotes.exe` が生成されます。`publish/` は `.gitignore` で除外されているためコミットされません。

```powershell
.\publish\PastNotes.exe fetch --days 30
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

### すべてのテストを実行

```bash
dotnet test
```

環境変数が設定されていない場合、統合テストは環境変数不足で失敗します。

### カバレッジレポート

各テストプロジェクトの `.csproj` に `CollectCoverage=true` が設定されているため、追加のフラグなしで `dotnet test` を実行するだけでカバレッジレポートが自動生成されます。

```bash
dotnet test --filter "Category=Unit"
```

レポートは各テストプロジェクト配下の `coverage/coverage.cobertura.xml` に生成されます(例: `PastNotes.Tests/coverage/coverage.cobertura.xml`、`PastNotes.Console.Tests/coverage/coverage.cobertura.xml`)。

**注意**: カバレッジレポートは実行した全テストが成功した場合のみ生成されます。統合テスト用の環境変数(`MISSKEY_INSTANCE_URL`・`MISSKEY_API_TOKEN`)を設定せずにフィルタなしで `dotnet test` を実行すると、統合テストが失敗しレポートが無言で生成されません。カバレッジを確認する際は上記のように `--filter "Category=Unit"` を付けて実行してください。

`--collect:"XPlat Code Coverage"` を指定した場合は、上記とは別に `coverlet.collector` によるレポートが各テストプロジェクトの `TestResults/<GUID>/coverage.cobertura.xml` に生成されます。出力先が異なるだけで内容は同じため、通常は指定不要です。
