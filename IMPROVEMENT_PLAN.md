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

### [ ] BUG-35. `SearchCommand` に到達不能な `notes == null` チェックが残存（BUG-31 適用漏れ）

**対象ファイル**: `PastNotes.Console/Commands/SearchCommand.cs`（25行目、54行目）

**問題**: BUG-31 で `ViewCommand` の `notes == null ||` を削除したのと同じ理由で、`SearchCommand.Execute()`（25行目）と `ExecuteAsync()`（54行目）にも同チェックが残っている。`NoteRepository.LoadFromFileAsync` はファイル不在時に `Enumerable.Empty<Note>()`、JSON 破損時に `InvalidDataException` を返すため null にはならない。死コードがコードの一貫性を損なう。

**修正案**: `if (notes == null || !notes.Any())` を `if (!notes.Any())` に変更する（2箇所）。

---

### [ ] BUG-36. `ViewHtmlCommand` に到達不能な `notes == null` チェックが残存（同上）

**対象ファイル**: `PastNotes.Console/Commands/ViewHtmlCommand.cs`（24行目）

**問題**: BUG-35 と同一の原因。`ViewHtmlCommand.Execute()` 24行目の `if (notes == null || !notes.Any())` で `notes == null` が到達不能。`ViewCommand` は BUG-31 修正済みだが `ViewHtmlCommand` には適用されていない。

**修正案**: `notes == null ||` を削除する（1箇所）。

---

### [ ] BUG-37. `MisskeyApiClient` がライブラリ内で直接 `System.Console.WriteLine` を呼び出している

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（288行目）

**問題**: `GetNotesWithPaginationFromApiAsync` のページネーションループ内で `System.Console.WriteLine($"  取得中... {allNotes.Count} 件")` を直接呼び出している。`PastNotes` はライブラリプロジェクトであり、コンソール出力を直接持つべきではない（Console に依存するとコンソールアプリ以外での再利用が困難、テスト出力が汚れる）。`FetchCommandTests` 等で `Console.SetOut` を差し替えてもこのメッセージは通常出力として混入し続ける。

**修正案**: `IProgress<string>` やコールバック Action などを引数として受け取る形に変更し、進捗出力の責任を呼び出し元（`FetchCommand`）に委ねる。または `System.Console.WriteLine` を条件付きコンパイルまたは設定フラグで無効化できるようにする。

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

### [ ] TST-19. `ConsoleAppTests.FetchCommand_WhenApiTokenMissing` の try ブロック内に `SetOut` 復元が重複（TST-17 の適用漏れ）

**対象ファイル**: `PastNotes.Console.Tests/ConsoleAppTests.cs`（186行目）

**問題**: TST-17 で `FetchCommandTests`・`ViewCommandTests` の `Console.SetOut/SetError` 復元を `finally` ブロックへ移動したが、`ConsoleAppTests` の `FetchCommand_WhenApiTokenMissing_ReturnsOneAndPrintsError` テストには同じパターンが残っている。try ブロック内 186行目と finally ブロックで `System.Console.SetOut(originalOutput)` を二重に呼んでいる。同ファイルの他のテスト（`SearchCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError` 等）は finally のみで復元しており不一致。

**修正案**: try ブロック内の `System.Console.SetOut(originalOutput)` 186行目を削除する。

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
