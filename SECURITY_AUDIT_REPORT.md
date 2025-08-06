# musicDLプロジェクト脆弱性診断レポート

**診断日時**: 2025-08-06  
**対象プロジェクト**: musicDL YouTube音楽ダウンローダー  
**診断範囲**: 全ソースコード、設定ファイル、依存関係  

## 🛡️ **総合評価: 中リスク**

### 🚨 **重大な脆弱性（2件）**

#### 1. **機密情報の平文保存** [高危険度]
- **場所**: `musicDL/setting.json:22`
- **詳細**: 
  ```json
  "spotifyClientSecret": "b144f7c7e957479994dc850c31eb69e8"
  ```
- **影響**: SpotifyのClient Secretが設定ファイルに平文で保存されており、GitHubリポジトリにコミット済み
- **リスク**: API認証情報の漏洩、不正なAPI利用
- **対策**: 
  - 環境変数 (`SPOTIFY_CLIENT_SECRET`) への移行
  - 設定ファイルからの機密情報削除
  - `.gitignore`への追加

#### 2. **コマンドインジェクションの可能性** [高危険度]
- **場所**: `musicDL/DL.cs:144-206`
- **詳細**:
  ```csharp
  var arg = $"-i \"{video.Data}\" -vcodec {Setting.VideoEncode[VideoExtension.ToString()][0]} " +
      $"-c:a {Setting.VideoEncode[VideoExtension.ToString()][1]} \"{_tempVideoPath}\" -y";
  ProcessStartInfo startInfo = new() { 
      FileName = Setting.FfmpegPath, 
      Arguments = arg 
  };
  ```
- **影響**: `video.Data`や設定値が適切にエスケープされておらず、任意のコマンド実行が可能
- **リスク**: システムの完全な乗っ取り
- **対策**:
  - `ProcessStartInfo.ArgumentList` の使用
  - 入力値の適切なエスケープ処理
  - ホワイトリストによる入力検証

### ⚠️ **中程度の脆弱性（3件）**

#### 3. **不適切な例外処理**
- **場所**: `AlbumArt/AlbumArt.cs:192`
- **詳細**:
  ```csharp
  DateTime date = DateTime.Parse(release); // FormatExceptionの可能性
  ```
- **影響**: 不正な日付形式でアプリケーションクラッシュ
- **対策**: `DateTime.TryParse` の使用

#### 4. **Path Traversal攻撃のリスク**
- **場所**: `AlbumArt/AlbumArt.cs:104`
- **詳細**:
  ```csharp
  albumArtPath = $@"{directlyPath}\AlbumArt\{music.FileName}.{extension}";
  ```
- **影響**: `music.FileName` に `../` が含まれる場合の任意ディレクトリアクセス
- **対策**: 
  - `Path.Combine` の使用
  - ファイル名の検証（不正文字の除去）
  - `Path.GetFullPath` による正規化

#### 5. **HTTPSの強制なし**
- **場所**: `Sharing/Sharing.cs:29`
- **詳細**:
  ```csharp
  private const string RedirectUri = "http://localhost:8080/callback";
  ```
- **影響**: OAuth認証フローでの通信傍受リスク（ローカル環境では問題なし）
- **対策**: 本番環境ではHTTPS URLの使用

### ✅ **良好な実装（4点）**

1. **OAuth 2.0 + PKCE**: 
   - `Sharing/Sharing.cs:134-143` で適切なPKCE実装
   - セキュアな認証フロー

2. **入力検証**: 
   - コマンドライン引数は `System.CommandLine` で適切に処理
   - 型安全な引数解析

3. **URI エスケープ**: 
   - `Sharing/Sharing.cs:173` で `Uri.EscapeDataString` を適切に使用

4. **MEFセキュリティ**: 
   - 拡張機能の動的読み込みは適切に実装
   - `[Export(typeof(IExtension))]` による制御

### 🔍 **その他の調査結果**


#### 依存関係の脆弱性
- **調査済みパッケージ**:
  - `YoutubeDLSharp 1.1.2`
  - `SpotifyAPI.Web 7.2.1`  
  - `Microsoft.AspNetCore.SignalR.Client 9.0.6`
  - `System.CommandLine 2.0.0-beta4.22272.1`
- **結果**: 既知の脆弱性は検出されず

### 📋 **推奨対策（優先度別）**

#### 🔴 **優先度：高（即座に対応）**
1. **機密情報の環境変数化**
   ```csharp
   string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET") 
       ?? throw new InvalidOperationException("SPOTIFY_CLIENT_SECRET not set");
   ```

2. **ffmpeg引数の安全な構築**
   ```csharp
   var startInfo = new ProcessStartInfo(Setting.FfmpegPath);
   startInfo.ArgumentList.Add("-i");
   startInfo.ArgumentList.Add(video.Data);
   startInfo.ArgumentList.Add("-vcodec");
   startInfo.ArgumentList.Add(Setting.VideoEncode[VideoExtension.ToString()][0]);
   ```

#### 🟡 **優先度：中（1-2週間以内）**
3. **例外処理の改善**
   ```csharp
   if (DateTime.TryParse(release, out var date))
   {
       musicFile.Tag.Year = Convert.ToUInt32(date.Year);
   }
   ```

4. **ファイルパスの安全な構築**
   ```csharp
   string safeFileName = Path.GetFileName(music.FileName); // パストラバーサル防止
   string albumArtPath = Path.Combine(directlyPath, "AlbumArt", $"{safeFileName}.{extension}");
   ```

#### 🟢 **優先度：低（必要に応じて）**
5. **HTTPS URLへの変更**（本番環境デプロイ時）
6. **証明書検証の強化**（企業環境での使用時）

### 📊 **セキュリティスコア: 6/10**

- **良好な点**: 基本的なセキュリティ対策、適切な認証フロー
- **改善点**: 機密情報管理、コマンドインジェクション対策
- **全体評価**: 個人使用レベルでは許容範囲、本格運用には改善が必要

### 📞 **報告者情報**

**診断実行**: Claude Code AI Assistant  
**診断手法**: 静的コード解析、設定ファイル監査、依存関係チェック  
**検証範囲**: C#ソースコード全体、JSON設定ファイル、プロジェクトファイル

---

> **注意**: このレポートは防御的セキュリティ分析の目的で作成されました。発見された脆弱性は速やかに修正することを強く推奨します。