# 改善計画 (Improvement Plan)

## 概要

プロジェクトレビューで識別された問題点をTDD（テスト駆動開発）で改善する計画。

## 優先度: 高

### 1. NoteRepositoryの非同期一貫性を改善する

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

### 2. HttpClientのライフサイクル管理を改善する

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

### 3. MisskeyApiClientのTODOコメントを整理する

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

### 4. キャッシュの有効期限管理を実装する

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

### 5. エラーハンドリングを改善する

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

### 6. IMisskeyApiClientインターフェースを拡張する

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

### 7. コンソールアプリの非同期呼び出しを改善する

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

## 実装順序

1. **優先度高**: NoteRepositoryの非同期一貫性
2. **優先度高**: HttpClientのライフサイクル管理
3. **優先度中**: TODOコメントの整理
4. **優先度低**: 残りの項目（順次実施）

## TDDルール

各改善項目に対して以下の手順を守る：

1. **テストファースト**: 失敗するテストを先に書く
2. **小さなステップ**: 一度に小さな機能単位で実装
3. **リファクタリング**: テストが通った後、コードの品質を改善
4. **継続的統合**: 各ステップ後にテストを実行

## 進捗管理

- [x] 1. NoteRepositoryの非同期一貫性を改善する
- [x] 2. HttpClientのライフサイクル管理を改善する
- [x] 3. MisskeyApiClientのTODOコメントを整理する
- [x] 4. キャッシュの有効期限管理を実装する
- [x] 5. エラーハンドリングを改善する
- [x] 6. IMisskeyApiClientインターフェースを拡張する
- [x] 7. コンソールアプリの非同期呼び出しを改善する
