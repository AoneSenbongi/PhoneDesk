# バイナリ配布

PhoneDesk-Setup.exeを単体で配布します。第三者ライセンス・NOTICE・依存ソース情報・FFmpegビルド情報はdistribution-assets/からペイロードへ含め、インストール先のlicenses/等へ展開します。

同じReleaseへPhoneDesk-1.0.22-Sources.zipとSHA256SUMSを添付する構成です。ソースZIPには該当コミットのgit archive、FFmpeg 9.0.2、SDL 3.4.18、libusb 1.0.30、dav1d 1.5.4のソースと上流ビルド手順を収録します。版・取得URL・SHA-256はSOURCES.jsonに記載します。

ADBはGoogleのplatform-tools 37.0.1と3ファイルのバイト一致を確認し、同じ配布物のNOTICEを収録しています。FFmpeg DLLのライセンス表示はLGPL 2.1以降で、共有ビルドを使用しています。MinGWランタイムとGCC Runtime Exception等も同梱します。

インストーラーの展開処理を使用し、一時フォルダーへの実ペイロード展開を検証します。通常のインストール・自動起動設定・アプリ起動はこの検証では実行しません。
