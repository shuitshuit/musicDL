# musicDL

musicDL は、YouTube の動画から音声を抽出し、メタデータを付与して保存するためのコマンドラインツールです。

## 主な機能

- **YouTubeからのダウンロード**: YouTubeのURLを指定して、動画をダウンロードし音声を抽出します。
- **メタデータ設定**: 曲のタイトル、アーティスト名を指定してファイルに埋め込むことができます。
- **コーデック選択**: 音声ファイルと動画ファイルのコーデックを柔軟に選択できます。
- **既存ファイルの処理**: すでにダウンロード済みのファイルに対して、メタデータを付与することも可能です。

## 必須コンポーネント

本ツールを使用するには、以下のソフトウェアがコンピュータにインストールされ、パスが通っている必要があります。

- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [FFmpeg](https://ffmpeg.org/)

これらのパスは、`setting.json` ファイルで個別に指定することも可能です。

## インストール

リリースページから最新のインストーラー (`setup.exe`) をダウンロードし、実行してください。

## 使用方法

### 音楽をダウンロードする

基本的なコマンドは `download` (またはエイリアス `dl`) です。

```bash
musicDL download <YouTubeのURL> [オプション]
```

**例:**

```bash
# URLを指定してダウンロード
musicDL dl "https://www.youtube.com/watch?v=xxxxxxxx"

# ファイル名、アーティスト名、タイトルを指定してダウンロード
musicDL dl "https://www.youtube.com/watch?v=xxxxxxxx" -f "ファイル名" -a "アーティスト" -t "タイトル"

# 音声コーデックをaacに指定
musicDL dl "https://www.youtube.com/watch?v=xxxxxxxx" --ae aac
```

#### `download` コマンドのオプション

| オプション | エイリアス | 説明 | デフォルト値 |
|:---|:---|:---|:---|
| `--file` | `-f` | 拡張子を除いたファイル名を指定します。 | 動画のタイトル |
| `--artist` | `-a` | アーティスト名を指定します。 | - |
| `--title` | `-t` | 曲のタイトルを指定します。 | - |
| `--videoExtension` | `--ve` | 動画のコーデックを指定します。 | `mp4` |
| `--audioExtension` | `--ae` | 音声のコーデックを指定します。 | `flac` |
| `-y` | | 確認なしでファイルを上書きします。 | `false` |
| `--debug` | `-d` | デバッグモードで実行します。 | `false` |

### 既存のファイルを処理する

`process` コマンドを使用すると、既存の音声ファイルにメタデータを付与できます。

```bash
musicDL process <ファイルパス> [オプション]
```

**例:**

```bash
musicDL process "C:\path\to\music.mp3" -a "アーティスト" -t "タイトル"
```

#### `process` コマンドのオプション

| オプション | エイリアス | 説明 |
|:---|:---|:---|
| `--artist` | `-a` | アーティスト名を指定します。 |
| `--title` | `-t` | 曲のタイトルを指定します。 |
| `--debug` | `-d` | デバッグモードで実行します。 |

## 設定

`musicDL.exe` と同じ階層にある `setting.json` ファイルを編集することで、各種設定をカスタマイズできます。

```json
{
  "FfmpegPath": "ffmpeg",
  "YtdlpPath": "yt-dlp",
  "VideoEncode": {
    // ビデオエンコード設定
  },
  "AudioEncode": {
    // オーディオエンコード設定
  },
  "Extended": {
    // 拡張設定
  }
}
```

- `FfmpegPath`: `ffmpeg.exe` のパスを指定します。
- `YtdlpPath`: `yt-dlp.exe` のパスを指定します。

## ビルド方法

このプロジェクトは Visual Studio 2022 で開発されています。
`musicDL.sln` ファイルを開き、ビルドしてください。
