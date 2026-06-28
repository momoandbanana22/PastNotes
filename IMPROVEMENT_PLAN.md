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

## 優先度: 高

### [x] 1. NoteRepositoryの非同期一貫性を改善する

**問題**: メソッド名に`Async`が付いているのに同期実装

**現在の実装**:
```csharp
public void SaveToFileAsync(IEnumerable<Note> notes, string filePath)
{
    var json = JsonSerializer.Serialize(notes);
    File.WriteAllText(filePath, json);  // 同期I/O
}

public IEnumerable<Note> LoadFromFileAsync(string filePath)
{
    // 同期I/O
}
```

**TDDアプローチ**:
1. 非同期メソッドのテストを書く（失敗することを確認）
2. メソッドを真の非同期実装に変更
3. テストが通ることを確認
4. 呼び出し元（FetchCommand, SearchCommand, ViewCommand）を非同期に対応

**期待される改善**:
- I/O操作が非同期になり、スレッドのブロックが解消
- スケーラビリティの向上

---

### [x] 2. HttpClientのライフサイクル管理を改善する

**問題**: `Program.cs`で`new HttpClient()`を使用しており、ソケット枯渇のリスク

**現在の実装**:
```csharp
var httpClient = new HttpClient();  // 毎回新しいインスタンス
```

**TDDアプローチ**:
1. HttpClientファクトリのテストを書く
2. `IHttpClientFactory`またはシングルトンパターンを実装
3. Program.csを修正
4. 統合テストで動作を確認

**期待される改善**:
- ソケット枯渇のリスク解消
- パフォーマンスの向上

---

## 優先度: 中

### [x] 3. MisskeyApiClientのTODOコメントを整理する

**問題**: 実装が不完全なTODOコメントが残っている

**現在の実装**:
```csharp
// TODO: 実際のAPI認証を実装
// TODO: 実際のAPI呼び出しを実装
// TODO: 実際のページネーション処理を実装
```

**TDDアプローチ**:
1. TODOコメントの内容を確認
2. 実際の実装状態をテストで検証
3. 完了している機能のTODOコメントを削除
4. 未実装の場合はテストを書いて実装

**期待される改善**:
- コードの可読性向上
- 実装状況の明確化

---

## 優先度: 低

### [x] 4. キャッシュの有効期限管理を実装する

**問題**: キャッシュに有効期限チェックがない

**現在の実装**:
```csharp
if (_cache.ContainsKey(cacheKey))
{
    return _cache[cacheKey];  // 有効期限チェックなし
}
```

**TDDアプローチ**:
1. キャッシュ有効期限のテストを書く
2. キャッシュエントリにタイムスタンプを追加
3. 有効期限チェックを実装
4. テストが通ることを確認

**期待される改善**:
- キャッシュの正確性向上
- 古いデータの自動破棄

---

### [x] 5. エラーハンドリングを改善する

**問題**: `GetNotesFromApiAsync`でHTTPエラー時の詳細なエラーハンドリングが不足

**現在の実装**:
```csharp
response.EnsureSuccessStatusCode();  // 例外の詳細が失われる
```

**TDDアプローチ**:
1. 各HTTPエラーコード（404, 429, 500等）のテストを書く
2. `HandleErrorResponse`メソッドを使用するように修正
3. 適切な例外がスローされることを確認

**期待される改善**:
- エラーの原因特定が容易に
- ユーザー体験の向上

---

### [x] 6. IMisskeyApiClientインターフェースを拡張する

**問題**: インターフェースが`GetNotesAsync`のみ定義

**現在の実装**:
```csharp
public interface IMisskeyApiClient
{
    Task<IEnumerable<Note>> GetNotesAsync(DateTime startDate, DateTime endDate);
}
```

**TDDアプローチ**:
1. インターフェースのテストを書く（モック使用）
2. 使用されているメソッドをインターフェースに追加
3. 実装クラスを修正
4. 既存テストが通ることを確認

**期待される改善**:
- 依存性注入の柔軟性向上
- テスト容易性の向上

---

### [x] 7. コンソールアプリの非同期呼び出しを改善する

**問題**: 非同期メソッドを`.GetAwaiter().GetResult()`で同期的に呼び出し

**現在の実装**:
```csharp
var result = fetchCommand.ExecuteAsync(days).GetAwaiter().GetResult();
```

**TDDアプローチ**:
1. 非同期Mainのテストを書く
2. Mainメソッドを`async Task`に変更
3. コマンド実装を非同期に対応
4. テストが通ることを確認

