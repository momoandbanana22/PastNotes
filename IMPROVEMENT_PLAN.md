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

### [x] BUG-9. `--days` と `--start/--end` のタイムゾーン処理の不整合

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`

**問題**:
- `ExecuteAsync(int days)` は `DateTime.Now`（マシンローカル時刻）をそのまま API に渡す
- `ExecuteAsync(DateTime, DateTime)` は入力を JST として扱い 9 時間引いて UTC に変換する

UTC 環境（CI や Linux サーバーなど）では同じ期間を指定しても取得されるノートが異なる。

**関連テスト**: TST-2

---

### [x] BUG-10. `convertedEndDate` への +1 秒が stale なロジック

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`（約 47 行目）

**問題**: `endDate.AddHours(-9).AddSeconds(1)` の +1 秒は、API の `untilDate` パラメータを inclusive にするための補正だった。しかしそのパラメータは削除済みであり、現在はクライアント側の `note.CreatedAt <= endDate` フィルタに余分な 1 秒が混入している。

ユーザーが `--end "2024-01-31 23:59:59"` と指定した場合、`2024-02-01 00:00:00 JST` 丁度のノートがフィルタを通過してしまう。

**関連テスト**: TST-5

---

### [x] BUG-11. テストモックの `.Result` によるデッドロックリスク

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`（約 36 行目）

**問題**: `request.Content.ReadAsStringAsync().Result` が非同期コンテキスト内でブロッキング待機を行っており、シングルスレッドの同期コンテキストではデッドロックが発生しうる。

**修正案**: `SendAsync` を `async Task<HttpResponseMessage>` に変更し `await request.Content.ReadAsStringAsync()` を使用する。

---

### [x] BUG-12. NoteHtmlGenerator の XSS 脆弱性

**対象ファイル**: `PastNotes/NoteHtmlGenerator.cs`（50行目、123行目）

**問題**: `note.Text`・`file.Name`・`file.Url` を HTML エスケープせずに文字列補間で埋め込んでいる。Misskey のノートに `<script>` タグが含まれると、`view-html` で生成した HTML をブラウザで開いたときに実行される。

**修正案**: `System.Net.WebUtility.HtmlEncode()` でエスケープしてから埋め込む。

**関連テスト**: TST-11

---

### [x] BUG-13. Windows 専用タイムゾーン ID

**対象ファイル**: `PastNotes.Console/Commands/ViewCommand.cs`（33行目）、`PastNotes/NoteHtmlGenerator.cs`（8行目）

**問題**: `TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")` は Windows 専用 ID。Linux/macOS では `"Asia/Tokyo"` でないと `TimeZoneNotFoundException` でクラッシュする。

**修正案**:
```csharp
var jstZone = TimeZoneInfo.FindSystemTimeZoneById(
    OperatingSystem.IsWindows() ? "Tokyo Standard Time" : "Asia/Tokyo");
```

---

### [x] BUG-14. テスト用コードが本番ロジックに混入

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（202〜205行目）

**問題**: `if (InstanceUrl.Contains("invalid-instance"))` というテスト専用の特殊文字列判定が本番コードに残っている。テストはコンストラクタのバリデーションや HTTP モックで代替すべき。

---

### [x] BUG-15. `notes` が複数回列挙されてデシリアライズが重複する

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（315〜329行目）

**問題**: `GetNotesFromApiAsync` が返す `IEnumerable<Note>` は `ParseApiResponse` の遅延評価 LINQ チェーン。`notes.Any()`・`notes.Last()`・`notes.Count()` と 3 回列挙され、JSON デシリアライズが 3 回走る。

**修正案**: `GetNotesFromApiAsync` の戻り値を `ToList()` で評価してから使う。

---

### [x] BUG-16. 未使用の死にコード

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（385〜391行目、274行目、279〜291行目）

**問題**:
- `MisskeyApiResponse` クラス: `ParseApiResponse` が `JsonElement` を直接使うため参照ゼロ
- `GetAuthorizationHeader()`: インターフェース外で呼び出し元なし
- `HandleErrorResponse(int, string)`: `HandleErrorResponse(HttpResponseMessage)` と重複、かつ挙動が異なる（`case 500` で `ApiException` vs `ServerErrorException`）

---

### [x] BUG-17. `AuthenticateAsync` にテスト用文字列判定が残っている（BUG-14 の残り）

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（75行目）

**問題**: HttpClient なしコンストラクタを使用した場合の `AuthenticateAsync` で `ApiToken != "invalid-token"` という特定文字列による判定が残っている。BUG-14 で `"invalid-instance"` チェックは削除されたが、同種の問題がこのコンストラクタとダミーデータ返却（lines 223-230）に残存。

**修正案**: HttpClient なしのコンストラクタを削除するか、テスト専用であることをより明示し、本番コードから切り離す。

---

### [x] BUG-18. `GetNotesWithRetry` が実装されているが使われていない

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（317行目〜）

**問題**: リトライ機能（指数バックオフ）が実装され、テストも存在するが、`FetchCommand` は `GetNotesAsync` を直接呼んでおり `GetNotesWithRetry` は一切使われていない。ページネーション中のネットワーク障害時（TST-9）にリトライが行われず例外がそのまま伝播する。

**修正案（選択肢）**:
- A: `FetchCommand` で `GetNotesWithRetry` を使う（リトライ機能を有効にする）
- B: `GetNotesWithRetry` とそのテストを削除する（死にコードを消す）

**対処（A を採用）**: リトライ機能が必要なため A を選択。`FetchCommand.FetchAndSaveAsync` で `GetNotesAsync` の代わりに `GetNotesWithRetry(maxRetries: 3)` を呼ぶよう変更。ネットワーク障害時・レート制限時に指数バックオフで最大3回リトライされる。`FetchCommandTests` の全モック設定も `GetNotesWithRetry` に更新。

---

### [x] BUG-19. `GetNotesAsync` 他でテスト用ダミーデータを返すパスが残存（BUG-14/BUG-17 の残り）

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（222〜230行目、276行目、325行目）

**問題**: HttpClient なしで生成した場合に以下3メソッドがテスト専用ダミーデータを返すか、それに依存する:
- `GetNotesAsync`: ハードコードされたダミーノート2件を返す
- `GetNotesWithPagination`: `GetNotesAsync` にフォールスルーし間接的にダミーデータを返す
- `GetNotesWithRetry`: 同上

本番コードに「HttpClient なし = ダミーデータ」という特殊ケースが存在するべきでない。BUG-14 で `invalid-instance`、BUG-17 で `invalid-token` と同種の問題を修正したが、このパスは見落としていた（RETRO-1 の原因のひとつ）。

**修正案**: 3メソッドの HttpClient なしパスでダミーデータを返す代わりに `InvalidOperationException` をスローする。依存しているテスト `GetNotesAsync_WhenCalledWithValidDateRange_ReturnsNotesWithinRange` はモック HttpClient を使うよう書き直す。

---

### [x] BUG-20. `SearchCommand` が UTC を JST に変換せずに表示する

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`（`Execute()` 約44行目、`ExecuteAsync()` 約66行目）

**問題**:
- `note.CreatedAt`（UTC）をそのまま `{note.CreatedAt:yyyy-MM-dd HH:mm}` で表示している
- `ViewCommand` は `TimeZoneInfo.ConvertTimeFromUtc` で JST に変換してから `{jstTime:yyyy-MM-dd HH:mm:ss}` で表示している
- README の技術仕様「UTCからJSTに変換して表示」と一致していない
- フォーマットも `HH:mm`（秒なし）と `HH:mm:ss`（秒あり）で一致していない

**修正案**: `ViewCommand` と同様に UTC→JST 変換と `HH:mm:ss` フォーマットを適用する。`Execute()` と `ExecuteAsync()` 両方に修正が必要。

---

### [x] BUG-21. `PastNotes.csproj` が `OutputType=Exe` のままでライブラリとして不適切

**対象ファイル**: `PastNotes/PastNotes.csproj`（4行目）、`PastNotes/Program.cs`

**問題**: `PastNotes` プロジェクトは `PastNotes.Console` から参照されるクラスライブラリとして機能しているが、`<OutputType>Exe</OutputType>` と宣言されており、`Program.cs` にテンプレートの `Hello, World!` エントリポイントが残存している。ライブラリを Exe として宣言することは意図を誤解させ、`dotnet run --project PastNotes` が誤って実行できてしまう。

**修正案**: `<OutputType>Exe</OutputType>` を削除（デフォルトは Library）し、`PastNotes/Program.cs` を削除する。

---

### [x] BUG-22. `GetNotesWithRetry` が1ページ（最大100件）しか取得しない（BUG-18 の退行）

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（`GetNotesWithRetryFromApiAsync` メソッド）

**問題**: `GetNotesWithRetryFromApiAsync` は内部で `GetNotesFromApiAsync`（1ページ取得）を呼んでいる。`GetNotesWithPaginationFromApiAsync`（全ページ取得）を呼んでいないため、`GetNotesWithRetry` で取得できるノートが最大100件に制限される。

BUG-18 の修正で `FetchCommand` が `GetNotesAsync` から `GetNotesWithRetry` に切り替わった結果、`fetch` コマンドが最大100件しか取得できないという退行が発生した。`GetNotesAsync` と `GetNotesWithPagination` は正しく `GetNotesWithPaginationFromApiAsync` を呼んでいるが、`GetNotesWithRetry` だけが呼んでいない。

**修正案**: `GetNotesWithRetryFromApiAsync` 内の `GetNotesFromApiAsync` を `GetNotesWithPaginationFromApiAsync` に変更する。リトライのロジック（指数バックオフ）は変更しない。

---

### [x] BUG-23. `GetNotesWithRetryFromApiAsync` 末尾の `throw` が到達不可能な dead code

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（342行目）

**問題**: `throw new RateLimitExceededException("Max retries exceeded");` は実行されない dead code。最後のリトライ（`retryCount == maxRetries`）で例外が発生した場合、`catch` の `when (retryCount < maxRetries)` が false になるため例外は捕捉されず、while ループ外へ直接伝播する。コードの意図（最大リトライ超過時に特定メッセージを投げる）と実装が一致していない。

**対処**: `while (retryCount <= maxRetries)` を `while (true)` に変更し、`when` ガードなしの `catch (HttpRequestException)` / `catch (RateLimitExceededException)` を追加して、リトライ上限到達時の最後の例外を明示的に `RateLimitExceededException("Max retries exceeded")` に変換するようにした。これにより末尾の到達不可能な `throw` も不要になり削除した。TDDで `GetNotesWithRetry_WhenMaxRetriesExceeded_ThrowsMaxRetriesExceededMessage` を追加して検証済み。

---

### [x] BUG-24. `FetchCommand` の表示部分で `TimeZoneHelper.Jst` の代わりに `AddHours(9)` をハードコード

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`（27行目）

**問題**: `ExecuteAsync(int days)` の表示部分で `var jstNow = utcNow.AddHours(9);` とハードコード。`ViewCommand`、`SearchCommand`、`NoteHtmlGenerator` は全て `TimeZoneHelper.Jst` と `TimeZoneInfo.ConvertTimeFromUtc` を使用しており、コードベース内で不統一。JST に夏時間はないため動作上の問題はないが、タイムゾーン変換ロジックの担当箇所が `TimeZoneHelper` に集約されていない。

