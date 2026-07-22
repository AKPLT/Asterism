# Asterism

社内の各部署に散在するツールのダウンロード・アップデート・起動を一元管理する社内ツールポータル。

- **サーバー**: 静的ファイル配信（manifest.json + ZIP配布）に加え、認証付きの管理API（ツール登録・編集・削除）を持つASP.NET Core
- **クライアント（Asterism.Client）**: C# WPF (.NET 8) デスクトップアプリ。一般利用者向けの一覧・インストール画面のみを持つ
- **管理ツール（Asterism.Admin）**: クライアントとは別の単体WPFアプリ。ツールの登録・編集・削除を行う。管理者のみに配布する想定で、一般利用者向けクライアントには管理機能を一切含まない
- **共有**: `ToolEntry`等のモデルはサーバー・クライアント・管理ツールで共有クラスライブラリ（`Asterism.Shared`）として共通化

## 構成

```
Asterism/
├── shared/
│   └── Asterism.Shared/        サーバー・クライアント・管理ツール共有モデル (ToolEntry, ToolManifest, PackageType)
├── server/
│   ├── Asterism.Server/        ASP.NET Core（静的ファイル配信 + 管理API）
│   │   └── wwwroot/            manifest.json / icons / tools (配布物置き場)
│   └── SampleTools/            デモ用ダミーツールのソース（DB Converter）
└── client/
    ├── Asterism.Client/        一般利用者向けWPFクライアント（一覧・インストール画面のみ）
    └── Asterism.Admin/         管理者向けWPFアプリ（ツールの登録・編集・削除、管理者画面）
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

### Visual Studioで実行・ビルドする

`Asterism.sln` をVisual Studio 2022（.NET 8 SDKワークロード）で開くだけで、CLIと同様にビルド・実行できます。

- **ビルド**: ソリューションエクスプローラーで「ソリューションのビルド」（既定ショートカット `Ctrl+Shift+B`）。
- **実行**: 同梱の `Asterism.sln.slnLaunch` により、F5キーで **Asterism.Server と Asterism.Client が同時に起動**するよう複数スタートアッププロジェクトが設定済みです。個別に1つだけ実行したい場合はソリューションを右クリック→「スタートアッププロジェクトの設定」から変更してください。
- 管理者画面（`Asterism.Admin`）は普段の開発ループには含めていません。動作確認したい場合は `Asterism.Admin` を右クリック→「スタートアッププロジェクトに設定」してF5するか、「スタートアッププロジェクトの設定」で一時的に複数スタートアップに追加してください。

## 使い方（デモ）

同梱の `wwwroot/manifest.json` には7カテゴリ・19種類のデモツールが登録されています。

- **DB Converter** - インストールするとZIPがダウンロード・展開され、起動できるようになります。
- その他18種のダミーツール（実体は同じZIPを使い回し）— カード一覧・検索・カテゴリ絞り込みの動作確認用です。

一覧画面では検索・カテゴリ絞り込み・お気に入り登録・インストール/更新/起動/アンインストール・バックグラウンドでの更新確認（既定10分間隔、ヘッダーの「更新を確認」で即時実行も可）が行えます。

### 更新フローを試す

1. `server/Asterism.Server/wwwroot/manifest.json` の `tool-db-converter` の `version` と `downloadUrl` を書き換えて保存します（`server/Asterism.Server/wwwroot/tools/` には `db-converter-1.0.0.zip` と `db-converter-1.1.0.zip` の2バージョンを同梱済みです）。
2. クライアントの「更新を確認」を押すと更新が検知され、対象カードのボタンが「更新」に変わります。

### 管理者画面（ツールの登録・編集）

ツールの登録・編集は、`Asterism.Admin`（管理者専用の別アプリ）から行います。一般利用者向けクライアント（`Asterism.Client`）には管理機能は一切含まれません。

1. サーバーの `appsettings.json`（本番）または `appsettings.Development.json`（開発、既定値 `admin`）で管理者パスワードを設定します。
   ```json
   { "Asterism": { "AdminApiKey": "任意の管理者パスワード" } }
   ```
2. `client/Asterism.Admin/appsettings.json` の `ServerBaseUrl` と `AdminApiKey`（サーバー側と同じ値）を設定します。
   ```json
   { "Asterism": { "ServerBaseUrl": "http://localhost:5000/", "AdminApiKey": "任意の管理者パスワード" } }
   ```
3. `Asterism.Admin` を起動すると、この設定値でサーバーに自動認証し、そのまま管理者画面（ツール一覧）が開きます。入力画面によるパスワード入力は行いません（配布そのものを管理者に限定する運用のため）。キーが一致しない場合、管理APIは401を返し、画面にはエラーメッセージが表示されます。
4. 管理者画面で「新規登録」「編集」「削除」が行えます。新規登録時はパッケージファイル（`.zip`）が必須です。アイコンは手動選択ではなく、パッケージ内の`executablePath`が指すexeから自動抽出されます（抽出できない場合はプレースホルダー表示）。
5. 保存すると、サーバー側でパッケージ/アイコンが `wwwroot/tools/` `wwwroot/icons/` に配置され、`manifest.json` に反映されます。一般利用者の画面では「更新を確認」を押すと新しいツールがすぐに一覧へ反映されます。バージョンを変更する場合は新しいパッケージファイルの選択が必須です（ファイルなしでのバージョン変更はエラーになります）。

**注意**: 削除ダイアログで「いいえ」を選ぶと `manifest.json` からエントリを取り除くのみで、サーバー上のパッケージ/アイコンファイルは残ります（誤操作時の復旧をしやすくするための挙動）。「はい」を選ぶと同じIDの全バージョンのパッケージ/アイコンファイルも同時に削除します（二段階確認あり）。

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

| フィールド                | 型       | 説明                                                                     |
| ------------------------- | -------- | ------------------------------------------------------------------------ |
| `id`                      | string   | 一意識別子。インストール先フォルダ名にも使用                             |
| `name` / `description`    | string   | 表示名・説明文                                                          |
| `version`                 | string   | 任意の文字列。前回インストール時と完全一致しない場合に更新ありと判定される |
| `category`                | string   | サイドバーのカテゴリ絞り込みに使う主カテゴリ                             |
| `tags`                    | string[] | 検索対象の補助タグ（省略時は空配列）                                     |
| `iconUrl` / `downloadUrl` | string   | 絶対URL・相対パスどちらも可（相対時はサーバーのBaseAddressを基準に解決） |
| `packageType`             | `"Zip"`  | 配布形式。zipを展開してインストールする                                 |
| `executablePath`          | string   | 展開後フォルダからの相対パス（zip直下がフォルダ1つだけの場合は剥がされた後の相対パス） |

## 本番運用

### 推奨構成

```
社内ネットワーク
├── サーバーPC（1台）
│   └── Asterism.Server.exe を常時起動（ポート5000）
│
├── クライアントPC（各利用者）
│   └── Asterism.Client.exe を配布・起動
│
└── 管理者PC（管理者のみ）
    └── Asterism.Admin.exe を配布・起動（appsettings.json にServerBaseUrl・AdminApiKeyを設定）