**期待される改善**:
- 非同期処理の一貫性
- デッドロックリスクの解消

---

## コードレビュー指摘事項（2026-06-28）

fetchコマンドへの日付範囲指定機能追加（`--start`/`--end`）のレビューで発見されたバグ。

### 優先度: 高（バグ）

#### [x] 8. ページネーション早期終了バグ

**対象ファイル**: `PastNotes/MisskeyApiClient.cs`（`GetNotesWithPaginationFromApiAsync` 内）

**問題**: `if (!filteredNotes.Any()) hasMoreNotes = false;` という終了条件が不正。Misskey は新着順にノートを返すため、対象期間より新しいノートしか含まないページで誤って終了してしまう。

**具体的な失敗シナリオ**:
- ユーザーが 2026 年のノートを持ち、`--start 2024-01-01 --end 2024-01-31` を指定した場合
- 最初のページが 2026 年のノート 100 件 → `filteredNotes` が空 → ループ終了
- 2024 年のノートが存在しても 0 件が返される

**正しい終了条件**: 「現在ページの最古ノートが `startDate` より前になった場合に終了」とすべき。

```csharp
// 現在（バグあり）
if (!filteredNotes.Any())
{
    hasMoreNotes = false;
}

// 修正案
var oldestNoteOnPage = notes.Last();
if (oldestNoteOnPage.CreatedAt < startDate)
{
    hasMoreNotes = false;
}
```

---

#### [ ] 9. モックの `_callCount` バグによりテストが常に空リストを返す

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`（`MockHttpMessageHandler`）

**問題**: 非ページネーションモードで `_callCount > 1` を空配列の返却条件にしているが、`_callCount` は `/api/i`（認証）リクエストでも加算される。そのため `/api/users/notes` への最初のリクエスト到達時には既に `_callCount == 2` となり、常に `[]` が返される。

**具体的な失敗シナリオ**:
1. `/api/i` リクエスト → `_callCount = 1`
2. `/api/users/notes` リクエスト → `_callCount = 2` → `if (_callCount > 1)` が true → `[]` 返却
3. ノート件数を検証するテストが誤った前提で通過してしまう

**修正案**: `/api/users/notes` 呼び出しのみカウントする `_notesCallCount`（既に追加済み）を非ページネーションモードでも使用する。

---

### 優先度: 中（バグ）

#### [ ] 10. `--days` と `--start/--end` のタイムゾーン処理の不整合

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`

**問題**:
- `ExecuteAsync(int days)` は `DateTime.Now`（マシンローカル時刻）をそのまま API に渡す
- `ExecuteAsync(DateTime, DateTime)` は入力を JST として扱い 9 時間引いて UTC に変換する

UTC 環境（CI や Linux サーバーなど）では同じ期間を指定しても取得されるノートが異なる。

---

#### [ ] 11. `convertedEndDate` への +1 秒が stale なロジック

**対象ファイル**: `PastNotes.Console/Commands/FetchCommand.cs`（約 47 行目）

**問題**: `endDate.AddHours(-9).AddSeconds(1)` の +1 秒は、API の `untilDate` パラメータを inclusive にするための補正だった。しかしそのパラメータは削除済みであり、現在はクライアント側の `note.CreatedAt <= endDate` フィルタに余分な 1 秒が混入している。

ユーザーが `--end "2024-01-31 23:59:59"` と指定した場合、`2024-02-01 00:00:00 JST` 丁度のノートがフィルタを通過してしまう。

---

### 優先度: 低

#### [ ] 12. テストモックの `.Result` によるデッドロックリスク

**対象ファイル**: `PastNotes.Tests/MisskeyApiClientTests.cs`（約 36 行目）

**問題**: `request.Content.ReadAsStringAsync().Result` が非同期コンテキスト内でブロッキング待機を行っており、シングルスレッドの同期コンテキストではデッドロックが発生しうる。

**修正案**: `SendAsync` を `async Task<HttpResponseMessage>` に変更し `await request.Content.ReadAsStringAsync()` を使用する。

---

#### [ ] 13. DEVELOPMENT.md に削除済み `--jst` フラグの使用例が残っている

**対象ファイル**: `DEVELOPMENT.md`（14 行目付近）

**問題**: `fetch --start 2024-01-01 --end 2024-01-31 --jst` という例が残っているが、`--jst` フラグは削除済み。実行すると `--jst` は無視されて処理は続行されるが、JST 変換が行われると誤解させる。