**対処**: `utcNow.AddHours(9)` を `TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneHelper.Jst)` に変更した。TDD で `ExecuteAsync_WithDays_PrintsJstDateRange` を追加して出力に `(JST)` が含まれることを検証済み。

---

### [x] BUG-25. 新規テストで `Console.SetOut` の復元が `finally` 外（BUG-20 のテスト追加分）

**対象ファイル**: `PastNotes.Console.Tests/Commands/SearchCommandTests.cs`（`Execute_WhenCalledWithUtcDateTime_ConvertsToJst`、`Execute_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds`）

**問題**: 両テストとも以下のパターンで `Console.Out` を退避・復元している。

```csharp
System.Console.SetOut(stringWriter);
command.Execute("Test");
var output = stringWriter.ToString();
System.Console.SetOut(originalOutput);
```

`command.Execute` が例外を投げた場合、`Console.SetOut(originalOutput)` が実行されず `Console.Out` が `using` で破棄済みの `StringWriter` を指したままになる。`PastNotes.Console.Tests` は `DisableTestParallelization = true` のため並列干渉はないが、同一プロセス内で後続に実行されるテストが `Console.Out` への書き込み時に `ObjectDisposedException` を起こす可能性がある。

**対処**: `Execute_WhenDateRangeSpecified_ShowsOnlyNotesInRange`・`Execute_WhenCalledWithUtcDateTime_ConvertsToJst`・`Execute_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds`・`ExecuteAsync_WhenNotesExistButNoMatch_ReturnsZero` の4テストすべてで `Console.SetOut(originalOutput)` を `finally` ブロックに移動した。

---

### [x] BUG-26. `SearchCommand.Execute`/`ExecuteAsync` で検索結果を二重に列挙している

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`（`Execute()` 約40行目、`ExecuteAsync()` 約69行目）

**問題**: `_repository.SearchByKeyword(notes, keyword)` は遅延評価の `IEnumerable<Note>` を返す。`results.Count()` で1回列挙した後、続く `foreach (var note in results)` で再度列挙しており、検索処理が実質2回走る。BUG-15 で `MisskeyApiClient` 側の同種の問題（多重列挙によるデシリアライズ重複）を修正した際と同じパターン。

**対処**: `Execute()`・`ExecuteAsync()` 両方で `SearchByKeyword(...).ToList()` に変更し、`results.Count()`（拡張メソッド）を `results.Count`（プロパティ）に変更。TDD で `Execute_FoundCountMatchesActualOutputLines` を追加して件数と出力行数の一致を検証済み。

---

### [x] BUG-27. JST⇔UTC変換のオフセットが `TimeZoneHelper` を介さず複数箇所にハードコードされている

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`（コンストラクタ、17〜18行目）、`PastNotes.Console/Commands/ViewCommand.cs`（コンストラクタ、20〜21行目）、`PastNotes.Console/Commands/FetchCommand.cs`（39〜40行目、BUG-24 参照）

**問題**: `SearchCommand`・`ViewCommand` のコンストラクタはユーザー入力（JST想定）を `AddHours(-9)` で直接 UTC に変換している。表示側では `TimeZoneInfo.ConvertTimeFromUtc` と `TimeZoneHelper.Jst` を使っているにもかかわらず、入力側の変換だけ `TimeZoneHelper` を経由しない別ロジックになっており、同じ「JSTオフセット」の知識が複数箇所に分散している。BUG-24 と根本原因は同じ。

**対処**: `TimeZoneHelper.ConvertToUtc(DateTime jstTime)` を追加し（`TimeZoneInfo.ConvertTimeToUtc` でラップ）、`SearchCommand`・`ViewCommand`・`FetchCommand` の `AddHours(-9)` を全て `TimeZoneHelper.ConvertToUtc` に統一した。TDD で `ConvertToUtc_WhenJstNewYearMidnight_ReturnsPreviousDayUtc` / `ConvertToUtc_WhenJstNoon_ReturnsUtcMorning` を追加して検証済み。

---

### [x] BUG-28. `GetNotesWithRetryFromApiAsync` の2つの `catch` ブロックが完全に同一処理

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（328〜339行目）

**問題**: `catch (HttpRequestException) when (...)` と `catch (RateLimitExceededException) when (...)` の本体（`retryCount++`、`await Task.Delay(delay)`、バックオフ計算）が完全に同一。リトライ処理を変更する際に2箇所を同時に直す必要があり、片方だけ修正漏れするリスクがある。

**対処**: 4つの `catch` ブロックを `catch (Exception ex) when (ex is HttpRequestException || ex is RateLimitExceededException)` の1つに統合し、ブロック内の `if/else` でリトライか `MaxRetriesExceeded` スローかを分岐するよう変更。TDD で `GetNotesWithRetry_WhenHttpRequestExceptionThenSucceeds_RetriesAndReturnsNotes` および `GetNotesWithRetry_WhenHttpRequestExceptionExhaustsMaxRetries_ThrowsMaxRetriesExceededMessage` を追加して `HttpRequestException` パスを検証済み。

---

### [x] BUG-29. `ViewCommand` で `notes.Count()` と `foreach` が二重列挙している（BUG-26 の適用漏れ）

**対象ファイル**: `PastNotes.Console/Commands/ViewCommand.cs`（`Execute()` 41・44行目、`ExecuteAsync()` 84・87行目）

**問題**: `Execute()` および `ExecuteAsync()` で、`--start/--end` 指定時に `FilterByDateRange` が返す遅延評価の `IEnumerable<Note>` を `notes.Count()` と `foreach` で2回列挙している。BUG-26 で `SearchCommand` に適用した `.ToList()` 修正が `ViewCommand` には適用されていなかった。

元コレクションは `JsonSerializer.Deserialize` の結果（実態は `Note[]`）のため動作上の誤りは生じないが、`SearchCommand` との実装の一貫性が欠ける。

**修正案**: `Execute()` と `ExecuteAsync()` 両方で、`FilterByDateRange` 呼び出し後に `.ToList()` を適用し、`notes.Count()` を `notes.Count`（プロパティ）に変更する。

**対処（部分）**: `FilterByDateRange` 後の `.ToList()` 適用のみ実施。`notes.Count()` → `notes.Count` への変更は未適用（BUG-31 参照）。

---

