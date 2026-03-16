# FindAncestor プロジェクト構成と責務

## ViewModels

- **MainMenuViewModel**
  - メニューUI操作
  - 画像追加・削除のコマンド
  - どの表示モード（Slide / Scroll）を開くかの制御
  - サービスに処理を委譲

- **HomeViewModel**
  - 各親画像（A/B/C/D）の ViewModel を保持
  - Back コマンドでメニューに戻る
  - Dispose 管理

- **Scroll1RowViewModel**
  - 1行スクロール表示のタイマー管理
  - 画像読み込み（ImageManager 利用）
  - オーディオ再生（AudioManager 利用）
  - スクロール速度・サイズ変更

- **ImageViewModel**
  - スライドショー表示
  - DispatcherTimer で画像切り替え

## Services

- **ImageManager**
  - 画像ファイルの追加・削除・読み込み
  - 無限スクロール用コピー処理も統一可能

- **AudioManager**
  - MediaPlayer を利用した音声再生
  - フェードイン/フェードアウト
  - ループ再生、ボリューム管理

- **ExportManager**
  - FFmpeg を利用した動画エクスポート
  - 音声あり/なし両対応
  - フレームフォルダ指定で動画生成

## Models

- **DisplaySize / AspectRatioItem / ImageWithWidth**
  - データモデル

- **Enum**
  - DisplayMode / ImageFolderType / ImageSaveFormat
  - 定数管理