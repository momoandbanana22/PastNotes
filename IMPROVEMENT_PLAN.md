# 改善計画 (Improvement Plan)

## 概要

プロジェクトレビューで識別された問題点をTDD（テスト駆動開発）で改善する計画。

## TDDルール

各改善項目に対して以下の手順を守る：

1. **テストファースト**: 失敗するテストを先に書く
2. **小さなステップ**: 一度に小さな機能単位で実装
3. **リファクタリング**: テストが通った後、コードの品質を改善
4. **継続的統合**: 各ステップ後にテストを実行

---

## BUG: バグ修正

### [x] BUG-1. NoteRepositoryの非同期一貫性を改善する

**問題**: メソッド名に`Async`が付いているのに同期実装（`File.WriteAllText` など）。非同期に修正し呼び出し元（FetchCommand, SearchCommand, ViewCommand）も対応済み。

---

### [x] BUG-2. HttpClientのライフサイクル管理を改善する

**問題**: `Program.cs` で `new HttpClient()` を毎回生成しソケット枯渇のリスク。シングルトンに修正済み。

---

### [x] BUG-3. キャッシュの有効期限管理を実装する

**問題**: キャッシュに有効期限チェックがなく古いデータが返り続けた。タイムスタンプ付きで TTL チェックを実装済み。

---

### [x] BUG-4. エラーハンドリングを改善する

**問題**: `GetNotesFromApiAsync` で `EnsureSuccessStatusCode()` のみで詳細なエラーハンドリングが不足。`HandleErrorResponse` で 404/429/500 等を個別処理済み。

---

### [x] BUG-5. IMisskeyApiClientインターフェースを拡張する

**問題**: インターフェースが `GetNotesAsync` のみ定義で DI・テスト容易性が低かった。使用されているメソッドを追加済み。

---

### [x] BUG-6. コンソールアプリの非同期呼び出しを改善する

**問題**: 非同期メソッドを `.GetAwaiter().GetResult()` で同期呼び出しし、デッドロックリスクがあった。`async Task Main` に変更済み。

---

### [x] BUG-7. ページネーション早期終了バグ

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（`GetNotesWithPaginationFromApiAsync` 内）

**問題**: `if (!filteredNotes.Any()) hasMoreNotes = false;` が不正。Misskey は新着順にノートを返すため、対象期間より新しいノートしか含まないページで誤って終了してしまう。

**具体的な失敗シナリオ**:
- ユーザーが 2026 年のノートを持ち、`--start 2024-01-01 --end 2024-01-31` を指定した場合
- 最初のページが 2026 年のノート 100 件 → `filteredNotes` が空 → ループ終了
- 2024 年のノートが存在しても 0 件が返される

**正しい終了条件**: 「現在ページの最古ノートが `startDate` より前になった場合に終了」。TDD で修正済み。

---

### [x] BUG-8. モックの `_callCount` バグによりテストが常に空リストを返す

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`（`MockHttpMessageHandler`）

**問題**: 非ページネーションモードで `_callCount > 1` を空配列の返却条件にしているが、`_callCount` は `/api/i`（認証）リクエストでも加算される。そのため `/api/users/notes` への最初のリクエスト到達時には既に `_callCount == 2` となり、常に `[]` が返される。

**具体的な失敗シナリオ**:
1. `/api/i` リクエスト → `_callCount = 1`
2. `/api/users/notes` リクエスト → `_callCount = 2` → `if (_callCount > 1)` が true → `[]` 返却
3. ノート件数を検証するテストが誤った前提で通過してしまう

**修正案**: `/api/users/notes` 呼び出しのみカウントする `_notesCallCount`（既に追加済み）を非ページネーションモードでも使用する。

---

### [ ] BUG-9. `--days` と `--start/--end` のタイムゾーン処理の不整合

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`

**問題**:
- `ExecuteAsync(int days)` は `DateTime.Now`（マシンローカル時刻）をそのまま API に渡す
- `ExecuteAsync(DateTime, DateTime)` は入力を JST として扱い 9 時間引いて UTC に変換する