```

### サーバーのセットアップ

#### 1. 単体EXEをビルドする

```powershell
cd server\Asterism.Server
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\
```

`-p:PublishSingleFile=true` を付けないと `Asterism.Server.exe` が実行に必要な大量のDLLに依存する構成になり、`exe`単体をコピーしても起動できません（`Asterism.Server.dll` が見つからない、というエラーになります）。上記コマンドなら `publish\` フォルダの中身（`Asterism.Server.exe` / `appsettings.json` / `wwwroot/`）は必要最小限のみになり、そのフォルダをまるごとサーバーPCにコピーするだけで動作します。

以前に `bin\` `obj\` `publish\` が残った状態で再度publishすると、中間キャッシュの影響で `publish\` の中に不要なフォルダが入れ子で残ることがあります。気になる場合は `bin\` `obj\` `publish\` を削除してから publish し直してください。

**Visual Studioで発行する場合**: ソリューションエクスプローラーで `Asterism.Server` プロジェクトを右クリック→「発行」を選択すると、同梱の発行プロファイル（`Properties\PublishProfiles\FolderProfile.pubxml`）が自動的に読み込まれ、上記CLIコマンドと同じ設定（Release / win-x64 / self-contained / シングルファイル）で `publish\` フォルダ発行 が行えます。あとは「発行」ボタンを押すだけです。

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

この値は `Asterism.Admin`（管理者アプリ）の appsettings.json に設定する `AdminApiKey` と一致させます。一致しない限り管理APIへのリクエストは401で拒否されるため、`Asterism.Admin` を配布していない第三者がAPIを直接叩いてもツールの登録・編集・削除はできません。

#### 3. 他PCからアクセスできるようにする（重要）

何も設定しないと、Kestrelは既定で `http://localhost:5000` のみをリッスンします。この状態だとサーバーPC自身からは動作確認できてしまいますが、**他のPC（クライアント）からは一切繋がりません**（クライアント側は「サーバーに繋がらずローカルキャッシュにフォールバックする」という分かりにくい形で症状が出ます）。

`appsettings.json` に `Urls` を追記し、`0.0.0.0`（全ネットワークインターフェース）で待ち受けるようにしてください。起動のたびに `--urls` 引数を付け忘れる心配がなく、サービス化しても確実に反映されます。

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "Asterism": { "AdminApiKey": "強いパスワード" },
  "Urls": "http://0.0.0.0:5000"
}
```

起動ログに `Now listening on: http://0.0.0.0:5000` と出ていればOKです。

#### 4. Windowsサービスとして常時起動する（推奨）

NSSM（Non-Sucking Service Manager）を使うのが簡単です。

```powershell
nssm install Asterism "C:\Asterism\Asterism.Server.exe"
nssm set Asterism AppDirectory "C:\Asterism"
nssm set Asterism AppEnvironmentExtra "Asterism__AdminApiKey=強いパスワード"
nssm start Asterism
```

タスクスケジューラで「ログオン時に起動」にする方法でも構いません。