### [x] BUG-30. 統合テストの `Assert.True(false, message)` がビルド警告を発生させている

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`（478・509・538・578 行目）

**問題**: 統合テストで環境変数が未設定のときに `Assert.True(false, message)` を使用しており、`xUnit2020` アナライザー警告が 4 件発生している。RELEASE_CHECKLIST の「ビルドエラー・警告がゼロ（`dotnet build`）」が `[x]` 済みになっているが、実際は 4 件の警告が存在する状態。xUnit 2.9.0 以降では `Assert.Fail(message)` を使用すべきと静的解析が警告している。

**対処**: 4 箇所の `Assert.True(false, message)` を `Assert.Fail(message)` に置き換えた。`dotnet build` の警告が 4 件 → 0 件になったことを確認済み。

---

### [x] BUG-31. BUG-29 の残り作業: `ViewCommand` の `notes.Count()` が `notes.Count` プロパティになっていない

**対象ファイル**: `PastNotes.Console/Commands/ViewCommand.cs`（`Execute()` 41行目、`ExecuteAsync()` 84行目）

**問題**: BUG-29 の修正コミット（"fix: ViewCommand の FilterByDateRange 後に .ToList() を適用"）で `FilterByDateRange` 後の `.ToList()` 適用は行われたが、IMPROVEMENT_PLAN に記載していた `notes.Count()` → `notes.Count`（プロパティ）への変更が未適用のまま残っている。`notes` の宣言型が `IEnumerable<Note>` のため `.Count` プロパティが直接呼べず、拡張メソッド `Count()` が残存している。動作上の問題は生じないが（`ICollection<T>` の最適化により O(1)）、`SearchCommand` が `results.Count`（プロパティ）を使用しているのと実装が一致しない。

**修正案**: `notes` の宣言型を `List<Note>` に変更し（`LoadFromFileAsync` 結果に即座に `.ToList()` を適用）、`notes.Count()` を `notes.Count` に変更する。`FilterByDateRange` 後の `.ToList()` は維持する。

**対処**: `Execute()` で `LoadFromFileAsync(...).GetAwaiter().GetResult().ToList()`、`ExecuteAsync()` で `(await LoadFromFileAsync(...)).ToList()` に変更し `notes` を `List<Note>` として宣言。`notes.Count()` → `notes.Count` に変更（2箇所）。`notes == null ||` の不要な null チェックも除去。TDD で既存の 44 件ユニットテストがリファクタリング前後とも全件パスすることを確認済み。

---

### [x] BUG-32. `search`・`view` で不正な日付フォーマットが無視される

**対象ファイル**: `PastNotes.Console/Program.cs`（116〜117行目、140〜141行目）

**問題**: `fetch` コマンドは `--start`/`--end` の日付パースに失敗すると `"Error: Invalid start/end date format"` を出力して exit 1 する。しかし `search`・`view` コマンドは同じ状況で `TryParse` の失敗を無視し、フィルタなしで全件処理してしまう。ユーザーが `--start invalid-date` と指定すると、エラーは出ず全ノートが表示/検索される。

**修正案**: `search`・`view` の日付パースを `TryParse` のワンライナーから分離し、失敗時はエラーメッセージを出力して exit 1 する（`fetch` と同一パターン）。

**対処**: `Program.cs` の `search`（116〜117行目）と `view`（140〜141行目）を修正。TDD で `SearchCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError`・`ViewCommand_WhenInvalidEndDate_ReturnsOneAndPrintsError` を追加して検証済み。

---

### [x] BUG-33. `NoteHtmlGenerator.GenerateHtml` で `note.Id` が HTML エンコードされていない（XSS）

**対象ファイル**: `PastNotes/NoteHtmlGenerator.cs`（18行目）

**問題**: `GenerateHtml` の `<title>` タグで `note.Id` を `WebUtility.HtmlEncode` せずに直接埋め込んでいる。同メソッド内の `note.Text`・`file.Url`・`file.Name` はすべて `WebUtility.HtmlEncode` を通しており、`note.Id` だけが漏れている。Misskey の ID は英数字のみのため実害は低いが、`</title><script>alert(1)</script>` のような値が入ると HTML 構造の破壊またはスクリプト注入が発生しうる。

**修正案**: `note.Id` を `WebUtility.HtmlEncode(note.Id)` に変更する。

**対処**: `NoteHtmlGenerator.cs` 18行目を `WebUtility.HtmlEncode(note.Id)` に変更。TDD で `GenerateHtml_WhenNoteIdContainsHtmlSpecialChars_EncodesIdInTitle` を追加して検証済み。107件ユニットテスト全件パス。

---

### [x] BUG-34. `search`・`view` で `--start`/`--end` に値を指定しないとエラーなく無視される

**対象ファイル**: `PastNotes.Console/Program.cs`（116行目、125行目、156行目、165行目付近）

**問題**: BUG-32 で「不正な日付フォーマット」のエラー処理を追加したが、`--start`/`--end` オプション自体に後続の値が存在しない場合（例: `pastnotes search keyword --end`）はエラーが出ない。`sEndIdx + 1 < args.Length` の条件が偽になり if ブロックをスキップし、日付フィルタが `null` のまま全件処理が走る。`fetch` コマンドは `sIdx >= 0 && sIdx + 1 < args.Length && eIdx >= 0 && eIdx + 1 < args.Length` の複合条件で「どちらかが欠けたら Usage 表示」になっており、`search`・`view` との扱いが非対称。

**具体的な失敗シナリオ**: `pastnotes search keyword --end` を実行すると `--end` が無視されて全期間を検索し、ユーザーは絞り込みが効いていないことに気づけない。

**修正案**: `search`・`view` の各 `if (idx >= 0 ...)` ブロックを、`idx >= 0 && idx + 1 >= args.Length` の場合にもエラーを返すよう拡張する（例: `"Error: --end requires a date value"`）。または `--start`/`--end` が存在するときは値も必須とし、なければ exit 1 する。

**対処**: `Program.cs` の `search`（116行目）と `view`（156行目）の両ブロックで `if (idx >= 0)` を外側の条件とし、値が存在しない場合に `"Error: --start/--end requires a date value"` を出力して return 1 するよう変更。TDD で `SearchCommand_WhenStartFlagHasNoValue_ReturnsOneAndPrintsError`・`ViewCommand_WhenEndFlagHasNoValue_ReturnsOneAndPrintsError` を追加して検証済み。109件ユニットテスト全件パス。

---

### [x] BUG-35. `MisskeyApiClient` がライブラリ内で直接 `System.Console.WriteLine` を呼び出している

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（288行目）

**問題**: `GetNotesWithPaginationFromApiAsync` のページネーションループ内で `System.Console.WriteLine($"  取得中... {allNotes.Count} 件")` を直接呼び出している。`PastNotes` はライブラリプロジェクトであり、コンソール出力を直接持つべきではない（Console に依存するとコンソールアプリ以外での再利用が困難、テスト出力が汚れる）。`FetchCommandTests` 等で `Console.SetOut` を差し替えてもこのメッセージは通常出力として混入し続ける。

**修正案**: `IProgress<string>` やコールバック Action などを引数として受け取る形に変更し、進捗出力の責任を呼び出し元（`FetchCommand`）に委ねる。または `System.Console.WriteLine` を条件付きコンパイルまたは設定フラグで無効化できるようにする。

**対処**: `IMisskeyApiClient.GetNotesWithRetry` / `GetNotesWithPagination` に `Action<string>? progress = null` を追加。`MisskeyApiClient` の内部実装で `Console.WriteLine` を `progress?.Invoke(...)` に置き換え。`FetchCommand` では `progress: msg => System.Console.WriteLine(msg)` を渡すことで進捗表示を維持。TDD で `GetNotesWithRetry_WhenProgressProvided_InvokesCallback`・`GetNotesWithRetry_WhenNoProgressProvided_WritesNothingToConsole` を追加、既存の `GetNotesAsync_WhenPaginating_PrintsProgressMessages` を `GetNotesAsync_WhenPaginating_WritesNothingToConsole` に更新。FetchCommandTests の Setup/Verify/Callback も 4引数対応に修正。111件ユニットテスト全件パス。

---

### [x] BUG-36. `fetch` の `--token` / `--instance-url` に値を指定しないとエラーなく環境変数にフォールバックする

**対象ファイル**: `PastNotes.Console/Program.cs`（34〜41行目）

**問題**: BUG-34 で `search`・`view` の `--start`/`--end` に値がない場合のエラー処理を追加したが、`fetch` の `--token` と `--instance-url` は同じ状況でエラーを返さない。例えば `pastnotes fetch --days 30 --token` と実行すると `--token` の値が取得できず、環境変数 `MISSKEY_API_TOKEN` への暗黙のフォールバックが行われる。ユーザーが意図したトークンが使われないまま処理が進む。

**具体的な失敗シナリオ**: `pastnotes fetch --days 30 --token` → `--token` が無視されて環境変数のトークンで認証が行われ、ユーザーは気づけない。

**修正案**: `--token`・`--instance-url` の各ブロックで `idx >= 0 && idx + 1 >= args.Length` の場合に `"Error: --token/--instance-url requires a value"` を出力して return 1 する（BUG-34 と同一パターン）。

---

### [x] BUG-37. `--max-retries` に値を指定しないとエラーなくデフォルト 3 にフォールバックする

**対象ファイル**: `PastNotes.Console/Program.cs`（65行目）

**問題**: `--token` / `--instance-url` はフラグの直後に値がない場合にエラーメッセージを出力して exit 1 するが（BUG-36）、`--max-retries` は同じ状況で無音にデフォルト値 3 を使用する。

```csharp
var maxRetries = (maxRetriesIdx >= 0 && maxRetriesIdx + 1 < args.Length && int.TryParse(...)) ? mr : 3;
```

ユーザーが `--max-retries` とだけ入力してリトライ回数を変更しようとした場合、エラーは出ず、意図しないデフォルト値で処理が進む。

**具体的な失敗シナリオ**: `pastnotes fetch --days 30 --max-retries` → エラーなしでリトライ回数 3 のまま実行され、ユーザーは設定が無視されたことに気づけない。

**修正案**: `--max-retries` の直後に値が存在しない場合（`maxRetriesIdx >= 0 && maxRetriesIdx + 1 >= args.Length`）に `"Error: --max-retries requires a number value"` を出力して return 1 する。`int.TryParse` が失敗する場合（値が数値でない）も同様にエラーを出す（BUG-34・BUG-36 と同一パターン）。

---

### [x] BUG-38. `FetchCommand` のエラー出力が stderr / stdout で不統一

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`（80〜84行目）、`PastNotes.Console/Program.cs`（119〜123行目）

**問題**: `FetchAndSaveAsync` は `UnauthorizedException` のみを個別に捕捉し `Console.Error.WriteLine`（stderr）へ出力する。しかしリトライ上限超過時に `GetNotesWithRetry` が投げる `RateLimitExceededException("Max retries exceeded")` は `FetchCommand` でキャッチされず、`Program.cs` の汎用 `catch (Exception ex)` に到達し `Console.WriteLine`（stdout）で出力される。

```csharp
// FetchCommand.cs — UnauthorizedException は stderr
catch (UnauthorizedException ex)
    System.Console.Error.WriteLine($"Error: Unauthorized - {ex.Message}");

// Program.cs — それ以外は stdout（不統一）
catch (Exception ex)
    System.Console.WriteLine($"Error: {ex.Message}");
```

**具体的な失敗シナリオ**: `stderr 2>/dev/null` でエラーを抑制しているシェルスクリプトからこのツールを呼ぶと、`UnauthorizedException` は抑制されるが `RateLimitExceededException` はそのまま stdout に出力されてパース対象に混入する。

**修正案**: `FetchAndSaveAsync` の `catch` ブロックを拡張し、`UnauthorizedException` に加えて `RateLimitExceededException`（リトライ上限超過）その他の `ApiException` サブクラスも `Console.Error.WriteLine` で出力して return 1 する。または `Program.cs` の汎用ハンドラを `Console.Error.WriteLine` に統一する。

**対処**: `FetchAndSaveAsync` に `catch (ApiException ex)` ブロックを追加（`UnauthorizedException` の直後）。これにより `RateLimitExceededException`・`ServerErrorException`・`NotFoundException` を含む全 `ApiException` サブクラスが stderr へ出力される。TDD（RED → GREEN）で対処：失敗テスト2件（`ExecuteAsync_WhenMaxRetriesExceeded_ReturnsOneAndPrintsToStderr`、`ExecuteAsync_WhenServerError_ReturnsOneAndPrintsToStderr`）追加後に実装し、全55件ユニットテストパスを確認済み。

---

### [x] BUG-39. `--max-retries 0` 時のエラーメッセージが誤解を招く

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（`GetNotesWithRetryFromApiAsync` の else ブロック）

**問題**: `maxRetries=0`（リトライ無効）のとき、最初のリクエストが失敗すると `RateLimitExceededException("Max retries exceeded")` を投げる。README は「`--max-retries 0` でリトライを無効化できる」と説明しているが、受け取るエラーメッセージは「リトライ回数を超過した」と主張しており矛盾する。また `HttpRequestException` などの元のエラーメッセージも失われる。

**対処**: TDD で対応。失敗テスト `GetNotesWithRetry_WhenMaxRetriesIsZeroAndRequestFails_ThrowsOriginalErrorMessage`（429 モック・`maxRetries: 0` で `ex.Message` が `"Rate limit exceeded"` であることを検証）を先に追加し RED を確認後、`throw new RateLimitExceededException("Max retries exceeded")` を `throw new RateLimitExceededException(retryCount == 0 ? ex.Message : "Max retries exceeded")` に変更して GREEN を確認した。

横展開確認: `Max retries exceeded`・`retryCount`・`maxRetries` を Grep で検索し、リトライメッセージ分岐ロジックが存在するのは `MisskeyApiClient.GetNotesWithRetryFromApiAsync` の1箇所のみであることを確認済み（他のヒットは呼び出し元での引数の受け渡しのみ）。

`PastNotes.Tests` 68件、`PastNotes.Console.Tests` 65件、全ユニットテストパス確認済み。

---

### [x] BUG-40. 日本語出力が文字化けする

**対象ファイル**: `PastNotes.Console/Program.cs`、`PastNotes.Console/Commands/ViewCommand.cs`（54・97行目）、`PastNotes/MisskeyApiClient.cs`（272行目）

**問題**: 以下の箇所が日本語テキストを `Console.WriteLine` で出力するが、`Program.cs` に `Console.OutputEncoding` の設定がないため、Windows PowerShell のデフォルトエンコーディング（CP932）と衝突して文字化けする。

- `MisskeyApiClient.cs:272` — `"  取得中... {allNotes.Count} 件"`（進捗メッセージ）
- `ViewCommand.cs:54,97` — `"  添付ファイル:"`（`view` コマンドの表示）

ユニットテストは `StringWriter` でキャプチャするためエンコーディング問題が隠れ、統合テスト実行まで気づかなかった。

**対処**: TDD で対応。失敗テスト `Main_WhenStarted_SetsConsoleOutputEncodingToUtf8`（`Program.Main` 実行後に `Console.OutputEncoding.WebName` が `"utf-8"` であることを検証）を先に追加し、実行環境の既定値が `Codepage - 932` であることを確認して RED を確認した後、`Program.cs` の `Main` 冒頭に `Console.OutputEncoding = System.Text.Encoding.UTF8;` を追加して GREEN を確認した（TST-30 のテストを兼ねる）。

横展開確認: `static.*Main\(` を Grep で検索し、エントリポイントは `PastNotes.Console/Program.cs` の1箇所のみ（BUG-21 でライブラリ側の `Program.cs` は削除済み）であることを確認済み。

`PastNotes.Tests` 68件、`PastNotes.Console.Tests` 66件、全ユニットテストパス、`dotnet build` 警告0件を確認済み。

---

## REFACTOR: リファクタリング

*動作を変えずにコード構造・一貫性を改善する変更。*

