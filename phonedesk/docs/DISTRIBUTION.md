# バイナリ配布

PhoneDesk-Setup.exeを単体で配布します。第三者ライセンス・NOTICE・依存ソース情報・FFmpegビルド情報はdistribution-assets/からペイロードへ含め、インストール先のlicenses/等へ展開します。スマホ側のPhoneDesk View APK（phone-desk-view.apk）もペイロードへ同梱します。

1.0.23では同じReleaseへPhoneDesk-1.0.23-Sources.zipとSHA256SUMSを添付します。ソースZIPには該当コミットのgit archiveを収録します。スマホ側ビューワのソース（phone-view/）とAPKビルド手順（build-phone-view.ps1）も含まれます。

ADBはGoogleのplatform-tools 37.0.1と3ファイルのバイト一致を確認し、同じ配布物のNOTICEを収録しています。FFmpeg DLLのライセンス表示はLGPL 2.1以降で、共有ビルドを使用しています。MinGWランタイムとGCC Runtime Exception等も同梱します。

インストーラーの展開処理を使用し、一時フォルダーへの実ペイロード展開を検証します。1.0.23ではさらにセットアップを実行し、インストール先のPhoneDesk.exe 1.0.23.0とphone-desk-view.apkを確認しました。
