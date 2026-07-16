# Asterism

社内の各部署に散在するツールのダウンロード・アップデート・起動を一元管理する社内ツールポータル（Native Accessのようなもの）のMVPです。

- **サーバー**: 動的APIを持たない静的ファイルサーバー（manifest.json + ZIP/インストーラー配布）
- **クライアント**: C# WPF (.NET 8) デスクトップアプリ（Electronは不使用）

## 構成

```
Asterism/
├── server/
│   ├── Asterism.Server/        ASP.NET Core 最小構成の静的ファイルサーバー
│   │   └── wwwroot/            manifest.json / icons / tools (配布物置き場)
│   └── SampleTools/            デモ用ダミーツールのソース（DB Converter, Sample Installer）
└── client/
    └── Asterism.Client/        WPFクライアント本体
```

## 必要環境

- .NET 8 SDK
- Windows（クライアントはWPFのためWindows専用）

## 実行方法

### 1. サーバーを起動する

```powershell
cd server\Asterism.Server
dotnet run
```

`http://localhost:5000` で起動します。`http://localhost:5000/manifest.json` にアクセスしてツール一覧が返ってくることを確認してください。

### 2. クライアントを起動する

別のターミナルで:

```powershell
cd client\Asterism.Client
dotnet run
```

起動すると `client/Asterism.Client/appsettings.json` の `Asterism:ServerBaseUrl`（既定 `http://localhost:5000/`）からツール一覧を取得し、一覧表示します。

## 使い方（デモ）

同梱の `wwwroot/manifest.json` には2種類のデモツールが登録されています。

- **DB Converter**（Zip型） - インストールするとZIPがダウンロード・展開され、起動できるようになります。
- **Sample Installer App**（Installer型） - 既製ツール（Visual Studioなど）のようなインストーラー配布を模したデモです。インストールするとサイレント引数付きで実行され、`%LOCALAPPDATA%\Asterism\DemoInstalled\` 配下に自身をコピーします。

一覧画面では検索・カテゴリ絞り込み・インストール/更新/起動/アンインストール・バックグラウンドでの更新確認（既定10分間隔、ヘッダーの「更新を確認」で即時実行も可）が行えます。

### 更新フローを試す

1. `server/Asterism.Server/wwwroot/manifest.json` の `tool-db-converter` の `version` と `downloadUrl` を書き換えて保存します（`server/Asterism.Server/wwwroot/tools/` には `db-converter-1.0.0.zip` と `db-converter-1.1.0.zip` の2バージョンを同梱済みです）。
2. クライアントの「更新を確認」を押すと更新が検知され、対象カードのボタンが「更新」に変わります。

### クライアントのローカル状態

インストール先とインストール状態は以下に保存されます（管理者権限不要）。

- ツール本体: `%LOCALAPPDATA%\Asterism\Tools\<tool-id>\`
- インストール状態: `%LOCALAPPDATA%\Asterism\installed.json`
- manifestのオフラインキャッシュ: `%LOCALAPPDATA%\Asterism\manifest.cache.json`

## manifest.json の仕様

```json
{
  "tools": [
    {
      "id": "tool-db-converter",
      "name": "DB Converter",
      "version": "1.0.0",
      "description": "社内データベースのフォーマット変換ツール",
      "category": "データ変換",
      "tags": ["db", "csv", "社内製"],
      "iconUrl": "icons/db-converter.png",
      "downloadUrl": "tools/db-converter-1.0.0.zip",
      "packageType": "Zip",
      "executablePath": "DbConverter.exe"
    }
  ]
}
```

| フィールド | 型 | 説明 |
|---|---|---|
| `id` | string | 一意識別子。インストール先フォルダ名にも使用 |
| `name` / `description` | string | 表示名・説明文 |
| `version` | string | `System.Version` でパース可能な形式（例: `1.0.0`） |
| `category` | string | サイドバーのカテゴリ絞り込みに使う主カテゴリ |
| `tags` | string[] | 検索対象の補助タグ（省略時は空配列） |
| `iconUrl` / `downloadUrl` | string | 絶対URL・相対パスどちらも可（相対時はサーバーのBaseAddressを基準に解決） |
| `packageType` | `"Zip"` \| `"Installer"` | 配布形式。`Zip`は展開、`Installer`はサイレント実行 |
| `executablePath` | string | `Zip`型: 展開後フォルダからの相対パス / `Installer`型: インストール後の絶対パス（環境変数展開可） |
| `installerArgs` | string（任意） | `Installer`型のみ。サイレントインストール引数 |

`packageType: "Installer"` を使うと、社内製ツール以外の既製ツール（Visual Studioなど）のインストーラー配布にも対応できます。ただしアンインストールはOS側の「アプリと機能」からの手動操作が必要です（Asterism側では導入記録の削除のみ行います）。

## 本番環境への移行

本番では動的サーバーは不要です。`server/Asterism.Server/wwwroot/` の中身（`manifest.json` / `icons/` / `tools/`）一式を社内Webサーバー（IIS・Nginxなど）の公開ディレクトリにそのまま配置し、クライアントの `appsettings.json` の `ServerBaseUrl` をそのURLに向けるだけで移行できます。
