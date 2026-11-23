# TwilightInputOverlay Microsoft Store

Human: Fall Flat の Microsoft Store / Xbox PC 版向けの入力表示プラグインです。第1段階の対象は Windows x64、Unity 2019.4.24f1 / IL2CPP、BepInEx 6.0.0-be.793 IL2CPP、.NET 6 です。

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
- 独立設定画面は既定で `Home` です。HSRTimer と同じキーで両方の画面が開く場合は、どちらかの設定キーを変更してください。数値や文字は対象をクリックし、入力後に Enter で適用、Esc で取消できます。
- 設定先は `%LOCALAPPDATA%\TwilightInputOverlay-MicrosoftStore` です。従来の設定を引き継ぐ場合は、ゲーム終了中に旧環境の `BepInEx\config\TwilightInputOverlay` から `settings.ini`、`presets`、`lang` をコピーします。プリセット読み込み時は現在の設定画面キーと言語を維持します。
- この段階では矩形のキーを表示します。角丸、マウスカーソルと軌跡、Raining Keys、および効果用のデバッグ切り替えは後段の対象です。既存 INI にある対応設定値は引き継ぎます。
- HSRTimer の「Input Overlay」タブ統合は後段の対象です。独立ウィンドウを担当する `SettingsPanel` と設定内容を描く `PanelContent` は別クラスです。

## 実機確認

1. 起動時に BepInEx の `LogOutput.log` でプラグインが読み込まれ、IL2CPP 登録や IMGUI の例外が出ていないこと。
2. 初期 HUD の Y・W・A・S・D・Space、および左右マウスボタンの二分割表示が、同時押しと解放も含めて反応すること。サイドボタンは割り当て変更後に確認すること。
3. `Home` で設定が開き、キーの再割り当て、色・位置・倍率、プリセットの保存・復元が反映されること。再起動後は設定とキー配置を確認すること。
4. メニューとステージを往復しても HUD が一つだけ表示されること。HSRTimer を導入した状態でも両方の HUD が動き、設定キーを分けると個別に操作できること。
