# Asterism

社内の各部署に散在するツールのダウンロード・アップデート・起動を一元管理する社内ツールポータル。

- **サーバー**: 静的ファイル配信（manifest.json + ZIP/インストーラー配布）に加え、認証付きの管理API（ツール登録・編集・削除）を持つASP.NET Core
- **クライアント**: C# WPF (.NET 8) デスクトップアプリ。一般利用者向けの一覧・インストール画面に加え、パスワードでロック解除する「管理者モード」でツール登録・編集ができる
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

同梱の `wwwroot/manifest.json` には7カテゴリ・20種類のデモツールが登録されています。

- **DB Converter**（Zip型） - インストールするとZIPがダウンロード・展開され、起動できるようになります。
- **Sample Installer App**（Installer型） - 既製ツール（Visual Studioなど）のようなインストーラー配布を模したデモです。インストールするとサイレント引数付きで実行され、`%LOCALAPPDATA%\Asterism\DemoInstalled\` 配下に自身をコピーします。
- その他18種のダミーツール（ファイルなし）— カード一覧・検索・カテゴリ絞り込みの動作確認用です。

一覧画面では検索・カテゴリ絞り込み・お気に入り登録・インストール/更新/起動/アンインストール・バックグラウンドでの更新確認（既定10分間隔、ヘッダーの「更新を確認」で即時実行も可）が行えます。

### 更新フローを試す

1. `server/Asterism.Server/wwwroot/manifest.json` の `tool-db-converter` の `version` と `downloadUrl` を書き換えて保存します（`server/Asterism.Server/wwwroot/tools/` には `db-converter-1.0.0.zip` と `db-converter-1.1.0.zip` の2バージョンを同梱済みです）。
2. クライアントの「更新を確認」を押すと更新が検知され、対象カードのボタンが「更新」に変わります。

### 管理者モード（ツールの登録・編集）

ツールの登録・編集は、クライアントの「管理者モード」から行います（別クライアントや管理用Webページは用意していません）。

1. サーバーの `appsettings.json`（本番）または `appsettings.Development.json`（開発、既定値 `dev-secret-key`）で管理者パスワードを設定します。
   ```json
   { "Asterism": { "AdminApiKey": "任意の管理者パスワード" } }
   ```
2. クライアントのメニューバー「ツール → 管理者モード」をクリックし、上記のパスワードを入力してロックを解除します。
3. 管理者画面で「新規登録」「編集」「削除」が行えます。新規登録時はパッケージファイル（`packageType`が`Zip`なら`.zip`、`Installer`なら`.exe`/`.msi`）が必須、アイコン画像は任意です。
4. 保存すると、サーバー側でパッケージ/アイコンが `wwwroot/tools/` `wwwroot/icons/` に配置され、`manifest.json` に反映されます。一般利用者の画面では「更新を確認」を押すと新しいツールがすぐに一覧へ反映されます。

**注意**: 削除ダイアログで「いいえ」を選ぶと `manifest.json` からエントリを取り除くのみで、サーバー上のパッケージ/アイコンファイルは残ります（誤操作時の復旧をしやすくするための挙動）。「はい」を選ぶとファイルも同時に削除します（二段階確認あり）。また、`packageType: "Installer"` のツールはOS側の「アプリと機能」からの手動アンインストールが必要なため、管理者モードでの削除もクライアント側の導入記録を消すだけです。

### クライアントのローカル状態

インストール先とインストール状態は以下に保存されます（管理者権限不要）。

- ツール本体: `%USERPROFILE%\Downloads\Asterism\<tool-id>\`（クライアントのインストール先変更ボタンで変更可）
- インストール状態: `%LOCALAPPDATA%\Asterism\installed.json`
- ユーザー設定（インストール先・お気に入り）: `%LOCALAPPDATA%\Asterism\user-settings.json`
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

| フィールド                | 型                       | 説明                                                                                              |
| ------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------- |
| `id`                      | string                   | 一意識別子。インストール先フォルダ名にも使用                                                      |
| `name` / `description`    | string                   | 表示名・説明文                                                                                    |
| `version`                 | string                   | `System.Version` でパース可能な形式（例: `1.0.0`）                                                |
| `category`                | string                   | サイドバーのカテゴリ絞り込みに使う主カテゴリ                                                      |
| `tags`                    | string[]                 | 検索対象の補助タグ（省略時は空配列）                                                              |
| `iconUrl` / `downloadUrl` | string                   | 絶対URL・相対パスどちらも可（相対時はサーバーのBaseAddressを基準に解決）                          |
| `packageType`             | `"Zip"` \| `"Installer"` | 配布形式。`Zip`は展開、`Installer`はサイレント実行                                                |
| `executablePath`          | string                   | `Zip`型: 展開後フォルダからの相対パス / `Installer`型: インストール後の絶対パス（環境変数展開可） |
| `installerArgs`           | string（任意）           | `Installer`型のみ。サイレントインストール引数                                                     |

`packageType: "Installer"` を使うと、社内製ツール以外の既製ツール（Visual Studioなど）のインストーラー配布にも対応できます。ただしアンインストールはOS側の「アプリと機能」からの手動操作が必要です（Asterism側では導入記録の削除のみ行います）。

## 本番運用

### 推奨構成

```
社内ネットワーク
├── サーバーPC（1台）
│   └── Asterism.Server.exe を常時起動（ポート5000）
│
└── クライアントPC（各利用者）
    └── Asterism.Client.exe を配布・起動
