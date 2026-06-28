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
- **タイムゾーン**: ノートの日時はUTCからJST（日本標準時）に変換して表示
- **添付ファイル**: ノートに添付されたファイル（画像など）の情報を取得・表示
- **HTML出力**: ノートをHTMLファイルとして生成し、画像をブラウザで表示
- **API制限**: Misskey APIの`sinceDate`と`untilDate`パラメータはバグがあるため使用せず、クライアント側で日付フィルタリングを行う

### 前提条件

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) がインストールされていること

### 使用方法

#### 認証情報の設定

認証情報は以下の3つの方法で指定できます。優先順位は **CLI 引数 > 環境変数 > .env ファイル** の順です。

**方法1: CLI 引数（推奨・一時的な使用に便利）**

    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30 --token your-api-token
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30 --token your-api-token --instance-url https://your.instance.example

**方法2: 環境変数**

```powershell
$env:MISSKEY_INSTANCE_URL = "https://misskey.io"
$env:MISSKEY_API_TOKEN = "your-api-token"
```

**方法3: `.env` ファイル（初回設定に便利）**

プロジェクトルートに `.env` ファイルを作成します（`.gitignore` に追加することを推奨）。

```
MISSKEY_INSTANCE_URL=https://misskey.io
MISSKEY_API_TOKEN=your-api-token
```

APIトークンは Misskey の「設定 → API」から取得できます。

#### 取得系コマンド

    # ノート取得（過去30日間）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30

    # ノート取得（日付範囲指定・日付はJSTで指定）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --start 2024-01-01 --end 2024-01-31

    # ノート取得（日時範囲指定）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --start "2024-01-01 00:00:00" --end "2024-01-31 23:59:59"

    # ノート取得（既存のnotes.jsonにマージ・重複IDは新しい方を優先）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --days 30 --append
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- fetch --start 2024-01-01 --end 2024-01-31 --append

    # ノート検索（全期間）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- search <keyword>

    # ノート検索（日付絞り込み・日付はJSTで指定）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- search <keyword> --start 2024-01-01 --end 2024-01-31

#### 表示系コマンド

    # ノート表示（全期間）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view

    # ノート表示（日付絞り込み・日付はJSTで指定）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view --start 2024-01-01 --end 2024-01-31

    # ノート表示（IDを含める）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view --show-id

    # ノート表示（日付絞り込み + ID表示の組み合わせ）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view --show-id --start 2024-01-01 --end 2024-01-31

ノートに添付ファイルがある場合、ファイル名、タイプ、URLが表示されます。

    # ノートをHTMLファイルに出力（html_output/notes.html に生成）
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view-html

    # ノートをHTMLファイルに出力してブラウザで開く
    dotnet run --project PastNotes.Console/PastNotes.Console.csproj -- view-html --open

`view-html` はカレントディレクトリの `html_output/notes.html` にHTMLファイルを生成します。`--open` を付けるとブラウザで自動的に開きます。

#### 運用上の注意事項

- **保存先**: `fetch` コマンドを実行したカレントディレクトリに `notes.json` として保存されます。
- **上書き**: デフォルトでは `fetch` を実行するたびに `notes.json` が**上書き**されます。`--append` を指定すると既存データにマージします（重複IDは新しい方を優先）。
- **`view` の表示順序**: `fetch` で保存した順序（新着順）で表示されます。
- **`search` で 0 件の場合**: ヒットしなかった場合は `Found 0 notes matching '...'` と表示し、終了コード 0 を返します（`notes.json` が存在しない場合は終了コード 1）。

改善計画・既知の問題は [IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md) を参照してください。
開発者向けドキュメントは [DEVELOPMENT.md](DEVELOPMENT.md) を参照してください。
