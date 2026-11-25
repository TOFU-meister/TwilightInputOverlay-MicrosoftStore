# TwilightInputOverlay Microsoft Store 0.1.2-pre 最終検証

## 2026-11-25 Windows実機結果（ユーザー報告）

環境は Human: Fall Flat Microsoft Store／Xbox PC 1696732、Unity 2019.4.24f1、BepInEx 6.0.0-be.793、IL2CPP／Windows x64、.NET 6.0.7、Windows PowerShell 5.1です。DLLの生成元は `7ab8f4b9ca4f63d2882198ab89942b3a03bfea43` です。

- Releaseビルドとx64・バージョン・GUID・埋め込みLICENSEの検査に成功し、最終ログはError 0件でした。
- HSRTimer 0.1.3-preとのInput Overlay統合に加え、HSRTimer未導入時の単独起動、Homeでの設定表示、プリセット保存を確認しました。
- 通常キー、Mouse Overlay、Raining Keysと角丸を確認しました。診断は既定OFFで、関連UIの非表示とON／OFFを確認しました。既存の設定とプリセットは保持されました。
- プリセット切り替え後の `EnableDebugMode` のINI上の値は自動スモークテストで確認済みです。今回の実機報告はプリセット保存と診断の動作までです。

Raining Keysは通常のバー全体マスクを使います。高さに依存しない分割マスク方式は撤回済みで、配布対象に含まれません。[描画の開発履歴](../raining-keys-stage8.md)・[第9段階の短時間計測](../debug-mode.md)。長時間の改善率やFPS向上率は未測定です。

## 添付前の最終照合

| 配布候補 | サイズ | 期待SHA256 |
| --- | ---: | --- |
| `TwilightInputOverlay-v0.1.2-pre.dll` | 今回の報告に記載なし | `800441777D1C1BC8C97673AD778839BE90DFB3247ADE0092F1BB3B9AC9CBBE76` |

第12-1段階の作業環境からWindows上のDLL原本にはアクセスできませんでした。公開前に、添付する実ファイルへ `Get-Item -LiteralPath $file` と `Get-FileHash -LiteralPath $file -Algorithm SHA256` を実行します。欠落または不一致があれば公開を止めます。生成元からの変更はドキュメントのみです。一致した検証済みDLLをそのまま添付できます。

公開予定タグは `v0.1.2-pre` です。著作権表示を含む[MIT LICENSE全文](../../LICENSE)を添付または掲載します。[リリースノート案](v0.1.2-pre.md)
