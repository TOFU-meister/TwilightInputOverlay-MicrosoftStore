# TwilightInputOverlay Microsoft Store

Human: Fall Flat の Microsoft Store / Xbox PC 版向けの入力表示プラグインです。対象は Windows x64、Unity 2019.4.24f1 / IL2CPP、BepInEx 6.0.0-be.793 IL2CPP、.NET 6 です。

- 移植元は [TwilightCup/TwilightInputOverlay v1.3.0](https://github.com/TwilightCup/TwilightInputOverlay/tree/b84b4d2bf0f8535f18e203c28e0491e8516b012c) です。
- IL2CPP のコンポーネント起動と IMGUI の互換処理は [HSRTimer Microsoft Store](https://github.com/TOFU-meister/HSRTimer-MicrosoftStore/tree/0a5ba0d708aa0bc55559ebc6ed12c2a2a4d4bf70) を参考にしています。
- [MIT License](LICENSE) · Copyright (c) 2026 TwilightCup

## Windows PowerShell でのビルド

.NET 6 SDK をインストールし、対象のゲームを BepInEx 6 IL2CPP で一度起動して終了します。対象ゲームの同じインストールから `BepInEx/core` と、自動生成された `BepInEx/interop` を参照します。

リポジトリ直下で PowerShell を開き、`$BepInExDir` を実際のパスに置き換えてください。

```powershell
$BepInExDir = 'C:\Path\To\HumanFallFlat\BepInEx'
dotnet build .\src\TwilightInputOverlay\TwilightInputOverlay.csproj -c Release "-p:BepInExDir=$BepInExDir"
```

ビルド成功後、ゲームを終了した状態で、DLL とライセンスを配置します。

```powershell
$BuildDir = '.\src\TwilightInputOverlay\bin\Release\net6.0'
$PluginDir = Join-Path $BepInExDir 'plugins\TwilightInputOverlay'
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item -Path (Join-Path $BuildDir 'TwilightInputOverlay.dll'), (Join-Path $BuildDir 'LICENSE') -Destination $PluginDir
```

同じプラグイン ID の DLL は一つだけ配置してください。言語ファイルは DLL に内蔵しています。スタブによる補助ビルドは C# ロジックの検証専用です。実機への配置や配布には上記の実機参照で生成した DLL を使用してください。

## 設定と対応範囲

- HUD はキーの押下状態を表示します。キー配置、二分割キー、キーの幅、ラベルのサイズ・位置、色、拡大率、フェードを設定できます。
- 独立設定画面は既定で `Home` です。HSRTimer と同じキーで両方の画面が開く場合は、どちらかの設定キーを変更してください。各スライダーの右に数値欄があります。クリックして入力し、Enter で確定、Esc で取り消します。範囲外の数値はスライダーの上下限に収めます。色の保存精度は従来の RGBA 各8ビットです。
- 設定先は Windows が返すドキュメントフォルダーの `TwilightInputOverlay-MicrosoftStore` です。初回は旧 `%LOCALAPPDATA%\TwilightInputOverlay-MicrosoftStore` の `settings.ini`、`presets`、`lang` を、移行先に同名ファイルがない場合にコピーします。旧データはそのまま残り、Documents が利用できない場合は LocalAppData を使用します。実際の保存先は起動ログに表示されます。
- 「Key Overlay」の角丸は `0` で矩形になります。「Mouse Overlay」では仮想カーソルと背景を個別に切り替えられます。軌跡を消すには伸びを `1` に設定します。
- 「Raining Keys」を有効にすると、最上段のキーからバーが上昇します。二分割キーは左右独立です。ルート画面の「Debug」で、効果の動作中にバー数、キャッシュ、メモリの統計をログに出せます。
- INI のキーとプリセット形式は第1段階から共通です。テクスチャの生成や転送に失敗した場合はログに記録し、キーとバーは矩形、仮想カーソルは四角の表示で継続します。
- HSRTimer Microsoft Store `0.1.0-pre.1` と組み合わせると、HSRTimer の設定に「Input Overlay」タブが追加されます。登録が成功した場合は HSRTimer の設定キーを使用します。HSRTimer が未導入、`0.1.0-pre`、または登録が失敗した環境では、Overlay の独立画面を使用します。統合時も設定とプリセットは Overlay の保存先に書き込みます。

## 実機確認

1. 起動時に BepInEx の `LogOutput.log` でプラグインが読み込まれ、IL2CPP 登録や IMGUI の例外が出ていないこと。
2. 初期 HUD の Y・W・A・S・D・Space、および左右マウスボタンの二分割表示が、同時押しと解放も含めて反応すること。サイドボタンは割り当て変更後に確認すること。
3. `Home` で設定が開き、キーの再割り当て、色・位置・倍率、プリセットの保存・復元が反映されること。再起動後は設定とキー配置を確認すること。
4. メニューとステージを往復しても HUD が一つだけ表示されること。HSRTimer を導入した状態でも両方の HUD が動き、設定キーを分けると個別に操作できること。
5. 「Key Overlay」の角丸を `0` と正の値で切り替え、キーの角、枠線、左右マウスの二分割と押下色を確認すること。
6. 「Mouse Overlay」でカーソルと背景を個別に切り替え、ゲームプレイ中とメニューで移動を確認すること。速い移動、停止、反転で軌跡が伸縮し、伸びを `1` にすると軌跡が消えること。Clamp、Wrap、中央に戻る設定も確認すること。
7. 「Raining Keys」で最上段のキーを長押しし、離してから再入力すること。バーが指定範囲で伸び、解放後に上端から消え、左右マウスで独立して発生すること。角丸、色、間隔、速度、幅を変更し、プリセットの保存と復元を確認すること。
8. 「Debug」の有効時に動作中の `[RainingKeys]` 統計が出て、無効時は止まること。Overlay 4クラスの登録が成功し、Overlay 名義の `unsupported parameter` が出ないことを確認すること。
