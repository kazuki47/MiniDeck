# MiniDeck v1.1.0 Release Notes

## 📦 配布パッケージ情報

**ファイル名**: `MiniDeck-SimpleDistribution.zip`  
**サイズ**: 約441KB  
**作成日**: 2025年6月22日  

## 🆕 v1.1.0 の新機能・修正

### 🔧 修正された問題
- **ボタン設定の永続化問題** - アプリ再起動後にボタン設定が失われる問題を修正
- **文字エンコーディング** - 設定ファイルの文字化け問題を解決（UTF-8対応）
- **プロパティ変更通知** - ActionButtonのプロパティ変更イベントを改善
- **設定保存の信頼性** - SettingsServiceの保存処理を強化

### ✨ 改善点
- より確実な設定の自動保存
- 日本語を含む設定の正確な保存・読み込み
- エラーハンドリングの強化

## 📋 パッケージ内容

```
MiniDeck-SimpleDistribution.zip
├── MiniDeck.exe (295KB)           # メインアプリケーション
├── MiniDeck.exe.config            # アプリ設定ファイル
├── MiniDeck.pdb                   # デバッグ情報
├── WindowsInput.dll (108KB)       # 入力制御ライブラリ
├── README.txt                     # 使用方法（日本語）
├── image/
│   └── MiniDeckIcon.ico          # アプリケーションアイコン
└── Resources/
    └── Icons/
        ├── calculator_icon.png    # 電卓アイコン
        ├── memo_icon.png         # メモ帳アイコン
        └── default_icon.jpg      # デフォルトアイコン
```

## 🖥️ 動作環境

- **必須**: Windows 10以降
- **推奨**: Windows 11
- **.NET Framework**: 4.7.2以降
- **メモリ**: 512MB以上
- **ディスク容量**: 10MB以上
- **画面解像度**: 1024x768以上

## 🚀 インストール方法

1. `MiniDeck-SimpleDistribution.zip` をダウンロード
2. 適当なフォルダに展開
3. `MiniDeck.exe` をダブルクリックして起動

**注意**: インストーラーは不要です。展開するだけで使用できます。

## 📝 使用例

### 基本的なボタン設定

| 表示名 | アクションタイプ | パス/キー | 説明 |
|--------|------------------|-----------|------|
| メモ帳 | アプリケーション起動 | `notepad.exe` | メモ帳を開く |
| 電卓 | アプリケーション起動 | `calc.exe` | 電卓を開く |
| コピー | キーボードショートカット | `Control+C` | 選択範囲をコピー |
| 貼り付け | キーボードショートカット | `Control+V` | クリップボードから貼り付け |

### 高度な設定例

| 表示名 | パス | 引数 | 説明 |
|--------|------|------|------|
| Chrome | `C:\Program Files\Google\Chrome\Application\chrome.exe` | `--new-window` | 新しいウィンドウでChrome起動 |
| VS Code | `C:\Users\[Username]\AppData\Local\Programs\Microsoft VS Code\Code.exe` | `.` | 現在のフォルダでVS Code起動 |

## 🛠️ トラブルシューティング

### よくある問題

**Q: アプリが起動しない**
- A: .NET Framework 4.7.2をインストールしてください

**Q: ボタンが反応しない**  
- A: 管理者権限で実行してみてください

**Q: 設定が保存されない**  
- A: ウイルス対策ソフトの設定を確認してください

## 🔄 以前のバージョンからの変更点

### v1.0.0 → v1.1.0
- ✅ 設定の永続化問題を完全に解決
- ✅ 文字化け問題を修正
- ✅ エラーハンドリングを改善
- ✅ コードの安定性を向上

## 📞 サポート

問題が発生した場合は、以下の情報と共にお知らせください：

- Windowsのバージョン
- エラーメッセージの詳細
- 実行しようとした操作
- .NET Frameworkのバージョン

---

**MiniDeck Project** © 2025  
シンプルで使いやすいデスクトップランチャー
