# musicDL

🎵 音楽ファイルのメタデータ管理と形式変換を行う強力なコマンドラインツールです。

## 機能

- **メタデータ処理**: アーティスト名、曲名の設定と音楽ファイルのメタデータ管理
- **音声変換**: 複数形式間での変換（MP3、FLAC、WAV、AAC、OGG対応）
- **アルバムアート処理**: 音楽ファイルのアルバムアートワーク管理
- **バッチ処理**: ディレクトリ内の複数ファイルの一括処理
- **拡張可能なアーキテクチャ**: カスタム拡張機能とプラグインのサポート
- **Spotify連携**: 強化されたメタデータ取得機能

## 必要要件

- **.NET 8.0** ランタイム
- **FFmpeg**（音声変換機能使用時）

## インストール

1. リポジトリをクローン
2. プロジェクトをビルド：
   ```bash
   dotnet build --configuration Release
   ```
3. 実行ファイルを起動またはグローバルインストール

## 使用方法

### メタデータ処理

既存の音声ファイルにメタデータを付与：

```bash
# 単一ファイルの処理
musicDL process <ファイルパス> --artist "アーティスト名" --title "曲名"
musicDL p <ファイルパス> -a "アーティスト名" -t "曲名"

# ディレクトリのバッチ処理
musicDL batch <ディレクトリ> --artist "アーティスト名"
musicDL b <ディレクトリ> -a "アーティスト名"
```

### 音声変換

音声ファイルを異なる形式間で変換：

```bash
# 単一ファイルの変換
musicDL convert <入力> --format mp3 --output <出力>
musicDL c <入力> -f mp3 -o <出力>

# ディレクトリのバッチ変換
musicDL batch-convert <ディレクトリ> --format mp3
musicDL bc <ディレクトリ> -f mp3
```

### 統合操作（変換＋処理）

一度の操作でファイル変換とメタデータ処理を実行：

```bash
# 単一ファイルの変換＋処理
musicDL transform <入力> --format mp3 --artist "アーティスト" --title "タイトル"
musicDL tf <入力> -f mp3 -a "アーティスト" -t "タイトル"

# ディレクトリのバッチ変換＋処理
musicDL batch-transform <ディレクトリ> --format mp3 --artist "アーティスト"
musicDL btf <ディレクトリ> -f mp3 -a "アーティスト"
```

## コマンドオプション

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--artist` | `-a` | メタデータのアーティスト名 |
| `--title` | `-t` | メタデータの曲名 |
| `--format` | `-f` | 変換先音声形式（mp3, flac, wav, aac, ogg） |
| `--output` | `-o` | 出力ファイルパス（オプション） |
| `--keep-metadata` | `-k` | 元ファイルのメタデータを保持（デフォルト: true） |
| `--debug` | `-d` | 詳細ログのデバッグモード有効化 |

## 対応形式

- **入力**: MP3, FLAC, WAV, AAC, OGG
- **出力**: MP3, FLAC, WAV, AAC, OGG

## 設定

アプリケーションは`setting.json`で設定を管理します。この設定ファイルには以下が含まれます：
- 拡張機能の設定
- 音声処理パラメータ
- API設定（Spotifyなど）

## 依存関係

このプロジェクトは以下の強力なライブラリを活用しています：

- **Microsoft.Extensions.Logging** - ログフレームワーク
- **Newtonsoft.Json** - JSONシリアライゼーション
- **SpotifyAPI.Web** - Spotify連携
- **System.CommandLine** - コマンドラインインターフェース
- **System.ComponentModel.Composition** - MEF コンポジション
- **TagLibSharp** - 音声メタデータ処理

## 使用例

```bash
# FLACファイルをメタデータ付きでMP3に変換
musicDL transform "song.flac" -f mp3 -a "The Beatles" -t "Hey Jude"

# フォルダ内の全音声ファイルをバッチ処理
musicDL batch-transform "./music_folder" -f mp3 -a "Various Artists"

# メタデータ変更なしのシンプルな形式変換
musicDL convert "input.wav" -f mp3 -o "output.mp3"
```

## ライセンス

このプロジェクトは[MIT License](LICENSE)の下で公開されています。
