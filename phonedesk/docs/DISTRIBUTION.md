# 配布依存について

今回の公開候補はソースリポジトリです。既存のWindows ZIPを一般公開するRelease操作は含めません。

Windowsビルドはscrcpy公式配布物を取得します。含まれるscrcpy/ADB/SDL/libusb/FFmpegなどのバイナリは、PhoneDeskソースのApache License 2.0だけで一括して扱いません。

ローカルの公式配布物をAPIで調査したところ、FFmpeg DLLはLGPL 2.1以降を返しました。依存ライセンス本文、該当ソースの提供経路、ビルド情報、改変範囲を整理したうえでバイナリReleaseを準備します。現時点ではバイナリ配布の準備完了とは扱いません。

追加したJavaヘルパーのソースは`../src/`、対応するビルド済みヘルパーは`../runtime-assets/`に含めています。端末アプリ・個人データ・ADB認証鍵は含めません。