### [x] REFACTOR-1. `SearchCommand` の到達不能な `notes == null` チェックを削除

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`（25行目、54行目）

**背景**: `NoteRepository.LoadFromFileAsync` はファイル不在時に `Enumerable.Empty<Note>()`、JSON 破損時に `InvalidDataException` を返すため null にはならない。BUG-31 で `ViewCommand` の同チェックを削除したが、`SearchCommand.Execute()`・`ExecuteAsync()` には残存していた。

**変更**: `if (notes == null || !notes.Any())` → `if (!notes.Any())`（2箇所）。動作不変。

**対処**: 変更後、既存109件ユニットテスト全件パスで動作不変を確認済み。

---

### [x] REFACTOR-2. `ViewHtmlCommand` の到達不能な `notes == null` チェックを削除

**対象ファイル**: `PastNotes.Console/Commands/ViewHtmlCommand.cs`（24行目）

**背景**: REFACTOR-1 と同一原因。`ViewCommand` は BUG-31 で修正済みだが `ViewHtmlCommand` には未適用だった。

**変更**: `if (notes == null || !notes.Any())` → `if (!notes.Any())`（1箇所）。動作不変。

**対処**: REFACTOR-1 と同一コミットで対処。

---

### [x] REFACTOR-3. `FetchCommand` の到達不能な `notes == null` チェックを削除

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`（54行目）

**背景**: REFACTOR-1 で `SearchCommand`、REFACTOR-2 で `ViewHtmlCommand` の `notes == null` チェックを削除したが、`FetchCommand.FetchAndSaveAsync` には同じパターンが残存している。

```csharp
if (notes == null || !notes.Any())
```

`GetNotesWithRetry` は内部で `List<Note>` を返すため null にはならない。

**変更**: `if (notes == null || !notes.Any())` → `if (!notes.Any())`。動作不変。

---

### [x] REFACTOR-4. `MisskeyApiClient.GetUserIdAsync()` が `IMisskeyApiClient` 外の dead public API

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（135〜149行目）、`PastNotes/IMisskeyApiClient.cs`

**問題**: `GetUserIdAsync()` は `public` メソッドだが `IMisskeyApiClient` インターフェースに定義されておらず、プロダクションコードからも外部テストからも呼ばれていない。`GetNotesFromApiAsync` は認証を `AuthenticateWithApiAsync()` を直接呼んで行っており、`GetUserIdAsync` を経由していない。

**影響**: 利用者がインターフェース経由でモックを使う場合に不要な混乱を招く。また `_userId` フィールドを経由するロジックが `GetUserIdAsync` 経由と `AuthenticateWithApiAsync` 直接経由の 2 経路に分岐しているため、将来の変更時に片方だけ修正する漏れが起きやすい。

**修正案**: `GetUserIdAsync()` を削除し、ユーザー ID が必要な箇所（`GetNotesFromApiAsync`）では既存の `AuthenticateWithApiAsync` 呼び出しパターンを引き続き使う。または `IMisskeyApiClient` に追加して意図的な公開 API として整備する（外部利用がない現状では削除が望ましい）。

**対処**: `GetUserIdAsync()` メソッド（15行）を `MisskeyApiClient.cs` から削除。テストコード・プロダクションコードいずれからも参照なしのため影響なし。全 122 件ユニットテストパス確認済み。削除により `PastNotes` のライン カバレッジが 88.41% → 90.55% に向上（dead code 除去の副作用）。

---

## TST: テスト追加

### [x] TST-1. 対象期間より古いノートしかない場合のページネーション終了

**関係するユースケース**: `fetch --start/--end`（ページネーション）

**問題**: 「新しいノートが先に来るケース」は BUG-7 修正で対応済みだが、「全ノートが `startDate` より前」のとき早期終了ロジックが正しく機能し 0 件を返すことのテストがない。

---

### [x] TST-2. `--days` と `--start/--end` の変換ロジック比較テスト（→ BUG-9 のテスト）

**関係するユースケース**: `fetch --days` / `fetch --start/--end`

**問題**: `ExecuteAsync(int days)` は `DateTime.Now` をそのまま API に渡す。`ExecuteAsync(DateTime, DateTime)` は -9h（JST→UTC）変換をする。同じ期間を指定しても返るノートが異なりうるが、比較するテストがない。

---

### [x] TST-3. 対象期間内にノートが 0 件の場合の FetchCommand 動作

**関係するユースケース**: `fetch --start/--end`

**問題**: API は接続できるがフィルタ後に 0 件になるとき、`No notes found.` を出して exit 0 を返す動作がモックで検証されていない。

---

### [x] TST-4. `CreatedAt` の `DateTimeKind` が save/load で保持されるか

**関係するユースケース**: `view` / `search`

**問題**: JSON ラウンドトリップで `DateTimeKind.Utc` が `Unspecified` になると `view` コマンドの JST 変換がずれる。件数の一致は検証しているが `DateTimeKind` まで検証するテストがない。

---

### [x] TST-5. `endDate` ちょうどのノートが含まれるか（上限境界）（→ BUG-10 のテスト）

**関係するユースケース**: `fetch --start/--end`

**問題**: `+1 秒` ロジック（BUG-10）の影響で `endDate` ちょうどのノートが含まれるか曖昧。意図を明示する境界テストがない。

---

### [x] TST-6. 壊れた JSON ファイルの読み込み

**関係するユースケース**: `view` / `search`

**問題**: 不正 JSON で `LoadFromFileAsync` が例外を投げるか空リストを返すか未定義。`view` や `search` がクラッシュする可能性がある。

---

### [x] TST-7. 401 Unauthorized での CLI 終了コードとメッセージ

**関係するユースケース**: `fetch`（エラー系）

**問題**: 無効なトークンで HTTP 401 が返ったとき、CLI の終了コードとエラーメッセージが適切かを検証するモックテストがない。

---

### [x] TST-8. JST 日付変更またぎの変換確認

**関係するユースケース**: `fetch --start/--end`

**問題**: JST 2024-01-01 00:00:00 = UTC 2023-12-31 15:00:00 のように、月初・年初の指定で UTC 変換後に前日になることの明示的な検証がない。

---

### [x] TST-9. ページネーション中のネットワーク断

**関係するユースケース**: `fetch`（エラー系）

**問題**: `GetNotesWithPaginationFromApiAsync` の途中で `HttpRequestException` が発生したとき、リトライされず例外がそのまま伝播する。このパスのテストがない。

---

### [x] TST-10. CLI レベルのトークンなしエラー

**関係するユースケース**: `fetch`（エラー系）

**問題**: `MISSKEY_API_TOKEN` 環境変数が未設定のとき、CLI の終了コードとエラーメッセージが保証されていない。

---

### [x] TST-11. HTML 出力の XSS 対策テスト（→ BUG-12 のテスト）

**関係するユースケース**: `view-html`

**問題**: `note.Text` に HTML 特殊文字（`<`, `>`, `&`）が含まれる場合にエスケープされることのテストがない。

---

### [x] TST-12. `search` で 0 件ヒットしたときの終了コードと出力テスト

**関係するユースケース**: `search`

**問題**: `notes.json` が存在しない（exit 1）と、検索結果が 0 件（exit 0）の区別が CLI レベルでテストされていない。

---

### [x] TST-13. `fetch` を 2 回実行すると `notes.json` が上書きされることのテスト

**関係するユースケース**: `fetch`

**問題**: 既存の `notes.json` がある状態で `fetch` を実行すると無条件に上書きされるが、そのことを確認するテストがない。

---

### [x] TST-14. `ViewCommand.Execute()`（同期版）の日付フィルタリングテストがない

**関係するユースケース**: `view --start/--end`

**問題**: `ExecuteAsync` の日付フィルタリングはテスト済みだが、同期版 `Execute()` への同等テストがない。`SearchCommand` では同様の欠落が実際のバグ（同期版フィルタリング未適用）として発覚した前例があり、`ViewCommand` も同一リスクを持つ。

---

### [x] TST-15. `UnitTest1.cs` のプレースホルダーテストが両テストプロジェクトに残っている

**対象ファイル**:
- `PastNotes.Tests/UnitTest1.cs`（`true == true` を検証するだけの無意味なテスト）
- `PastNotes.Console.Tests/UnitTest1.cs`（メソッドが空の無意味なテスト）

**問題**: プロジェクトテンプレートのまま残っており、テストカバレッジのノイズになる。テスト結果の可読性を下げ、将来のコントリビューターに誤解を与える可能性がある。

**修正案**: 両ファイルを削除する（あるいはファイルごと削除する）。

---

### [x] TST-16. `PastNotes.Tests` に `DisableTestParallelization` がない（TST-15 の作業漏れ）

**対象ファイル**: `PastNotes.Tests/`（AssemblyConfig.cs が存在しない）

**問題**: TST-15 の修正で `PastNotes.Console.Tests/UnitTest1.cs` 削除時に `AssemblyConfig.cs` を作成して `DisableTestParallelization` を保持したが、`PastNotes.Tests/UnitTest1.cs` 削除時には同様の対応を行わなかった。`MisskeyApiClient.GetNotesWithPaginationFromApiAsync` は `System.Console.WriteLine` でプログレスを出力する（グローバル状態への書き込み）。`GetNotesAsync_WhenPaginating_PrintsProgressMessages` はこの出力を `Console.SetOut(stringWriter)` で捕捉して検証しており、並列実行時に他のテストのプログレスメッセージが意図しない StringWriter に混入する可能性がある。

**対処**: `PastNotes.Tests/AssemblyConfig.cs` を作成し `[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]` を追加した。`PastNotes.Console.Tests/AssemblyConfig.cs` と同一フォーマット。

---

### [x] TST-17. `Console.SetOut`/`SetError` が `finally` 外（BUG-25 の適用漏れ）

**対象ファイル**:
- `PastNotes.Console.Tests/Commands/FetchCommandTests.cs`（3 テスト）
- `PastNotes.Console.Tests/Commands/ViewCommandTests.cs`（7 テスト）

**問題**: BUG-25 で `SearchCommandTests` の 4 テストについて `Console.SetOut(originalOutput)` を `finally` ブロックへ移動したが、同じパターンが `FetchCommandTests` と `ViewCommandTests` に残存している。テストが予期しない例外を投げた場合、`Console.Out`（または `Console.Error`）が破棄済みの `StringWriter` を指したままになり、後続テストが `ObjectDisposedException` で失敗しうる。`DisableTestParallelization = true` のため並列干渉はないが、例外発生時のリスクは BUG-25 と同一。

**FetchCommandTests で未対応のテスト**:
- `ExecuteAsync_WithDateRange_WhenNoNotesFound_ReturnsZeroAndPrintsMessage`
- `ExecuteAsync_WithDays_WhenNoNotesFound_ReturnsZeroAndPrintsMessage`
- `ExecuteAsync_WhenApiReturns401_ReturnsOneAndPrintsError`（`Console.SetError`）

**ViewCommandTests で未対応のテスト**:
- `Execute_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds`
- `Execute_WhenCalledWithExistingNotes_HidesIdByDefault`
- `Execute_WhenCalledWithShowIdOption_DisplaysId`
- `Execute_WhenCalledWithUtcDateTime_ConvertsToJst`
- `Execute_WhenDateRangeSpecified_ShowsOnlyNotesInRange`
- `ExecuteAsync_WhenDateRangeSpecified_ShowsOnlyNotesInRange`
- `Execute_WhenNoteHasFiles_DisplaysFileInformation`

