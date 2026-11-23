# TwilightInputOverlay Microsoft Store

Human: Fall Flat の Microsoft Store / Xbox PC 版向けの入力表示プラグインです。対象は Windows x64、Unity 2019.4.24f1 / IL2CPP、BepInEx 6.0.0-be.793 IL2CPP、.NET 6 です。

今回のテスト版は **0.1.1-pre** です。Raining Keys のテクスチャ管理を修正し、無効なテクスチャの参照による連続例外を防ぎます。角丸の描画に失敗した場合は、矩形のバーに切り替わります。

## 対応環境

- Windows 64 ビットの Human: Fall Flat Microsoft Store／Xbox PC 版（Unity 2019.4.24f1 IL2CPP）
- BepInEx **6.0.0-be.793 Unity IL2CPP Windows x64**
- ソースからビルドする場合は .NET 6 SDK が必要です。

## インストール方法

### 1. Xbox アプリでゲームのフォルダーを開く

1. Windows の **Xbox アプリ**を開き、**マイ ライブラリ**で **Human: Fall Flat** を探します。
2. ゲームを右クリックするか、ゲームの **…** メニューを開き、**管理 (Manage)** を選びます。
3. **ファイル (Files)** タブの **参照 (Browse)**、またはフォルダーを開くボタンを押します。エクスプローラーで開いた場所に `Content` があれば、その中を確認します。**ゲーム本体の `.exe` があるフォルダー**が、この手順で使う場所です。

`C:\XboxGames\Human Fall Flat\Content\` は例です。別のドライブやインストール先を選んだ PC では場所が異なります。メニュー名が違う場合や参照ボタンが見つからない場合は、Xbox アプリで選んだインストール先のドライブをエクスプローラーで開き、`XboxGames` などのゲーム保存フォルダーを確認してください。エクスプローラーで `.exe` が表示されない場合は、表示メニューでファイル名拡張子を表示できます。開けるゲームフォルダーを確認できず、`WindowsApps` にしか見当たらない場合は、ここで作業を止めてください。

参考 [Xbox アプリのゲーム管理とインストール先](https://news.xbox.com/en-us/2021/11/18/advanced-installation-and-management-features-available-for-insiders-through-the-xbox-app-for-windows/)

### 2. 対応する BepInEx をダウンロードする

[BepInEx 6.0.0-be.793 Unity IL2CPP Windows x64 の ZIP](https://builds.bepinex.dev/projects/bepinex_be/793/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.793%2Bdbc82f0.zip) をダウンロードします。**BepInEx 5、Unity Mono、Linux、Windows x86** はそれぞれ別の環境向けです。この手順ではリンク先の **Unity IL2CPP Windows x64** を使います。

### 3. BepInEx を入れて、ゲームを一度起動する

1. ゲームを終了します。ダウンロードした BepInEx の ZIP を右クリックし、**すべて展開**を選びます。
2. 展開先を開き、`BepInEx` と `winhttp.dll` が並んでいる場所まで進みます。**その階層の中身をすべて**、手順1で見つけたゲーム本体の `.exe` と同じフォルダーへコピーします。
3. 次の並びになっているか確認します。**`Content\BepInEx\BepInEx\`** のように二重になると読み込めません。

```text
C:\XboxGames\Human Fall Flat\Content\
├─ （ゲーム本体の .exe など）
├─ BepInEx\
│  └─ core\
├─ dotnet\
├─ winhttp.dll
├─ doorstop_config.ini
├─ .doorstop_version
└─ changelog.txt
```

4. Xbox アプリからゲームを起動します。初回は BepInEx が準備用のファイルを作るため、起動に時間がかかることがあります。ゲームのメニューが表示されたら終了します。
5. ゲームフォルダーの `BepInEx` を開き、`config\BepInEx.cfg`、`interop\`、`LogOutput.log` を確認します。`interop` は初回の準備が成功すると生成されます。`plugins` がなければ、次の手順で `BepInEx` の中に作れます。

参考 [BepInEx の IL2CPP ゲーム向け導入手順](https://docs.bepinex.dev/v6.0.0-pre.1/articles/user_guide/installation/unity_il2cpp.html)

### 4. プラグインの DLL を入れる

ゲームを終了したまま、使いたいプラグインの GitHub Releases を開き、対象バージョンの **Assets** から **DLL を直接**ダウンロードします。以下は対象バージョンが公開されたときのファイル名です。

| プラグイン | ダウンロードする DLL | 置くフォルダー |
| --- | --- | --- |
| [HSRTimer Releases](https://github.com/TOFU-meister/HSRTimer-MicrosoftStore/releases) | `HSRTimer-v0.1.2-pre.dll` | `BepInEx\plugins\` |
| [TwilightInputOverlay Releases](https://github.com/TOFU-meister/TwilightInputOverlay-MicrosoftStore/releases) | `TwilightInputOverlay-v0.1.1-pre.dll` | `BepInEx\plugins\` |

`BepInEx` の中に `plugins` がなければ作ります。旧 DLL を取り除き、ダウンロードした DLL を名前そのままで `plugins` の直下にコピーします。プラグインごとのサブフォルダーは不要です。新しい配布形式は **DLL 単体**です。GitHub の **Source code (zip)** はソースコードで、導入には使いません。過去の HSRTimer **`0.1.0-pre`** を使う場合だけ、当時の配布 ZIP を展開して、中の `HSRTimer.dll` を同じ HSRTimer 用フォルダーに入れます。

片方だけ使う場合は、その DLL だけを入れます。両方使う場合は次のようになります。パスは一例です。

```text
C:\XboxGames\Human Fall Flat\Content\
└─ BepInEx\
   └─ plugins\
      ├─ HSRTimer-v0.1.2-pre.dll
      └─ TwilightInputOverlay-v0.1.1-pre.dll
