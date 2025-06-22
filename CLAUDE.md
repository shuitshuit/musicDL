# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。

## ビルドコマンド

```bash
# ソリューション全体をビルド
dotnet build musicDL.sln

# メインプロジェクトをビルドして実行
dotnet run --project musicDL

# 単一ファイル実行可能ファイルを公開
dotnet publish musicDL/musicDL.csproj -c Release --self-contained

# 特定のプロジェクトをビルド
dotnet build AlbumArt/AlbumArt.csproj
```

## アーキテクチャ概要

拡張可能なプラグインシステムを持つYouTube音楽ダウンローダーです。メインアプリケーションはyt-dlp経由で動画をダウンロードし、ffmpegで変換し、拡張機能を通じて処理します。

### コアコンポーネント

- **musicDL/DL.cs**: yt-dlpとの連携、ffmpeg変換（動画→音声）、音声正規化、拡張機能実行を処理するメインダウンロードオーケストレーター
- **musicDL/Program.cs**: System.CommandLineを使用したCLIインターフェース（ダウンロードおよび更新コマンド）
- **拡張機能**: MEF（Managed Extensibility Framework）を使用したプラグインシステム
  - AlbumArt: メタデータとアルバムアート埋め込み用のSpotify API統合
  - Sharing: SignalR認証フローを使ったWebサービスへのファイルアップロード

### 主要なアーキテクチャポイント

- 拡張機能は`IExtension`を実装し、`[Export(typeof(IExtension))]`属性を使用
- `setting.json`による設定には外部ツールパスと拡張機能設定が含まれる
- 一時ファイルは一時ディレクトリで処理され、最終的な保存先に移動される
- 音声処理パイプライン: ダウンロード → 動画変換 → 音声抽出 → 正規化 → 拡張機能処理 → ファイル配置

### 外部依存関係

- `yt-dlp.exe`: YouTubeビデオダウンロード
- `ffmpeg.exe`: メディア変換と音声正規化（dynaudnormフィルタ）
- Spotify Web API: メタデータとアルバムアート取得
- カスタムWebサービス: ファイル共有機能

### 設定構造

`setting.json`で設定される項目:
- ツールパス（`ffmpegPath`, `ytdlpPath`）
- エンコード設定（`videoEncode`, `audioEncode`辞書）
- 拡張機能設定（APIキーとエンドポイントを含む`extended`オブジェクト）