**修正案**: 各テストの `Console.SetOut(originalOutput)` / `Console.SetError(originalError)` を `finally` ブロックに移動する（BUG-25 と同一パターン）。

**対処**: FetchCommandTests 3件（SetOut ×2、SetError ×1）と ViewCommandTests 7件（SetOut ×7）の計10テストすべてで復元処理を `finally` ブロックに移動した。ViewCommandTests の同一パターン5件は一括置換で対応。ビルド警告ゼロ、44件ユニットテスト全件パスを確認済み。

---

### [x] TST-18. `PastNotes.Console.Tests` の `coverlet.collector` バージョンが古い（TST-16 との不整合）

**対象ファイル**: `PastNotes.Console.Tests/PastNotes.Console.Tests.csproj`

**問題**: `PastNotes.Tests` は `coverlet.collector 10.0.1` と `coverlet.msbuild 10.0.1` を使用しているが、`PastNotes.Console.Tests` は `coverlet.collector 6.0.4` のみで `coverlet.msbuild` が未追加。カバレッジ収集方法・バージョンが 2 プロジェクト間で一致していない。TST-16 の際に `PastNotes.Tests` の `csproj` を更新したが、`PastNotes.Console.Tests` の更新が漏れた可能性がある。

**修正案**: `PastNotes.Console.Tests.csproj` の `coverlet.collector` を `10.0.1` に更新し、`coverlet.msbuild 10.0.1` を追加する。

**対処**: `coverlet.collector` を `6.0.4` → `10.0.1` に更新し `IncludeAssets`/`PrivateAssets` を追加。`coverlet.msbuild 10.0.1` を追加。`CollectCoverage`・`CoverletOutputFormat`・`CoverletOutput` の PropertyGroup を追加し `PastNotes.Tests.csproj` と完全に統一した。`dotnet restore` 後にビルド警告ゼロ、104件ユニットテスト全件パス、両プロジェクトのカバレッジ合算集計が動作することを確認済み。

---

### [x] TST-19. `ConsoleAppTests.FetchCommand_WhenApiTokenMissing` の try ブロック内に `SetOut` 復元が重複（TST-17 の適用漏れ）

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`（186行目）

**問題**: TST-17 で `FetchCommandTests`・`ViewCommandTests` の `Console.SetOut/SetError` 復元を `finally` ブロックへ移動したが、`ConsoleAppTests` の `FetchCommand_WhenApiTokenMissing_ReturnsOneAndPrintsError` テストには同じパターンが残っている。try ブロック内 186行目と finally ブロックで `System.Console.SetOut(originalOutput)` を二重に呼んでいる。同ファイルの他のテスト（`SearchCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError` 等）は finally のみで復元しており不一致。

**修正案**: try ブロック内の `System.Console.SetOut(originalOutput)` 186行目を削除する。

**対処**: 229行目の重複 `System.Console.SetOut(originalOutput)` を削除。BUG-35/36 と同一コミットで対処。109件ユニットテスト全件パス。

---

### [x] TST-20. `ConsoleAppTests.FetchCommand_WhenAppendPrecedesStartEnd_ParsesArgsCorrectly` の try ブロック内に `SetOut` 復元が重複（TST-19 の適用漏れ）

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`（113行目）

**問題**: TST-19 で `FetchCommand_WhenApiTokenMissing_ReturnsOneAndPrintsError` の try ブロック内 `Console.SetOut(originalOutput)` を削除したが、同一ファイルの `FetchCommand_WhenAppendPrecedesStartEnd_ParsesArgsCorrectly` に同じパターンが残っている。113行目（try ブロック内）と120行目（finally ブロック）の両方で `System.Console.SetOut(originalOutput)` を呼んでいる。

**修正案**: 113行目の try ブロック内の `System.Console.SetOut(originalOutput)` を削除する（finally ブロックで保証されているため不要）。

---

### [x] TST-21. `FetchCommand_WhenAppendPrecedesStartEnd_ParsesArgsCorrectly` が接続失敗 + 3回リトライでテストが遅い

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`（`FetchCommand_WhenAppendPrecedesStartEnd_ParsesArgsCorrectly` テスト）

**問題**: このテストは `--instance-url http://localhost:1` を指定して `Program.Main` を呼ぶ。`FetchCommand` は `GetNotesWithRetry(maxRetries: 3)` を使用しており、接続失敗時に指数バックオフ（1秒 + 2秒 + 4秒 = 最低7秒）でリトライする。その結果、このテスト1件だけで~7秒以上かかり、Console テストスイート全体の24秒の大半を占めている。

**修正案（選択肢）**:
- A: `FetchCommand` のコンストラクタで `maxRetries` をデフォルト引数として受け取れるようにし、テストでは `maxRetries: 0` を指定できるようにする
- B: テストの `--instance-url` を変更せず、テスト専用のフェイク `IMisskeyApiClient` に差し替えて接続そのものをスキップする（より根本的な解決）
- C: `Program.cs` を変更して `--max-retries` CLI オプションを追加する（スコープ外の変更になるため非推奨）

---

### [x] TST-22. `FetchCommandTests` に実質同一のテストが2件重複している

**対象ファイル**: `PastNotes.Console.Tests/Commands/FetchCommandTests.cs`（35〜65行目、68〜100行目）

**問題**: `ExecuteAsync_WhenCalledWithDateRange_ReturnsSuccess` と `ExecuteAsync_WhenCalledWithDateRange_ConvertsJstToUtcByDefault` が実質同一の内容を検証している。どちらも「JST→UTC 変換が正しく行われること」を `Verify` で確認しており、セットアップ・アサーション・意図が重複している。テストの意図の違いが名前から読み取れず、将来のメンテナンス時に混乱を招く可能性がある。

**対処**: `ExecuteAsync_WhenCalledWithDateRange_ReturnsSuccess` から `mockApiClient.Verify` 呼び出しを削除し「日付範囲指定時に exit 0 を返すこと」のみを検証するよう変更した。JST→UTC 変換の検証は `ExecuteAsync_WhenCalledWithDateRange_ConvertsJstToUtcByDefault` に集約。113件ユニットテスト全件パス。

---

### [x] TST-23. テストクラスがファイル名・名前空間と一致していない

**対象ファイル**:
- `PastNotes.Tests/NoteHtmlGeneratorTests.cs`（`TimeZoneHelperTests` クラスが同居）
- `PastNotes.Tests/NoteTests.cs`（`NoteHtmlGeneratorOutputTests`・`NoteRepositoryTests` クラスが同居）

**問題**:

1. `NoteHtmlGeneratorTests.cs` の先頭クラスが `TimeZoneHelperTests` になっており、ファイル名と内容が一致していない。`TimeZoneHelperTests` は `TimeZoneHelperTests.cs` に独立させるべき。
2. `NoteTests.cs` に `NoteHtmlGeneratorOutputTests`（HTML 生成テスト）と `NoteRepositoryTests`（リポジトリテスト）が同居しており、1ファイル1責務の原則に反する。
3. `TimeZoneHelperTests` と `NoteHtmlGeneratorTests`（`NoteHtmlGeneratorTests.cs` 内）の名前空間が `PastNotes` であり、テストプロジェクトの慣習（`PastNotes.Tests`）と一致していない。

**影響**: テスト自体は正常に実行されるため動作上の問題はないが、新しい開発者がどのファイルにどのテストがあるかを把握しにくく、テスト追加時に誤ったファイルに記述するリスクがある。

**修正案**:
- `PastNotes.Tests/TimeZoneHelperTests.cs` を新規作成し `TimeZoneHelperTests` クラスを移動（名前空間を `PastNotes.Tests` に変更）
- `NoteHtmlGeneratorTests.cs` の `NoteHtmlGeneratorTests` クラスも `PastNotes.Tests` 名前空間に変更
- `NoteTests.cs` から `NoteHtmlGeneratorOutputTests` を `NoteHtmlGeneratorTests.cs` に、`NoteRepositoryTests` を `NoteRepositoryTests.cs` に分離

---

### [x] TST-24. `PastNotes.Console` のテストカバレッジが目標 80% を下回っている

**対象ファイル**: `PastNotes.Console.Tests/`、`PastNotes.Console/Program.cs`

**問題**: `dotnet test` 実行時のカバレッジレポートで `PastNotes.Console` プロジェクトが **68.54%**（行）・**62.02%**（分岐）と、DEVELOPMENT.md に記載の目標「80%以上」を満たしていない。

主な未カバーパス（確認分）:
- `view-html` コマンドの JSON 破損時エラーパス（`InvalidDataException` が `Program.cs` catch に到達するケース）
- `fetch` で `--start`/`--end` いずれかだけ指定したときの Usage 表示パス（`Program.cs` 93行目の else 分岐）
- `FetchCommand.FetchAndSaveAsync` の `UnauthorizedException` 以外の例外伝播パス（BUG-38 関連）
- `Program.cs` の `Unknown command` 分岐

**修正案**: 上記パスをカバーするユニットテストを `ConsoleAppTests` または各コマンドの Tests クラスに追加する。BUG-38 対処と合わせて進めると効率的。

**対処**: 以下のテストを追加し、`PastNotes.Console` のラインカバレッジを **68.54% → 82.27%**（分岐 62.02% → 76.58%、メソッド 100%）に引き上げた。全 132 件ユニットテストパス確認済み。
- `ConsoleAppTests`: `Unknown command` / `fetch` else 分岐 / `view-html` ノートなし・JSON 破損 / `search` キーワードなし・`--end` 無効日付 / `view` `--start` 値なし・無効日付（9 件追加）
- `ViewCommandTests`（`ViewHtmlCommandTests` クラス）: ノートなし → 1 返却 / JSON 破損 → `InvalidDataException`（2 件追加）

---

### [x] TST-25. `NoteRepository.FilterByDateRange` の `startDate == endDate` 境界値テストがない

**対象ファイル**: `PastNotes.Tests/NoteRepositoryTests.cs`、`PastNotes/NoteRepository.cs`

**問題**: `FilterByDateRange` のテストは範囲内・範囲外のケースを確認しているが、`startDate == endDate`（同一日時の1点フィルタ）でノートが含まれるかどうかを検証するテストがない。実装は `>=` と `<=` を使っているが境界が正しく含まれることをテストで保証していない。

**対処**: `FilterByDateRange_WhenStartDateEqualsEndDate_ReturnsOnlyExactMatchingNote` を追加。対象日時ちょうどのノート・1秒前・1秒後の3件を用意し、`FilterByDateRange(notes, targetDate, targetDate)` がちょうどのノート1件のみを返すことを検証した。実装（`>=`・`<=`）は既に正しく境界を含んでいたため、テスト追加のみで実装変更は不要だった（69件全ユニットテストパス）。

横展開確認: `note.CreatedAt >= ... && note.CreatedAt <= ...` を Grep で検索した結果、`MisskeyApiClient.cs:269`（ページネーション中の日付フィルタ）にも同一パターンが存在し、`startDate == endDate` 境界のテストがないことを確認した。`NoteRepository.FilterByDateRange` とは別クラスのため本コミットでは対処せず、TST-33 として記録する。

---

### [x] TST-26. `NoteHtmlGenerator.GenerateHtmlForAllNotes` の空リスト入力テストがない

**対象ファイル**: `PastNotes.Tests/NoteHtmlGeneratorTests.cs`、`PastNotes/NoteHtmlGenerator.cs`