**サービスを削除する場合**:

```powershell
nssm stop Asterism
nssm remove Asterism confirm
```

`confirm` を付けないと削除の確認プロンプトが表示されます。サービス登録を消してもインストール先の `C:\Asterism` フォルダ（EXE・appsettings.json・wwwroot）は残るため、アンインストールとして完全に片付けたい場合はフォルダも別途削除してください。

#### 5. ファイアウォールを設定する

```powershell
netsh advfirewall firewall add rule name="Asterism" dir=in action=allow protocol=TCP localport=5000
```

上記の `Urls` 設定とファイアウォール開放の両方が揃って初めて他PCから疎通できます。片方だけでは繋がらないので、クライアントから接続できない場合はまずこの2点を疑ってください。

### クライアントの配布

#### 1. 単体EXEをビルドする

```powershell
cd client\Asterism.Client
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\
```

**Visual Studioで発行する場合**: `Asterism.Client` プロジェクトを右クリック→「発行」で、同梱の発行プロファイル（`Properties\PublishProfiles\FolderProfile.pubxml`）が読み込まれ、同じ設定で `publish\` フォルダ発行が行えます。

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

この手順を省略しても、クライアント起動後にメニュー「ツール → サーバー設定...」からサーバーURLを変更できます（設定は `%LOCALAPPDATA%\Asterism\user-settings.json` に保存され、アプリ再起動不要で即座に反映されます）。サーバーの移設・IP変更時にも、配布物を作り直さずこの画面から追従できます。

#### 3. 各PCに配布する

`publish\` フォルダ（EXEと appsettings.json）を各クライアントPCに展開するだけで動作します。.NETランタイムのインストールは不要です。

### 管理者ツール（Asterism.Admin）の配布

#### 1. 単体EXEをビルドする

```powershell
cd client\Asterism.Admin
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\
```

**Visual Studioで発行する場合**: `Asterism.Admin` プロジェクトを右クリック→「発行」で、同梱の発行プロファイル（`Properties\PublishProfiles\FolderProfile.pubxml`）が読み込まれ、同じ設定で `publish\` フォルダ発行が行えます。

#### 2. appsettings.json を設定する

配布前に `publish\appsettings.json` の `ServerBaseUrl` と `AdminApiKey`（サーバー側の値と一致させる）を書き換えます。

```json
{
  "Asterism": {
    "ServerBaseUrl": "http://192.168.1.100:5000/",
    "AdminApiKey": "強いパスワード"
  }
}
```

`Asterism.Admin` は起動時にこの値で自動認証し、入力画面なしでそのまま管理者画面を開きます。**そのため `publish\` フォルダは管理者本人にのみ配布してください**（クライアントと違い、appsettings.json自体に平文の管理者パスワードが入っています）。

#### 3. 管理者に配布する

`publish\` フォルダを管理者のPCに展開するだけで動作します。常時起動しておく必要はなく、ツールを登録・編集したいときだけ起動する運用を想定しています。

### 注意事項

| 項目                       | 内容                                                                                                                                                                                                                              |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **通信**                   | 現状HTTPのみ。社内LAN内での利用のみを想定                                                                                                                                                                                          |
| **他PCから繋がらない場合** | サーバー側の `appsettings.json` に `Urls: "http://0.0.0.0:5000"` の設定漏れ、またはファイアウォール未開放が原因のことが多い（「サーバーのセットアップ」手順3・5を参照）。クライアントは接続失敗時にローカルキャッシュへ静かにフォールバックするため、症状だけでは気づきにくい |
| **管理者パスワード**       | `Asterism.Admin` の appsettings.json に平文で保存される（起動時に自動認証するため）。この配布物自体を管理者以外に渡さないこと。一般利用者向け `Asterism.Client` には管理機能・管理者パスワードを一切含めない                     |
| **wwwrootのバックアップ**  | `wwwroot/` 以下（manifest.json・tools/・icons/）が全資産。定期的にバックアップしてください                                                                                                                                        |
| **クライアントの自己更新** | ツールID `tool-asterism-client`（Zip型）をAsterismに登録し、クライアントEXE入りZIPを配布すると、管理者がバージョンを上げたときに一般ユーザーへ更新通知が届き、カードの「更新」ボタンでダウンロード→再起動の流れで自動適用できます。特別な実装は不要で、他のツールと全く同じ登録・更新フローに乗るだけです（`ToolCardViewModel`が`tool-asterism-client`というIDを特別扱いし、更新完了後に「再起動して適用しますか？」の確認→exeの上書き→再起動、を自動で行います） |
| **サーバー自体の更新**     | クライアントのような自己更新の仕組みはありません。新しい `Asterism.Server.exe` に差し替えてWindowsサービスを再起動する、という手動運用を想定しています（多数のPCに配るクライアントと違い、サーバーは常時稼働のインフラ側1台なので自動化の必要性が薄いため）                                                        |
| **ポート変更**             | `appsettings.json` の `Kestrel:Endpoints:Http:Url` または起動引数 `--urls` で変更できます                                                                                                                                         |