UTC 環境（CI や Linux サーバーなど）では同じ期間を指定しても取得されるノートが異なる。

**関連テスト**: TST-2

---

### [ ] BUG-10. `convertedEndDate` への +1 秒が stale なロジック

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`（約 47 行目）

**問題**: `endDate.AddHours(-9).AddSeconds(1)` の +1 秒は、API の `untilDate` パラメータを inclusive にするための補正だった。しかしそのパラメータは削除済みであり、現在はクライアント側の `note.CreatedAt <= endDate` フィルタに余分な 1 秒が混入している。

ユーザーが `--end "2024-01-31 23:59:59"` と指定した場合、`2024-02-01 00:00:00 JST` 丁度のノートがフィルタを通過してしまう。

**関連テスト**: TST-5

---

### [ ] BUG-11. テストモックの `.Result` によるデッドロックリスク

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`（約 36 行目）

**問題**: `request.Content.ReadAsStringAsync().Result` が非同期コンテキスト内でブロッキング待機を行っており、シングルスレッドの同期コンテキストではデッドロックが発生しうる。

**修正案**: `SendAsync` を `async Task<HttpResponseMessage>` に変更し `await request.Content.ReadAsStringAsync()` を使用する。

---

### [ ] BUG-12. NoteHtmlGenerator の XSS 脆弱性

**対象ファイル**: `PastNotes/NoteHtmlGenerator.cs`（50行目、123行目）

**問題**: `note.Text`・`file.Name`・`file.Url` を HTML エスケープせずに文字列補間で埋め込んでいる。Misskey のノートに `<script>` タグが含まれると、`view-html` で生成した HTML をブラウザで開いたときに実行される。

**修正案**: `System.Net.WebUtility.HtmlEncode()` でエスケープしてから埋め込む。

**関連テスト**: TST-11

---

### [ ] BUG-13. Windows 専用タイムゾーン ID

**対象ファイル**: `PastNotes.Console/Commands/ViewCommand.cs`（33行目）、`PastNotes/NoteHtmlGenerator.cs`（8行目）

**問題**: `TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")` は Windows 専用 ID。Linux/macOS では `"Asia/Tokyo"` でないと `TimeZoneNotFoundException` でクラッシュする。

**修正案**:
```csharp
var jstZone = TimeZoneInfo.FindSystemTimeZoneById(
    OperatingSystem.IsWindows() ? "Tokyo Standard Time" : "Asia/Tokyo");
```

---

### [ ] BUG-14. テスト用コードが本番ロジックに混入

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（202〜205行目）

**問題**: `if (InstanceUrl.Contains("invalid-instance"))` というテスト専用の特殊文字列判定が本番コードに残っている。テストはコンストラクタのバリデーションや HTTP モックで代替すべき。

---

### [ ] BUG-15. `notes` が複数回列挙されてデシリアライズが重複する

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（315〜329行目）

**問題**: `GetNotesFromApiAsync` が返す `IEnumerable<Note>` は `ParseApiResponse` の遅延評価 LINQ チェーン。`notes.Any()`・`notes.Last()`・`notes.Count()` と 3 回列挙され、JSON デシリアライズが 3 回走る。

**修正案**: `GetNotesFromApiAsync` の戻り値を `ToList()` で評価してから使う。

---

### [ ] BUG-16. 未使用の死にコード

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（385〜391行目、274行目、279〜291行目）

**問題**:
- `MisskeyApiResponse` クラス: `ParseApiResponse` が `JsonElement` を直接使うため参照ゼロ
- `GetAuthorizationHeader()`: インターフェース外で呼び出し元なし
- `HandleErrorResponse(int, string)`: `HandleErrorResponse(HttpResponseMessage)` と重複、かつ挙動が異なる（`case 500` で `ApiException` vs `ServerErrorException`）

---

## TST: テスト追加

### [ ] TST-1. 対象期間より古いノートしかない場合のページネーション終了