**問題**: `GenerateHtmlForAllNotes` は複数ノートを受け取りファイルを生成するが、空リストを渡したときの動作（ファイルが生成されるか、例外が出るか、空の HTML が出力されるか）を確認するテストがない。

**対処**: `GenerateHtmlForAllNotes_WhenNotesListIsEmpty_GeneratesValidHtmlWithoutException` を追加。空リストを渡しても例外を投げず、`<!DOCTYPE html>`〜`</html>` を含む有効な HTML ファイルが生成されることを検証した。実装は `foreach` がゼロ回実行されるだけで安全なため、テスト追加のみで実装変更は不要だった（70件全ユニットテストパス）。

横展開確認: `ViewHtmlCommand` 呼び出し元でのノート0件時の挙動は TST-24 で既にテスト済み（`ViewHtmlCommandTests`: ノートなし → 1 返却）。`GenerateHtml`（単一ノート版）は空リストの概念が存在しないため対象外。他に同種の未検証箇所はなし。

---

### [x] TST-27. `SearchCommand`・`ViewCommand` でファイル破損時の例外伝播テストがない

**対象ファイル**: `PastNotes.Console.Tests/Commands/SearchCommandTests.cs`、`PastNotes.Console.Tests/Commands/ViewCommandTests.cs`

**問題**: `ViewHtmlCommand` では JSON 破損時に `InvalidDataException` が伝播することをテストしているが（`Execute_WhenCorruptedJson_ThrowsInvalidDataException`）、同じ `NoteRepository.LoadFromFileAsync` を呼ぶ `SearchCommand.ExecuteAsync` と `ViewCommand.ExecuteAsync` には対応するテストがない。

**対処**: `SearchCommandTests`・`ViewCommandTests` それぞれに `Execute_WhenCorruptedJson_ThrowsInvalidDataException`（同期版）・`ExecuteAsync_WhenCorruptedJson_ThrowsInvalidDataException`（非同期版）を追加（計4件）。課題本文は `ExecuteAsync` のみを挙げていたが、同期版 `Execute` も同一の `LoadFromFileAsync` 呼び出しで例外処理を持たないため同一コミットで対処した。実装はいずれも既に例外を捕捉せず伝播させる作りだったため実装変更は不要（70件全ユニットテストパス）。

横展開確認: `LoadFromFileAsync` の呼び出し箇所を Grep 検索した結果、`FetchCommand.cs`（`--append` 時の既存ファイル読み込み）にも同じ呼び出しがあり、破損 JSON 時の挙動を確認するテストがないことを確認した。`FetchCommand` は `catch (UnauthorizedException)`・`catch (ApiException)` のみを捕捉するため `InvalidDataException` は `Program.cs` の汎用ハンドラ（stdout）まで伝播し、`FetchCommand` の他のエラー出力が stderr に統一されている（BUG-38）のと不整合になる可能性がある。別シナリオ（`--append` 限定）のため本コミットでは対処せず、TST-34 として記録する。

---

### [x] TST-28. `NoteRepositoryTests` に `SaveToFileAsync`/`LoadFromFileAsync` の実質重複テストがある

**対象ファイル**: `PastNotes.Tests/NoteRepositoryTests.cs`

**問題**: `SaveToFileAsync_WhenCalledWithNotes_SavesNotesToFile`（同期的な動作確認）と後続の `LoadFromFileAsync` テストがほぼ同一の Arrange/Act を繰り返しており、同じ振る舞いを重複して検証している。

**対処**: リファクタリングのため新規テストは追加せず（CLAUDE.md ルール1）、削除前に既存70件のユニットテストが GREEN であることを確認した上で、以下3件の完全重複テストを削除した。`NoteRepository.SaveToFileAsync`/`LoadFromFileAsync` は非同期メソッドしか存在せず、"Async" 接尾辞の有無以外に Arrange/Act/Assert の差異がなかったため、責務分離ではなく削除で重複を解消した。
- `SaveToFileAsync_WhenCalledWithNotes_SavesNotesToFileAsync`（`SaveToFileAsync_WhenCalledWithNotes_SavesNotesToFile` と完全重複）
- `LoadFromFileAsync_WhenCalledWithValidFile_ReturnsNotesAsync`（`LoadFromFileAsync_WhenCalledWithValidFile_ReturnsNotes` と完全重複）
- `LoadFromFileAsync_WhenCalledWithInvalidFile_ReturnsEmptyListAsync`（`LoadFromFileAsync_WhenCalledWithInvalidFile_ReturnsEmptyList` と完全重複）

削除後も67件のユニットテスト全件パスし、動作不変であることを確認した（`DateTimeKind` 保持テスト・破損 JSON テストなど責務が異なるテストは維持）。

横展開確認: `\w+Async\(\)` という命名の重複パターンが他テストファイルに残っていないか Grep 検索した結果、`PastNotes.Tests` 配下に該当するメソッド名は残っていないことを確認した。`SearchCommand`/`ViewCommand` の `Execute`/`ExecuteAsync` ペアは実装自体が同期・非同期の2メソッドとして存在するため対象外（重複ではない）。

---

### [x] TST-29. `SearchCommandTests`・`ViewCommandTests` に日付フィルタリング系テストの重複がある

**対象ファイル**: `PastNotes.Console.Tests/Commands/SearchCommandTests.cs`、`PastNotes.Console.Tests/Commands/ViewCommandTests.cs`

**問題**: 両クラスに UTC→JST 変換・秒数表示・日付フィルタリングを確認するテストがほぼ同じ構造で存在しており、`NoteRepository.FilterByDateRange` の動作を重複して検証している。フィルタリングロジック自体は `NoteRepositoryTests` で検証すれば十分。

**調査結果**: 全テストを精査した結果、「安全に削除できる重複」と「削除すべきでない重複（実質は別責務）」に分かれることが判明した。

1. **安全に統合した重複**（`ViewCommandTests.cs` 内）: `Execute_WhenDateRangeSpecified_ShowsOnlyNotesInRange`（jan/feb 2件・Jan含む/Feb除外のみ検証）は、`Execute_TotalCountMatchesActualDisplayedNotes`（BUG-29のため既存・jan/feb/mar 3件で同一シナリオ+`Total notes: 1`検証まで含む上位互換）に完全に包含されていた。`ExecuteAsync` 版も同様。統合の際、前者にあった `Assert.Equal(0, result)`（戻り値検証）が後者に欠けていたため、統合先に追加した上で前者2件を削除した。
2. **削除すべきでない「重複」**: `SearchCommandTests`・`ViewCommandTests` それぞれが持つ「UTC→JST変換」（`Execute_WhenCalledWithUtcDateTime_ConvertsToJst`）・「秒数表示」（`Execute_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds`）テストは、一見構造が似ているが**各コマンドクラスが独立して同じ変換ロジックを実装している**ため、片方を消すと片方の実装だけが壊れた場合に検知できない。実際に BUG-20（`SearchCommand` だけ UTC→JST 変換をせず表示していた）はこの独立実装の食い違いによって発生した既知のバグであり、`SearchCommand`・`ViewCommand` 双方にテストがあることでこの再発を防いでいる。同様に「日付フィルタリングを呼び出すこと」を確認するテスト自体も、BUG-26（`SearchCommand`）・BUG-29（`ViewCommand`）という別々の実際のバグへの regression テストを兼ねており、コマンドクラス単位で維持する必要があると判断した。

**対処**: 上記1のみ統合により重複を解消（4件→2件、68件全ユニットテストパス、動作不変）。上記2は根本原因（コマンドごとに変換・フィルタリングロジックが独立実装されている設計、DESIGN-4参照）を解消しない限り安全に削減できないため、削除せず本項目としては対応完了とする。

---

### [x] TST-30. `Program.cs` の起動時エンコーディング設定を検証するテストがない

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`、`PastNotes.Console/Program.cs`

**問題**: `MisskeyApiClient.GetNotesWithPaginationFromApiAsync`（272行目）が日本語文字列 `"  取得中... {allNotes.Count} 件"` を出力するが、`Program.cs` に `Console.OutputEncoding = Encoding.UTF8` の設定がなく、Windows 環境で文字化けが発生する（BUG-40）。この問題はコードを読めば発見できるにもかかわらず、`Program.cs` の起動初期化コードを検証するユニットテストがなかったために見落とされた。

**対処**: BUG-40 の対処と同一コミットで対応済み。`ConsoleAppTests.Main_WhenStarted_SetsConsoleOutputEncodingToUtf8` を追加し、`Program.Main` 実行後に `Console.OutputEncoding.WebName == "utf-8"` であることを検証する。

---

### [x] TST-31. エンドツーエンドテスト（ユーザーシナリオ全体）がない

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`

**問題**: 統合テストは `MisskeyApiClient` の API 接続と `FetchCommand` の単独動作を確認しているが、`fetch` → `search`・`view`・`view-html` の一連のユーザーシナリオを通しで確認するテストが存在しない。fetch で取得・保存したノートを search/view で正しく読み込めるかどうかは、現状テストで保証されていない。

**対処**: `ConsoleAppTests.EndToEndScenario_FetchThenSearchViewViewHtml_AllCommandsSucceedConsistently`（`Category=Integration`）を追加した。実 API で `fetch --days 30` を実行し、保存されたノート件数と `view` の `Total notes: N`・`view-html` が生成する `<div class="note">` の件数が一致すること、`search` に存在しないキーワードを渡すと `Found 0 notes matching` で exit 0 になることを検証する。環境変数未設定時に `Assert.Fail` で案内が出ることを確認済み（CLAUDE.md ルール7 のとおり自動省略はしていない）。利用者が実環境（`MISSKEY_INSTANCE_URL`/`MISSKEY_API_TOKEN` 設定済み）で実行し、実ノート460件で `fetch → search → view → view-html` の一連のシナリオがパスすることを確認済み。

---

### [x] TST-32. 状態遷移テスト（コマンドの実行順序・前提条件）がない

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`

**問題**: `search`・`view`・`view-html` は `notes.json` が存在することを前提とするが、ファイルが存在しない状態での動作（「No notes found. Run 'fetch' command first.」）と、存在する状態での動作の両方を組み合わせた状態遷移テストがない。個別のコマンドのテストはあるが、実行順序による状態変化が正しく処理されることを保証するテストが不足している。

**対処**: `ConsoleAppTests.StateTransition_NotesFileAbsentThenPresent_SearchAndViewBehaveAccordingly`（`Category=Unit`）を追加した。`notes.json` なし → `search`/`view` が exit 1・`No notes found. Run 'fetch' command first.` を返す → `NoteRepository.SaveToFileAsync` で `notes.json` を作成（実 API を使う `fetch` の代わりに状態変化のみを再現）→ `search`/`view` が exit 0・期待どおりの内容を返す、という一連の状態遷移を1テストで検証する。実 API 呼び出しを伴わないため `Category=Unit`（DESIGN-2 と同様、CWD への実ファイル I/O を伴う許容範囲のユニットテスト）。69件全ユニットテストパス。

横展開確認: `view-html` も同様に `notes.json` の有無で挙動が変わるが、ファイルなし・破損 JSON の各状態は TST-24 で個別にテスト済みのため対象外とした（本項目の課題文も `search`・`view` を主眼としている）。

---

### [x] TST-33. `MisskeyApiClient` のページネーション日付フィルタに `startDate == endDate` 境界値テストがない（TST-25 の横展開）

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`、`PastNotes/MisskeyApiClient.cs`（269行目）