```

## 動作確認

1. Xbox アプリからゲームを起動し、メニューが表示されることを確認します。
2. HSRTimer を入れた場合はタイマー HUD を確認します。TwilightInputOverlay を入れた場合はキーを押し、入力表示が反応するか確認します。
3. **Home** キーで設定を開きます。キーを変更してある場合は設定済みのキーを使います。TwilightInputOverlay 単独なら独立した設定画面が開きます。対応版を両方入れた場合は、HSRTimer の設定画面で **Input Overlay** タブを開きます。
4. 読み込みを確認したい場合は、ゲームフォルダー内の `BepInEx\LogOutput.log` をメモ帳で開き、プラグイン名とバージョンを検索します。

## 更新・アンインストール

- **更新**するときはゲームを終了し、`BepInEx\plugins` 以下の別フォルダーも含めて、同じプラグインの旧 DLL を取り除いてから新しい DLL を入れます。名前にバージョンが付かない旧 DLL も確認してください。併用時は各プラグインの DLL を1個ずつ置きます。
- **アンインストール**するときはゲームを終了し、外したいプラグインの DLL を取り除きます。設定・PB・プリセットは DLL と別の場所にあるので、通常は残ります。
- **保存データも消したい場合**は、下の「設定と保存先」とログで実際の保存場所を確認します。残したいデータを先にバックアップし、ゲームを終了したうえで、対象プラグインの保存フォルダーだけを削除します。

## 設定と保存先

- HUD はキーの押下状態を表示します。キー配置、二分割キー、キーの幅、ラベルのサイズ・位置、色、拡大率、フェードを設定できます。
- 独立設定画面は既定で `Home` です。HSRTimer と同じキーで両方の画面が開く場合は、どちらかの設定キーを変更してください。位置や速度などのスライダーの右に数値欄があります。色は RGBA スライダーまたはカラーコード欄で指定できます。クリックして入力し、Enter で確定、Esc で取り消します。範囲外の数値はスライダーの上下限に収めます。色の保存精度は従来の RGBA 各8ビットです。
- 設定先は Windows が返すドキュメントフォルダーの `TwilightInputOverlay-MicrosoftStore` です。初回は旧 `%LOCALAPPDATA%\TwilightInputOverlay-MicrosoftStore` の `settings.ini`、`presets`、`lang` を、移行先に同名ファイルがない場合にコピーします。旧データはそのまま残り、Documents が利用できない場合は LocalAppData を使用します。実際の保存先は起動ログに表示されます。
- 「Key Overlay」の角丸は `0` で矩形になります。「Mouse Overlay」では仮想カーソルと背景を個別に切り替えられます。軌跡を消すには伸びを `1` に設定します。
- 「Raining Keys」を有効にすると、最上段のキーからバーが上昇します。二分割キーは左右独立です。ルート画面の「Debug」で、効果の動作中にバー数、キャッシュ、メモリの統計をログに出せます。
- INI のキーとプリセット形式は第1段階から共通です。テクスチャの生成や転送に失敗した場合はログに記録し、キーとバーは矩形、仮想カーソルは四角の表示で継続します。
- HSRTimer Microsoft Store `0.1.2-pre` と組み合わせると、HSRTimer の設定に「Input Overlay」タブが追加されます。登録が成功した場合は HSRTimer の設定キーを使用します。HSRTimer が未導入、`0.1.0-pre`、または登録が失敗した環境では、Overlay の独立画面を使用します。統合時も設定とプリセットは Overlay の保存先に書き込みます。

## トラブルシューティング

| 症状 | 確認すること |
| --- | --- |
| ゲームのフォルダーが見つからない | Xbox アプリのゲームの **… → 管理 → ファイル → 参照** を探します。見つからない場合は、インストール先に選んだドライブで `XboxGames` などを確認します。 |
| BepInEx が読み込まれない | `winhttp.dll`、`doorstop_config.ini`、`dotnet`、`BepInEx` がゲーム本体の `.exe` と同じ階層にあるか確認します。初回起動後に `LogOutput.log` が作られたかも確認します。 |
| `BepInEx` が二重になっている | コピー先を確認し、ゲーム本体の `.exe` から見て `BepInEx\core` になるよう、展開した ZIP の中身を配置し直します。 |
| 間違った BepInEx を入れた | ゲームを終了し、混在を避けて上記の **6.0.0-be.793 Unity IL2CPP Windows x64** を入れ直します。 |
| HUD が出ない | DLL が `BepInEx\plugins` の下にあるか、ログに読み込みが記録されているか、設定で HUD 表示が有効かを確認します。 |
| Home で設定が開かない | ゲーム画面をクリックしてから Home を押します。キーを変更済みの場合は変更後のキーを使います。統合時は HSRTimer 側の設定キーを使います。 |
| Input Overlay タブが出ない | HSRTimer **0.1.2-pre** と TwilightInputOverlay **0.1.1-pre** の両方がロードされているかをログで確認します。旧 HSRTimer **0.1.0-pre** との組み合わせでは Overlay の独立画面を使います。 |
| 旧版と新版が重複している | ゲームを終了し、`BepInEx\plugins` 全体でプラグイン名を検索します。同じプラグインの旧 DLL を外し、新 DLL だけを残します。 |

BepInEx のコンソール画面を隠したい場合は、ゲーム終了後に `BepInEx\config\BepInEx.cfg` をメモ帳で開き、既存の `[Logging.Console]` の `Enabled = false` を設定します。ログファイルは `BepInEx\LogOutput.log` で確認できます。[コンソール設定の参考](https://docs.bepinex.dev/articles/user_guide/troubleshooting.html#enable-console)

## 開発者向けビルド・配布手順

### 配布用 DLL を作成する

Windows x64 で実際にゲームを起動して生成した `BepInEx/interop` と、同じインストールの `BepInEx/core` を使います。

```powershell
.\scripts\New-StoreReleaseDll.ps1 -BepInExDir 'C:\Path\To\HumanFallFlat\BepInEx' -OutputDirectory .\release
```

この開発版では `TwilightInputOverlay-v0.1.1-pre.dll` が生成されます。公開時は実機確認済みの DLL を Release に添付し、著作権表示を含む [LICENSE](LICENSE) の全文を Release 本文に掲載するか、別の `LICENSE` アセットとして添付します。DLL にも同じライセンス全文が埋め込まれています。

### Windows PowerShell でのビルド

.NET 6 SDK をインストールし、対象のゲームを BepInEx 6 IL2CPP で一度起動して終了します。対象ゲームの同じインストールから `BepInEx/core` と、自動生成された `BepInEx/interop` を参照します。

リポジトリ直下で PowerShell を開き、`$BepInExDir` を実際のパスに置き換えてください。

```powershell
$BepInExDir = 'C:\Path\To\HumanFallFlat\BepInEx'
dotnet build .\src\TwilightInputOverlay\TwilightInputOverlay.csproj -c Release "-p:BepInExDir=$BepInExDir"
```

ビルド成功後、ゲームを終了した状態で、DLL とライセンスを配置します。

```powershell
$BuildDir = '.\src\TwilightInputOverlay\bin\Release\net6.0'
$PluginDir = Join-Path $BepInExDir 'plugins'
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item -Path (Join-Path $BuildDir 'TwilightInputOverlay.dll'), (Join-Path $BuildDir 'LICENSE') -Destination $PluginDir
```

同じプラグイン ID の DLL は一つだけ配置してください。言語ファイルは DLL に内蔵しています。スタブによる補助ビルドは C# ロジックの検証専用です。実機への配置や配布には上記の実機参照で生成した DLL を使用してください。

### 開発版の追加確認

1. 起動時に BepInEx の `LogOutput.log` でプラグインが読み込まれ、IL2CPP 登録や IMGUI の例外が出ていないこと。
2. 初期 HUD の Y・W・A・S・D・Space、および左右マウスボタンの二分割表示が、同時押しと解放も含めて反応すること。サイドボタンは割り当て変更後に確認すること。
3. `Home` で設定が開き、キーの再割り当て、色・位置・倍率、プリセットの保存・復元が反映されること。再起動後は設定とキー配置を確認すること。
4. メニューとステージを往復しても HUD が一つだけ表示されること。HSRTimer を導入した状態でも両方の HUD が動き、設定画面が統合されること。
5. 「Key Overlay」の角丸を `0` と正の値で切り替え、キーの角、枠線、左右マウスの二分割と押下色を確認すること。
6. 「Mouse Overlay」でカーソルと背景を個別に切り替え、ゲームプレイ中とメニューで移動を確認すること。速い移動、停止、反転で軌跡が伸縮し、伸びを `1` にすると軌跡が消えること。Clamp、Wrap、中央に戻る設定も確認すること。
7. 「Raining Keys」で最上段のキーを長押しし、離してから再入力すること。バーが指定範囲で伸び、解放後に上端から消え、左右マウスで独立して発生すること。角丸、色、間隔、速度、幅を変更し、プリセットの保存と復元を確認すること。
8. 「Debug」の有効時に動作中の `[RainingKeys]` 統計が出て、無効時は止まること。Overlay 4クラスの登録が成功し、Overlay 名義の `unsupported parameter` が出ないことを確認すること。

### バージョン管理

Store 版は `0.x.x-pre` を使い、各プラグインで独立して進めます。先頭の `0` と末尾の `-pre` はプレリリース期間中固定です。小機能、バグ修正、UI 調整は3番目を進めます。大きな機能追加、仕様変更、大規模な修正は2番目を進め、3番目を `0` に戻します。バージョン更新時は `PluginInfo.cs` と `.csproj` の番号をそろえます。公開時のタグは `v<version>`、配布 DLL は `TwilightInputOverlay-v<version>.dll` とします。


## ライセンス・クレジット

- 移植元は [TwilightCup/TwilightInputOverlay v1.3.0](https://github.com/TwilightCup/TwilightInputOverlay/tree/b84b4d2bf0f8535f18e203c28e0491e8516b012c) です。
- IL2CPP のコンポーネント起動と IMGUI の互換処理は [HSRTimer Microsoft Store](https://github.com/TOFU-meister/HSRTimer-MicrosoftStore/tree/0a5ba0d708aa0bc55559ebc6ed12c2a2a4d4bf70) を参考にしています。
- [MIT License](LICENSE) · Copyright (c) 2026 TwilightCup
