# MiniDeck インストーラーガイド

## インストーラーのビルド方法

1. **前提条件**
   - Visual Studio 2022以降
   - Visual Studio Installer Projects拡張機能
   - .NET Framework 4.7.2以降

2. **ビルドの手順**
   - リポジトリのルートディレクトリにある`BuildInstaller.bat`を実行する
   - または、Visual Studioで`MiniDeck.sln`を開き、`MiniDeckSetup`プロジェクトを右クリックしてビルドする

3. **MSIファイルの場所**
   - `c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi`

## インストーラーのカスタマイズ

### プロダクト情報の変更
MiniDeckSetup.vdprojファイルを開き、以下の項目を編集します：
- ProductName：製品名
- ProductVersion：バージョン番号
- Manufacturer：製造元名
- ProductCode：製品の固有識別子（自動生成）
- UpgradeCode：アップグレード用コード（一度設定したら変更しないでください）

### インストール先の変更
- DefaultLocation設定を編集して、デフォルトのインストール先を変更できます
- 通常は`[ProgramFiles64Folder][Manufacturer]\\[ProductName]`のような形式で指定します

### アイコンと画像の変更
- アプリケーションアイコン：ARPPRODUCTICONプロパティで指定
- インストーラーのバナー：BannerBitmapプロパティで指定

### ショートカットの設定
- スタートメニューとデスクトップにショートカットを作成するよう設定済み
- 追加のショートカットが必要な場合は、Shortcutセクションに追加します

## 配布方法

### 直接配布
1. 生成されたMSIファイルをユーザーに配布
2. ユーザーはMSIファイルをダブルクリックしてインストール

### Webサイトでの配布
1. MSIファイルをWebサーバーにアップロード
2. ダウンロードリンクを作成
3. 必要に応じてHTML説明ページを作成

### 更新プログラムの配布
1. バージョン番号を上げる（ProductVersion）
2. 新しいMSIをビルド
3. 古いバージョンからアップグレードするには、RemovePreviousVersionsプロパティをTRUEに設定

## トラブルシューティング

### ビルドエラーが発生する場合
- Visual Studioが管理者権限で実行されているか確認
- すべてのプロジェクト参照が正しいか確認
- リリースビルド構成が正しく設定されているか確認

### インストールエラーが発生する場合
- Windows Installerログを確認：`msiexec /i MiniDeckSetup.msi /l*v install_log.txt`
- すべての必要なファイルがインストーラーに含まれているか確認
- ユーザーが適切な権限を持っているか確認

---

## 開発者向け追加情報

### カスタムアクションの追加
インストール時に特定のスクリプトやコマンドを実行する必要がある場合：
1. "CustomAction"セクションにカスタムアクションを追加
2. インストーラーのイベントシーケンスにカスタムアクションを追加

### インストーラーUIのカスタマイズ
Visual Studioのインストーラープロジェクトエディターを使用して：
1. "User Interface"エディターを開く
2. 各ダイアログの外観と内容をカスタマイズ

### 多言語サポート
1. 複数の言語バージョンのインストーラーを作成する場合は、各言語用に個別のビルド構成を作成
2. または、WiX Toolsetなどの高度なインストーラー作成ツールに移行することを検討