**問題**: TST-25 で `NoteRepository.FilterByDateRange` に `startDate == endDate` の境界値テストを追加した際、同一パターン（`note.CreatedAt >= startDate && note.CreatedAt <= endDate`）が `MisskeyApiClient.GetNotesWithPaginationFromApiAsync` のページネーション中フィルタにも存在することが判明した。こちらには対応する境界値テストがない。

**対処**: `MockHttpMessageHandler` に `SimulateBoundaryNotes(DateTime exactDate)` を追加し、対象日時ちょうど・1秒前・1秒後の3件を新着順（後→ちょうど→前）の1ページで返すようにした。`GetNotesAsync_WhenStartDateEqualsEndDate_ReturnsOnlyExactMatchingNote` を追加し、`GetNotesAsync(targetDate, targetDate)` がちょうどのノート1件のみを返すことを検証した。実装（`>=`・`<=`）は既に正しく境界を含んでいたため、テスト追加のみで実装変更は不要だった（68件全ユニットテストパス）。

---

### [x] TST-34. `FetchCommand` の `--append` モードで破損 JSON 読み込み時の挙動が未検証（TST-27 の横展開）

**対象ファイル**: `PastNotes.Console.Tests/Commands/FetchCommandTests.cs`、`PastNotes.Console/Commands/FetchCommand.cs`（65行目）

**問題**: TST-27 で `SearchCommand`・`ViewCommand` の破損 JSON テストを追加した際、`FetchCommand.FetchAndSaveAsync` も `--append` 指定時に既存の `notes.json` を `LoadFromFileAsync` で読み込んでおり、同じ `InvalidDataException` 伝播の可能性があることが判明した。`FetchCommand` は `catch (UnauthorizedException)`・`catch (ApiException)` のみを捕捉するため、この例外は `Program.cs` の汎用ハンドラ（`Console.WriteLine`、stdout）まで伝播する。`FetchCommand` の他のエラーは BUG-38 で stderr に統一済みのため、この経路だけ出力先が不整合になる可能性がある。

**対処**: TDD で対応。失敗テスト `ExecuteAsync_WhenAppendModeWithCorruptedExistingFile_ReturnsOneAndPrintsToStderr`（`--append` 指定・既存 `notes.json` が破損 JSON のケースで `command.ExecuteAsync` を直接呼び出す）を追加し、`InvalidDataException` が未捕捉のまま伝播することを確認して RED を確認した後、`FetchAndSaveAsync` に `catch (InvalidDataException ex)` を追加（`UnauthorizedException`・`ApiException` と同じ stderr 出力パターン）して GREEN を確認した。BUG-38 で確立した「`FetchCommand` のエラーは exit 1・stderr に統一する」方針を `InvalidDataException` にも適用したことになる。70件全ユニットテストパス、`dotnet build` 警告0件。

横展開確認: `view-html`（`ViewHtmlCommand`）でも破損 JSON 時に `Program.cs` の汎用ハンドラ経由で stdout にエラーが出力されることを確認した（`ConsoleAppTests.Main_WhenViewHtmlWithCorruptedJson_ReturnsOneAndPrintsError` は stdout のみを検証しており、これを裏付ける）。これは DESIGN-3（`SearchCommand`/`ViewCommand` の例外が `Program.cs` 経由で stdout に出る問題）と同一分類の課題であり、`ViewHtmlCommand` も対象に含めて DESIGN-3 側で一括検討する（本項目ではこれ以上の修正は行わない）。

---

## DOC: ドキュメント

### [x] DOC-1. MisskeyApiClientのTODOコメントを整理する

**問題**: 実装が完了しているにもかかわらず TODO コメントが残っていた。削除済み。

---

### [x] DOC-2. DEVELOPMENT.md に削除済み `--jst` フラグの使用例が残っている

**対象ファイル**: `DEVELOPMENT.md`（14 行目付近）

**問題**: `fetch --start 2024-01-01 --end 2024-01-31 --jst` という例が残っているが、`--jst` フラグは削除済み。実行すると `--jst` は無視されて処理は続行されるが、JST 変換が行われると誤解させる。

---

### [x] DOC-3. README に運用上の重要事項を追記する

**問題**:
- `notes.json` がカレントディレクトリに保存されること、および上書きされることの説明がない
- 実行前提条件（.NET 10 SDK が必要）の記載がない
- `view` の表示順序（保存順）が記載されていない
- `search` で 0 件ヒットしたときの挙動が記載されていない

---

### [x] DOC-4. `--max-retries` CLI オプションが README・DEVELOPMENT.md に記載されていない

**対象ファイル**: `PastNotes.Console/Program.cs`（64〜65行目）、`README.md`、`DEVELOPMENT.md`

**問題**: `--max-retries` オプションが `Program.cs` に実装されているが、どのドキュメントにも記載がない。

```csharp
var maxRetriesIdx = Array.IndexOf(args, "--max-retries");
var maxRetries = (maxRetriesIdx >= 0 && maxRetriesIdx + 1 < args.Length && int.TryParse(args[maxRetriesIdx + 1], out var mr)) ? mr : 3;
```

TST-21 の修正時に「Option A: FetchCommand コンストラクタへの注入」と「Option C: CLI オプション追加」の両方が実装されたが、後者のドキュメント化が漏れた。ユーザーはこのオプションの存在を知る手段がない。

**修正案**: README の `fetch` コマンド使用例と DEVELOPMENT.md のビルド・実行コマンド一覧に `--max-retries <n>` の説明を追記する。

**解決**: TDD で対応。`Main_WhenCalledWithNoArgs_UsageContainsMaxRetries` テストを先に追加して失敗を確認（RED）後、`Program.cs` の usage 文字列に `[--max-retries <n>]` を追加してテストを通過させた（GREEN）。README.md と DEVELOPMENT.md にも使用例を追記。51件ユニットテスト全件パス。

---

### [x] DOC-5. `DEVELOPMENT.md` に存在しない `run-integration-tests.ps1` が記載されている

**対象ファイル**: `DEVELOPMENT.md`（131行目付近）

**問題**: 統合テストの実行方法として以下の記述がある。

```powershell
.\run-integration-tests.ps1 -InstanceUrl "https://misskey.io" -ApiToken "your-api-token"
```

しかし `run-integration-tests.ps1` はリポジトリに存在しない。開発者がこの手順を試みると「ファイルが見つかりません」エラーになる。

**修正案（選択肢）**:
- A: `run-integration-tests.ps1` を作成し、環境変数設定と `dotnet test --filter "Category=Integration"` を実行するスクリプトを実装する
- B: 「方法2: PowerShellスクリプトを使用」ブロックを削除し、方法1（環境変数を設定して直接 `dotnet test`）のみを残す

---

### [x] DOC-6. `run-integration-tests.ps1` が存在するがどのドキュメントにも記載されていない

**対象ファイル**: `run-integration-tests.ps1`（リポジトリルート）、`DEVELOPMENT.md`

**問題**: DOC-5 の修正でコミット `6cd7fea` において「DEVELOPMENT.md から存在しない `run-integration-tests.ps1` の参照を削除」したが、当該スクリプトは実際にはリポジトリに存在している（`git log -- run-integration-tests.ps1` で確認：commit `92ad537` で追加済み）。DOC-5 解決時に選択肢 B（ドキュメント参照を削除）を適用したため、スクリプトがリポジトリに残ったまま完全に undocumented になった。

スクリプトの内容は有効（引数でインスタンス URL・API トークンを受け取り環境変数を設定して統合テストを実行）であり、削除せずに活用できる。

**具体的な失敗シナリオ**: 開発者がリポジトリをクローンして `ls` や `Get-ChildItem` でルートを確認した際に `run-integration-tests.ps1` を発見するが、README にも DEVELOPMENT.md にも説明がないため、このスクリプトが使えるものか廃止されたものかを判断できない。

**修正案（選択肢）**:
- A: DEVELOPMENT.md の「統合テストのみ実行」セクションにスクリプトの使用例を再掲載する（DOC-5 の選択肢 A を改めて適用、ファイルは既に存在）
- B: `run-integration-tests.ps1` をリポジトリから削除し、ドキュメントとコードを一致させる

**対処**: 選択肢 B を採用。スクリプトの処理内容（環境変数設定 + `dotnet test --filter "Category=Integration"`）は DEVELOPMENT.md の「方法1」と完全に重複しており、維持する価値がないと判断。`run-integration-tests.ps1` をリポジトリから削除した。

---

## FEAT: 機能追加

### [x] FEAT-1. `fetch` 中の進捗表示

**問題**: ノートが多い場合、ページネーションが何十回も走るが進捗が表示されない。ユーザーには無言の待機になる。ページ取得ごとに件数を出力するだけでも改善になる。

---

### [x] FEAT-2. `fetch` の追記モード（上書きではなくマージ）

**問題**: `fetch` を実行するたびに `notes.json` を完全上書きする。異なる期間を複数回 fetch して統合する手段がない。`--append` フラグで既存データにマージする機能があるとよい。

---

### [x] FEAT-3. `view`・`search` に日付絞り込みオプションを追加

**問題**: `fetch` で広い期間を取得した後、`view` や `search` で日付を絞り込む手段がない。`view --start 2024-01-01 --end 2024-01-31` のようなオプションがあると、再 fetch なしで絞り込める。

---

### [x] FEAT-4. 環境変数以外での認証情報指定

**問題**: インスタンス URL と API トークンを環境変数でしか渡せない。初回利用者には設定方法がわかりにくく、`.env` ファイルや `--token` 引数での指定があると使いやすい。

---

## DESIGN: 設計上の既知制約

*動作上の問題はなく、プロジェクトの規模・性質を踏まえて許容した設計上のトレードオフ。将来スケールする場合は改善候補となりうる。*

### DESIGN-1. `NoteRepository` がインターフェースを持たない

**対象ファイル**: `PastNotes/NoteRepository.cs`、`PastNotes.Console/Commands/ViewHtmlCommand.cs`、`SearchCommand.cs`、`ViewCommand.cs`、`FetchCommand.cs`

**内容**: `NoteRepository` は具象クラスのまま `IMisskeyApiClient` のようなインターフェースを持たない。各コマンドクラスはコンストラクタで `NoteRepository` を直接受け取っており、テストでは実ファイル I/O が発生する。

**許容理由**: 小規模 CLI ツールとして一貫した設計選択であり、動作上の問題はない。`NoteRepository` の実装は単純（JSON 読み書き）で変更リスクが低い。将来 DB 等への移行が必要になった場合は `INoteRepository` を切り出して DI する。

---

