# PhoneDesk

[scrcpy](https://github.com/Genymobile/scrcpy) をベースにした、Windows向けAndroid操作アプリです。Genymobile公式版とは別の派生プロジェクトです。

## 機能

- USB接続を検出し、同じウィンドウ内へスマホ画面を表示
- PCのマウス・物理キーボードで操作。日本語変換はスマホ側IMEを使用
- 点灯、操作を続けたままの消灯、端末ロック
- スマホ本体の右回転と元に戻す操作。切断時は回転設定を復元
- PC音声出力の切り替え。映像・キーボードを再接続せず、Windows全体の音量へ同期
- スマホでコピーしたテキストをPCへ転送。PCからスマホへの自動転送は無効

## 対応環境・制限

Windows 10/11 x64、.NET Framework 4.x、USBデバッグと端末側の接続許可が必要です。音声転送はAndroid 13以降が対象で、アプリによる音声キャプチャ制限があります。コピー共有は文字のみで、画像・ファイルは対象外です。ロック画面や保護された画面はAndroid側の制限で黒く表示される場合があります。

## 上流との差分

ベースはscrcpy v5.0.1（commit `a60891aea193d92e7e5c3942700eca63f9d19a5f`）です。

- `phonedesk/`：Windows GUI、自動接続、音量・電源・回転・コピー共有のヘルパー、セットアップ、テスト
- `app/src/screen.c`：Windows IMEがUHID入力を消費しないよう、SDLのテキスト入力を無効化
- Windowsパッケージのビルドは、SHA-256を固定した公式バイナリに同じSDLインポート変更を適用します。ネイティブクライアントをソースから再ビルドしたものではありません。

詳細は[PhoneDeskの使い方](phonedesk/README.md)、[設計](phonedesk/docs/DESIGN.md)、[検証結果](phonedesk/docs/VALIDATION.md)を参照してください。上流の説明は[README.upstream.md](README.upstream.md)に保持しています。

## ビルド

```powershell
cd phonedesk
# ヘルパーを再ビルドする場合。Android SDKとJDKの場所を指定します。
./build-power.ps1 -AndroidSdk $env:ANDROID_HOME -JavaHome $env:JAVA_HOME
./build.ps1
```

WindowsアプリのビルドにはWindows標準の.NET Framework C#コンパイラーを使用します。初回は公式scrcpy v5.0.1配布物をダウンロードし、SHA-256を照合します。生成されるセットアップは`phonedesk/dist/PhoneDesk-Setup.exe`です。

## ライセンス・クレジット

scrcpyおよびPhoneDesk追加ソースはApache License 2.0で公開する構成です。[LICENSE](LICENSE)と[NOTICE](NOTICE)を参照してください。外部依存のバイナリにはそれぞれのライセンスが適用されます。配布バイナリの第三者ライセンス整備は[配布依存について](phonedesk/docs/DISTRIBUTION.md)に記載しています。
