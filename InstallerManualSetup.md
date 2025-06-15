# Visual Studio Installer Projectの手動設定ガイド

## README.txtの追加方法

1. Visual Studioで `MiniDeck.sln` を開く
2. ソリューションエクスプローラーで `MiniDeckSetup` プロジェクトを右クリック
3. 「View」→「File System」を選択
4. 「File System on Target Machine」が表示される
5. 「Application Folder」を右クリック→「Add」→「File...」を選択
6. `c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\README.txt` を参照して追加

![ファイルシステムの設定画面](image/installer_file_system.png)

## インストーラープロジェクトのビルド方法

1. ソリューションエクスプローラーで `MiniDeckSetup` プロジェクトを右クリック
2. 「Build」を選択
3. ビルドが成功すると、以下の場所にMSIファイルが生成されます：
   `c:\Users\s_kaz\source\repos\MiniDeck\MiniDeckSetup\Release\MiniDeckSetup.msi`

## インストーラーのカスタマイズ

### インストーラーUIの設定

1. `MiniDeckSetup` プロジェクトを右クリック→「View」→「User Interface」を選択
2. インストール時の各ステップ（スタート画面、インストール先フォルダ選択など）を設定可能

### ショートカットの追加

1. 「File System on Target Machine」で「User's Programs Menu」または「User's Desktop」を選択
2. 右クリック→「Create New Shortcut」を選択
3. 「Application Folder」内の `MiniDeck.exe` を選択
4. ショートカット名を入力（例: "MiniDeck"）

## 製品情報の設定

1. `MiniDeckSetup` プロジェクトを右クリック→「Properties」を選択
2. 以下の項目を設定:
   - ProductName: MiniDeck
   - Manufacturer: あなたの会社名
   - Version: x.y.z (例: 1.0.0)
   - Description: デスクトップマクロボタンアプリケーション

## インストーラービルド後の配布手順

1. `MiniDeckSetup.msi` ファイルと `README.txt` を同じフォルダに配置
2. 必要に応じて両ファイルをZIPアーカイブに圧縮
3. ウェブサイトやファイル共有サービスにアップロードして配布

*注意: MSIファイルに変更（バージョン更新など）を加える場合は、ProductCodeを変更する必要があります。UpgradeCodeは同一製品の異なるバージョン間で共通にしてください。*