### DESIGN-2. `ConsoleAppTests` の一部テストが CWD にファイル I/O を行う

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`（`Main_WhenViewHtmlWithNoNotes`、`Main_WhenViewHtmlWithCorruptedJson`）

**内容**: `Program.cs` の `view-html` ブロックは `new ViewHtmlCommand(repository)` とデフォルトパス `"notes.json"` を使うため、このパスをカバーするテストは CWD に `notes.json` を読み書きする。`[Trait("Category", "Unit")]` と分類しているが、厳密には結合テストに近い。

**許容理由**: `DisableTestParallelization = true` で順次実行されるため競合しない。`finally` ブロックでクリーンアップしており実害はない。`Program.cs` の `view-html` ブロックをカバーする現実的かつ最小コストの手段として許容する。

---

### [x] DESIGN-3. `SearchCommand`・`ViewCommand`・`ViewHtmlCommand` の例外が `Program.cs` 汎用ハンドラ経由で stdout に出力される

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`、`ViewCommand.cs`、`ViewHtmlCommand.cs`、`Program.cs`

**問題**: BUG-38 で `FetchCommand` の `ApiException` を stderr に統一し、TST-34 で `--append` 時の `InvalidDataException` も同様に stderr へ統一したが、`SearchCommand.ExecuteAsync`・`ViewCommand.ExecuteAsync`・`ViewHtmlCommand.Execute` は例外をキャッチせず `Program.cs` の汎用 `catch (Exception ex)` に到達し `Console.WriteLine`（stdout）で出力される（`ViewHtmlCommand` は TST-34 の横展開確認で追加確認）。同種の例外（例：ファイル破損時の `InvalidDataException`）の出力先がコマンドによって異なる。

**対処**: 修正案の2案（各コマンドクラスに `catch` を追加 vs `Program.cs` の汎用ハンドラを直す）のうち、`fetch`・`search`・`view`・`view-html` の4箇所に完全に同一の `catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); return 1; }` が重複していたため、根本原因（同一パターンの4箇所重複）を1箇所で解消する後者を採用した（CLAUDE.md ルール3）。TDD で対応：`search`・`view` の破損 JSON テストを新規追加、`view-html` の既存テスト（`Main_WhenViewHtmlWithCorruptedJson_ReturnsOneAndPrintsError`）を stderr 検証に更新し、3件とも RED を確認した後、`Program.cs` の4箇所の `Console.WriteLine` を `Console.Error.WriteLine` に統一して GREEN を確認した。72件全ユニットテストパス、`dotnet build` 警告0件。

横展開確認: `Console.WriteLine($"Error` パターンを Grep 検索し、該当4箇所以外に残存しないことを確認した。一方で `--token requires a value` 等の引数バリデーションエラー（`Program.cs` に15箇所、例外ではなく検証失敗時の直接 `Console.WriteLine`）は本項目のスコープ外（DESIGN-3 は「例外」の出力先に関する課題）のため変更していない。ただし本対処により「例外は stderr・バリデーションエラーは stdout」という新たな出力先の不統一が可視化されたため、DESIGN-8 として記録する。

---

### [x] DESIGN-4. `SearchCommand`・`ViewCommand` に `Execute()`（同期）と `ExecuteAsync()`（非同期）が共存している

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`、`ViewCommand.cs`、`FetchCommand.cs`

**問題**: `SearchCommand` と `ViewCommand` には同期版 `Execute()` と非同期版 `ExecuteAsync()` の両方が存在し、`Execute()` は内部で `.GetAwaiter().GetResult()` を呼んでスレッドをブロックする。`FetchCommand` は `ExecuteAsync()` のみ。設計パターンが一貫していない。

**対処**: 非同期版に統一する案を採用した。事前に `Program.cs` を確認し、`searchCommand.Execute(...)`・`viewCommand.Execute(...)` の呼び出しが存在しない（`ExecuteAsync` のみ使用）ことを確認した上で、`SearchCommand.Execute(string)`・`ViewCommand.Execute()` を削除した。`ViewHtmlCommand.Execute()` は対象外（同期のままで許容、HTML 生成・ファイル I/O のため）。

テスト側は、`Execute()` 専用でテストされていたシナリオ（BUG-20 の JST 変換・秒数表示、BUG-26/BUG-29 の二重列挙防止など）を全て `ExecuteAsync()` 呼び出しに書き換えて維持し、`ExecuteAsync()` 側に同一内容の重複テストが既に存在していたもの（正常系・0件失敗・破損 JSON 系、計6件）のみ削除した。動作・カバー範囲は変更していない。

横展開確認: `SearchCommand.Execute`・`ViewCommand.Execute` への参照を Grep 検索し、削除後に呼び出し元が残っていないことを確認した。72件 → 66件（重複6件削除）、全ユニットテストパス、`dotnet build` 警告0件。

---

### [x] DESIGN-5. `IMisskeyApiClient` がページネーション・リトライという実装詳細を露出している

**対象ファイル**: `PastNotes/IMisskeyApiClient.cs`

**問題**: `IMisskeyApiClient` に `GetNotesWithPagination` と `GetNotesWithRetry` が含まれており、「どうやってノートを取得するか」という実装手段がインターフェースに漏れている。呼び出し側（`FetchCommand`）が `GetNotesWithRetry` を直接指定しなければならず、実装の切り替えが困難。

**対処**: `IMisskeyApiClient` の唯一の利用者である `FetchCommand`（および `FetchCommandTests` の Moq モック）を Grep で確認したところ、インターフェース経由で実際に呼ばれているのは `GetNotesWithRetry` のみで、`GetNotesAsync`・`AuthenticateAsync`・`GetNotesWithPagination` はインターフェース経由では一度も使われていないことを確認した。この3メソッドをインターフェースから削除し、`GetNotesWithRetry` のみを残した（各メソッドの実装自体は `MisskeyApiClient` クラスに残し、`PastNotes.Tests` の既存カバレッジも維持）。

`GetNotesWithRetry` 自体のリネーム（意図ベースの `GetNotes` への変更）は見送った。理由: 同メソッドは `PastNotes.Tests/MisskeyApiClientTests.cs` で具象クラス経由で60件以上のテストから直接呼び出されており、リネームの影響範囲が本項目の目的（呼び出し側に実装の選択肢を露出させないこと）に対して不釣り合いに大きい。インターフェースが1メソッドのみになったことで「どの取得方法を選ぶか」という選択自体が発生しなくなり、設計上の問題（呼び出し側が複数の実装戦略から選ぶ必要がある状態）は解消されたと判断した。

横展開確認: プロジェクト内に `IMisskeyApiClient` 以外のインターフェースが存在しないことを Grep で確認済み。66件・68件の全ユニットテストパス（動作不変）、`dotnet build` 警告0件。

---

### [x] DESIGN-6. `MisskeyApiClient` でキャッシュ機能を持つメソッドと持たないメソッドが混在している

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`

**問題**: `GetNotesAsync()` はキャッシュ（5分 TTL）を持つが、`GetNotesWithPagination()` と `GetNotesWithRetry()` はキャッシュなしで API を直接呼ぶ。同じ「ノート取得」という責務に対して動作が異なり、どのメソッドを呼ぶかでキャッシュの有無が変わることが利用者に不明瞭。

**対処**: 修正案の2案のうち「キャッシュ有無をメソッド名で明示的に区別する」を採用した。`GetNotesWithPagination`・`GetNotesWithRetry` は既に「With+特徴」という命名でキャッシュなしであることが（名前からは直接わからないが、少なくとも機能追加を明示する形で）一貫していたのに対し、`GetNotesAsync` だけがこの命名規則から外れておりキャッシュの有無が名前から読み取れなかった。`GetNotesAsync` を `GetNotesWithCache` にリネームし、3メソッドとも「With+特徴」で統一した。

キャッシュ機構自体の一元化（修正案のもう1案）は採用しなかった。理由: `PastNotes.Console` は CLI ツールであり実行のたびに新規プロセスが起動するため、プロセス内メモリキャッシュ（`_cache` フィールド）は同一プロセス内で同じ期間を複数回取得する場合にしか効果がなく、実際の `fetch` コマンドの利用パターン（1回の実行で1回だけ取得）では意味を持たない。加えて DESIGN-5 の対応により `GetNotesWithCache`（旧 `GetNotesAsync`）は現状どの呼び出し元からも使われていないことが判明しており、一元化のための設計変更に見合うだけの実利用がない。

`PastNotes.Tests/MisskeyApiClientTests.cs` の呼び出し箇所・テストメソッド名（計39箇所）を `GetNotesWithCache` に一括リネーム。68件全ユニットテストパス（動作不変）、`dotnet build` 警告0件。

---

### [ ] DESIGN-7. `Program.cs` に引数パース・DI・例外処理が全て集約されており肥大化している

**対象ファイル**: `PastNotes.Console/Program.cs`

**問題**: コマンドライン引数のパース、依存オブジェクトの生成、コマンドの実行、例外処理が `Main()` の 250 行に全て書かれており、コマンドが増えるほど比例して肥大化する。現状は4コマンドで管理可能だが、保守性が低い。

**修正案**: コマンドごとにパーサークラスを切り出すか、`System.CommandLine` 等のライブラリを導入してコマンド定義を分離する。

---

### [ ] DESIGN-8. 引数バリデーションエラーが stdout、例外エラーが stderr で不統一（DESIGN-3 の対処で可視化）

**対象ファイル**: `PastNotes.Console/Program.cs`

**問題**: DESIGN-3 で `fetch`・`search`・`view`・`view-html` の例外（`catch (Exception ex)`）を stderr に統一したが、`--token requires a value`・`Invalid start date format` 等、引数パース段階でのバリデーションエラー（`Program.cs` に15箇所、try/catch の外で直接 `Console.WriteLine` している）は stdout のまま残っている。同じ「ユーザーへのエラー通知」でも、検出タイミング（引数パース時 vs 実行時例外）によって出力先が異なる状態になった。

**具体的な失敗シナリオ**: `pastnotes search keyword --end 2>/dev/null` のように stderr を捨てるシェルスクリプトから呼び出すと、実行時例外（stderr）は抑制されるが、`--end` の値なしエラー（stdout）は抑制されずそのまま標準出力に混ざる。

**修正案**: 引数バリデーションエラーの15箇所も `Console.Error.WriteLine` に統一する。ただし該当箇所を検証している既存テスト（`SearchCommand_WhenInvalidEndDate_ReturnsOneAndPrintsError` 等、`Console.SetOut` で捕捉している一連のテスト）の更新が必要になるため、まとまった作業として別途対応する。

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

---

## RETRO: ふりかえり・プロセス上の問題

### RETRO-1. リリースチェックリスト作成時に「設計品質」「テスト品質」の観点が欠落していた

**経緯**:
- RELEASE_CHECKLIST.md の初版（Claude 作成）には「コード品質」「ドキュメント」「セキュリティ」「REL-1/2」「最終リリース作業」のみが含まれていた
- 「設計品質」「テスト品質」のセクションが抜けていた
- そのセクションをレビューして初めて以下の問題が発覚した：
  - BUG-17: `AuthenticateAsync` の "invalid-token" ハードコード残存
  - BUG-18: `GetNotesWithRetry` が実装・テスト済みだが本番フローで未使用
  - 統合テストが長期間実行されておらず、実行の仕組みが人間の記憶に依存していた

**根本原因**:
チェックリストを「何をするか（タスク）」の視点でのみ作成し、「何が正しいか（品質基準）」の視点が抜けていた。

**教訓**:
リリースチェックリストには最初から以下の観点を含めるべきだった：
- 設計品質: 死にコード・未使用の実装・テスト専用ロジックの本番混入がないか
- テスト品質: テストの網羅性・実行されていないテストカテゴリがないか（特に統合テスト）