**関係するユースケース**: `fetch --start/--end`（ページネーション）

**問題**: 「新しいノートが先に来るケース」は BUG-7 修正で対応済みだが、「全ノートが `startDate` より前」のとき早期終了ロジックが正しく機能し 0 件を返すことのテストがない。

---

### [ ] TST-2. `--days` と `--start/--end` の変換ロジック比較テスト（→ BUG-9 のテスト）

**関係するユースケース**: `fetch --days` / `fetch --start/--end`

**問題**: `ExecuteAsync(int days)` は `DateTime.Now` をそのまま API に渡す。`ExecuteAsync(DateTime, DateTime)` は -9h（JST→UTC）変換をする。同じ期間を指定しても返るノートが異なりうるが、比較するテストがない。

---

### [ ] TST-3. 対象期間内にノートが 0 件の場合の FetchCommand 動作

**関係するユースケース**: `fetch --start/--end`

**問題**: API は接続できるがフィルタ後に 0 件になるとき、`No notes found.` を出して exit 0 を返す動作がモックで検証されていない。

---

### [ ] TST-4. `CreatedAt` の `DateTimeKind` が save/load で保持されるか

**関係するユースケース**: `view` / `search`

**問題**: JSON ラウンドトリップで `DateTimeKind.Utc` が `Unspecified` になると `view` コマンドの JST 変換がずれる。件数の一致は検証しているが `DateTimeKind` まで検証するテストがない。

---

### [ ] TST-5. `endDate` ちょうどのノートが含まれるか（上限境界）（→ BUG-10 のテスト）

**関係するユースケース**: `fetch --start/--end`

**問題**: `+1 秒` ロジック（BUG-10）の影響で `endDate` ちょうどのノートが含まれるか曖昧。意図を明示する境界テストがない。

---

### [ ] TST-6. 壊れた JSON ファイルの読み込み

**関係するユースケース**: `view` / `search`

**問題**: 不正 JSON で `LoadFromFileAsync` が例外を投げるか空リストを返すか未定義。`view` や `search` がクラッシュする可能性がある。

---

### [ ] TST-7. 401 Unauthorized での CLI 終了コードとメッセージ

**関係するユースケース**: `fetch`（エラー系）

**問題**: 無効なトークンで HTTP 401 が返ったとき、CLI の終了コードとエラーメッセージが適切かを検証するモックテストがない。

---

### [ ] TST-8. JST 日付変更またぎの変換確認

**関係するユースケース**: `fetch --start/--end`

**問題**: JST 2024-01-01 00:00:00 = UTC 2023-12-31 15:00:00 のように、月初・年初の指定で UTC 変換後に前日になることの明示的な検証がない。

---

### [ ] TST-9. ページネーション中のネットワーク断

**関係するユースケース**: `fetch`（エラー系）

**問題**: `GetNotesWithPaginationFromApiAsync` の途中で `HttpRequestException` が発生したとき、リトライされず例外がそのまま伝播する。このパスのテストがない。

---

### [ ] TST-10. CLI レベルのトークンなしエラー

**関係するユースケース**: `fetch`（エラー系）

**問題**: `MISSKEY_API_TOKEN` 環境変数が未設定のとき、CLI の終了コードとエラーメッセージが保証されていない。

---

### [ ] TST-11. HTML 出力の XSS 対策テスト（→ BUG-12 のテスト）

**関係するユースケース**: `view-html`

**問題**: `note.Text` に HTML 特殊文字（`<`, `>`, `&`）が含まれる場合にエスケープされることのテストがない。

---

### [ ] TST-12. `search` で 0 件ヒットしたときの終了コードと出力テスト

**関係するユースケース**: `search`

**問題**: `notes.json` が存在しない（exit 1）と、検索結果が 0 件（exit 0）の区別が CLI レベルでテストされていない。

---

### [ ] TST-13. `fetch` を 2 回実行すると `notes.json` が上書きされることのテスト

**関係するユースケース**: `fetch`