```

### サーバーのセットアップ

#### 1. 単体EXEをビルドする

```powershell
cd server\Asterism.Server
dotnet publish -c Release -r win-x64 --self-contained -o publish\
```

`publish\` フォルダ内の `Asterism.Server.exe` と `wwwroot/` をサーバーPCに配置します。

#### 2. 管理者パスワードを設定する

環境変数で設定するのが安全です（appsettings.json にコミットしない）。

```powershell
$env:Asterism__AdminApiKey = "強いパスワード"
.\Asterism.Server.exe
```

または `appsettings.json` に直接記載する場合:

```json
{ "Asterism": { "AdminApiKey": "強いパスワード" } }
```

#### 3. Windowsサービスとして常時起動する（推奨）

NSSM（Non-Sucking Service Manager）を使うのが簡単です。

```powershell
nssm install Asterism "C:\Asterism\Asterism.Server.exe"
nssm set Asterism AppDirectory "C:\Asterism"
nssm set Asterism AppEnvironmentExtra "Asterism__AdminApiKey=強いパスワード"
nssm start Asterism
```

タスクスケジューラで「ログオン時に起動」にする方法でも構いません。

#### 4. ファイアウォールを設定する

```powershell
netsh advfirewall firewall add rule name="Asterism" dir=in action=allow protocol=TCP localport=5000
```

### クライアントの配布

#### 1. 単体EXEをビルドする

```powershell
cd client\Asterism.Client
dotnet publish -c Release -r win-x64 --self-contained -o publish\
```

#### 2. appsettings.json をサーバーに向ける

配布前に `publish\appsettings.json` の `ServerBaseUrl` を実際のサーバーIPまたはホスト名に書き換えます。

```json
{
  "Asterism": {
    "ServerBaseUrl": "http://192.168.1.100:5000/",
    "PollingIntervalMinutes": 10
  }
}
```

#### 3. 各PCに配布する

`publish\` フォルダ（EXEと appsettings.json）を各クライアントPCに展開するだけで動作します。.NETランタイムのインストールは不要です。

### 注意事項

| 項目                       | 内容                                                                                                                                                                                                                              |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **通信**                   | 現状HTTPのみ。社内LANのみの利用であればそのままでOK。外部公開する場合はIIS/nginxでリバースプロキシ＋HTTPS化を推奨                                                                                                                 |
| **管理者パスワード**       | 管理者のみが知る運用とし、クライアントのappsettings.jsonには記載しない                                                                                                                                                            |
| **wwwrootのバックアップ**  | `wwwroot/` 以下（manifest.json・tools/・icons/）が全資産。定期的にバックアップしてください                                                                                                                                        |
| **クライアントの自己更新** | ツールID `tool-asterism-client`（Zip型）をAsterismに登録し、クライアントEXE入りZIPを配布すると、管理者がバージョンを上げたときに一般ユーザーへ更新通知が届き、カードの「更新」ボタンでダウンロード→再起動の流れで自動適用できます |
| **ポート変更**             | `appsettings.json` の `Kestrel:Endpoints:Http:Url` または起動引数 `--urls` で変更できます                                                                                                                                         |
