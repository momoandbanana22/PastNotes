# PastNotes

## プロジェクト概要

Misskey.ioから、自分の書いたノートを、期間指定で読みだすツールです。

### プロジェクトゴール

- Misskey.ioのAPIを使用して、指定した期間内に自分が投稿したノートを取得する ✅
- 取得したノートをローカルに保存・管理する機能を提供する ✅
- ユーザーが簡単に過去のノートを検索・閲覧できるようにする ✅

### 技術仕様

- **ページネーション**: Misskey APIの`untilId`パラメータを使用して、重複なしで全ノートを取得
- **APIエンドポイント**: `/api/users/notes`（ノート取得）、`/api/i`（認証）
- **データ形式**: JSON形式でノートを保存（`notes.json`）

### 使用方法

#### 環境変数の設定

```powershell
$env:MISSKEY_INSTANCE_URL = "https://misskey.io"
$env:MISSKEY_API_TOKEN = "your-api-token"
```

#### コマンド

```bash
# ノート取得（過去30日間）
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30

# ノート検索
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- search <keyword>

# ノート表示
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view

# ノート表示（IDを含める）
dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view --show-id
```

詳細なテスト計画は [TEST_PLAN.md](TEST_PLAN.md) を参照してください。
開発者向けドキュメントは [DEVELOPMENT.md](DEVELOPMENT.md) を参照してください。
