# Asterism

社内の各部署に散在するツールのダウンロード・アップデート・起動を一元管理する社内ツールポータル（Native Accessのようなもの）のMVPです。

- **サーバー**: 静的ファイル配信（manifest.json + ZIP/インストーラー配布）に加え、認証付きの管理API（ツール登録・編集・削除）を持つASP.NET Core
- **クライアント**: C# WPF (.NET 8) デスクトップアプリ（Electronは不使用）。一般利用者向けの一覧・インストール画面に加え、パスワードでロック解除する「管理者モード」でツール登録・編集ができる
- **共有**: `ToolEntry`等のモデルはサーバー・クライアントで共有クラスライブラリ（`Asterism.Shared`）として共通化

## 構成

```
Asterism/
├── shared/
│   └── Asterism.Shared/        サーバー・クライアント共有モデル (ToolEntry, ToolManifest, PackageType)
├── server/
│   ├── Asterism.Server/        ASP.NET Core（静的ファイル配信 + 管理API）
│   │   └── wwwroot/            manifest.json / icons / tools (配布物置き場)
│   └── SampleTools/            デモ用ダミーツールのソース（DB Converter, Sample Installer）
└── client/
    └── Asterism.Client/        WPFクライアント本体（一般画面 + 管理者モード）
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

### 管理者モード（ツールの登録・編集）

ツールの登録・編集は、クライアントの「管理者モード」から行います（別クライアントや管理用Webページは用意していません）。

1. サーバーの `appsettings.json`（本番）または `appsettings.Development.json`（開発、既定値 `dev-secret-key`）で管理者パスワードを設定します。
   ```json
   { "Asterism": { "AdminApiKey": "任意の管理者パスワード" } }
   ```
2. クライアントのヘッダーにある「管理者モード」ボタンをクリックし、上記のパスワードを入力してロックを解除します。
3. 「ツール管理」画面で「新規登録」「編集」「削除」が行えます。新規登録時はパッケージファイル（`packageType`が`Zip`なら`.zip`、`Installer`なら`.exe`/`.msi`）が必須、アイコン画像は任意です。
4. 保存すると、サーバー側でパッケージ/アイコンが `wwwroot/tools/` `wwwroot/icons/` に配置され、`manifest.json` に反映されます。一般利用者の画面では「更新を確認」を押すと新しいツールがすぐに一覧へ反映されます。

**注意**: 削除は `manifest.json` からエントリを取り除くのみで、サーバー上のパッケージ/アイコンファイルは残ります（誤操作時の復旧をしやすくするための挙動です。不要になったファイルは手動で削除してください）。また、`packageType: "Installer"` のツールはOS側の「アプリと機能」からの手動アンインストールが必要なため、管理者モードでの削除もクライアント側の導入記録を消すだけです。

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

配布物の静的ファイル（`manifest.json` / `icons/` / `tools/`）は社内Webサーバー（IIS・Nginxなど）の公開ディレクトリにそのまま配置できますが、ツール登録・編集用の管理API（`/api/admin/*`）はASP.NET Coreサーバー（`Asterism.Server`）自体を稼働させる必要があります。クライアントの `appsettings.json` の `ServerBaseUrl` を実際のサーバーURLに向けてください。

本番の `Asterism:AdminApiKey` はリポジトリにコミットせず、環境変数 `Asterism__AdminApiKey` などで上書きして運用してください。
