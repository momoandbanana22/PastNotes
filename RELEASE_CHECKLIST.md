# リリース前チェックリスト

## 設計品質

- [ ] 設計に問題がないこと

## テスト品質

- [ ] テスト設計に過不足がないこと。

## コード品質

- [ ] 全ユニットテストが通過している（`dotnet test --filter "Category=Unit"`）
- [ ] ビルドエラー・警告がゼロ（`dotnet build`）
- [ ] APIトークンがコードにハードコードされていない

## ドキュメント

- [ ] README.md が完成している（コマンド・オプション・認証方法が網羅されている）
- [ ] DEVELOPMENT.md が完成している（認証設定・ビルド手順・テスト手順）
- [ ] IMPROVEMENT_PLAN.md の全項目（BUG/TST/DOC/FEAT/REFACTOR）が `[X]`

## セキュリティ・除外設定

- [ ] `.env` が `.gitignore` に含まれている
- [ ] `publish/` が `.gitignore` に含まれている

## REL-1: シングルファイル .exe のビルドと配布

- [ ] `dotnet publish` でシングルファイル `.exe` が生成できる
- [ ] 生成した `.exe` で全コマンドが正常動作する（手動確認）
  - [ ] `fetch --days 30`
  - [ ] `fetch --start ... --end ...`
  - [ ] `fetch --append`
  - [ ] `search <keyword>`
  - [ ] `view`
  - [ ] `view-html --open`
- [ ] README を `.exe` ベースの手順に更新する
- [ ] DEVELOPMENT.md に `dotnet publish` 手順を追記する

## REL-2: GitHub Actions によるリリース自動化

- [ ] `.github/workflows/release.yml` を作成する
- [ ] `v*` タグをプッシュすると自動で `.exe` がビルドされ GitHub Releases にアップロードされる

## 最終リリース作業

- [ ] バージョン番号を決める（例: `v1.0.0`）
- [ ] GitHub Releases のリリースノートを書く
- [ ] タグをプッシュしてリリースが正常に作成されることを確認する