**問題**: 既存の `notes.json` がある状態で `fetch` を実行すると無条件に上書きされるが、そのことを確認するテストがない。

---

## DOC: ドキュメント

### [x] DOC-1. MisskeyApiClientのTODOコメントを整理する

**問題**: 実装が完了しているにもかかわらず TODO コメントが残っていた。削除済み。

---

### [ ] DOC-2. DEVELOPMENT.md に削除済み `--jst` フラグの使用例が残っている

**対象ファイル**: `DEVELOPMENT.md`（14 行目付近）

**問題**: `fetch --start 2024-01-01 --end 2024-01-31 --jst` という例が残っているが、`--jst` フラグは削除済み。実行すると `--jst` は無視されて処理は続行されるが、JST 変換が行われると誤解させる。

---

### [ ] DOC-3. README に運用上の重要事項を追記する

**問題**:
- `notes.json` がカレントディレクトリに保存されること、および上書きされることの説明がない
- 実行前提条件（.NET 10 SDK が必要）の記載がない
- `view` の表示順序（保存順）が記載されていない
- `search` で 0 件ヒットしたときの挙動が記載されていない

---

## FEAT: 機能追加

### [ ] FEAT-1. `fetch` 中の進捗表示

**問題**: ノートが多い場合、ページネーションが何十回も走るが進捗が表示されない。ユーザーには無言の待機になる。ページ取得ごとに件数を出力するだけでも改善になる。

---

### [ ] FEAT-2. `fetch` の追記モード（上書きではなくマージ）

**問題**: `fetch` を実行するたびに `notes.json` を完全上書きする。異なる期間を複数回 fetch して統合する手段がない。`--append` フラグで既存データにマージする機能があるとよい。

---

### [ ] FEAT-3. `view`・`search` に日付絞り込みオプションを追加

**問題**: `fetch` で広い期間を取得した後、`view` や `search` で日付を絞り込む手段がない。`view --start 2024-01-01 --end 2024-01-31` のようなオプションがあると、再 fetch なしで絞り込める。

---

### [ ] FEAT-4. 環境変数以外での認証情報指定

**問題**: インスタンス URL と API トークンを環境変数でしか渡せない。初回利用者には設定方法がわかりにくく、`.env` ファイルや `--token` 引数での指定があると使いやすい。

---

## REL: リリース整備

### [ ] REL-1. ユーザー向けに .exe で実行できるようにする

**問題**: README の使用方法が `dotnet run --project ...` になっており、.NET SDK のインストールと長いコマンドが必要。エンドユーザーには不親切。

**目標**: `dotnet publish` でシングルファイルの自己完結型 .exe をビルドし、README をそれで実行できる手順に更新する。

**実装案**:

```powershell
dotnet publish PastNotes.Console/PastNotes.Console.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish
```

実行後は `.\publish\PastNotes.Console.exe fetch --days 30` で動くようになる。

**対応が必要なファイル**:
- `README.md` — `dotnet run` をやめて `.\PastNotes.Console.exe` の手順に変更
- `DEVELOPMENT.md` — publish 手順を追記
- `.gitignore` — `publish/` ディレクトリを除外

---

### [ ] REL-2. GitHub Actions でリリース自動化（exe 配布）

**問題**: REL-1 で .exe ビルドができるようになっても、リリースの作成・配布が手動では継続的なメンテナンスが負担になる。

**目標**: `v*` タグをプッシュしたら自動で .exe をビルドし、GitHub Releases にアップロードされる CI/CD パイプラインを構築する。

**実装案** (`.github/workflows/release.yml`):

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build-and-release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Run tests
        run: dotnet test --filter "Category=Unit"

      - name: Publish
        run: |
          dotnet publish PastNotes.Console/PastNotes.Console.csproj `
            -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=true -o ./publish

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: ./publish/PastNotes.Console.exe
```

**リリース手順（タグを切るだけ）**:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

**対応が必要なファイル**:
- `.github/workflows/release.yml` — 新規作成
