■ エンドユーザー向け使用方法（完成版）
① 初回セットアップ
1. 必要ソフト
VOICEVOXエンジン起動
http://localhost:50021
 が動作していること
Python環境
emotion解析用スクリプト配置
2. voice_config.json 設置

配置場所：

アプリ.exe と同じフォルダ
3. 起動
FindAncestor.exe を起動
② 基本操作フロー
1. キャラクター選択
voice_config.json に定義したキャラが使用されます

例：

default
tsundere
cool
2. テキスト入力
セリフを入力（またはOCRから自動取得）

例：

今日はいい天気だね
3. 再生

内部で以下が自動実行されます：

① 感情解析（Python）
② セグメント分割
③ キャラクター補正
④ VOICEVOX音声生成
⑤ 音声再生（NAudio）
③ 動作イメージ

入力：

今日は…楽しいね！

内部処理：

[今日は…] → sad (0.4)
[楽しいね！] → happy (0.8)

再生：

前半：低め・ゆっくり
後半：高め・速め

👉 自然な感情変化で再生

④ エラー時の挙動
自動でエラーダイアログ表示

内容：

Python失敗
VOICEVOX未起動
JSON不正

対応：

「コピー」で内容取得可能
⑤ キャンセル操作
再生中に停止可能
連打しても安全設計
⑥ カスタマイズ
voice_config.json 編集

例：

{
  "name": "tsundere",
  "basePitch": 1.1,
  "speakingStyle": "tsundere"
}

変更可能：

声の高さ
話速
感情強さ
語尾

👉 自分だけのキャラを作成可能

⑦ 高度機能（自動適用）
感情カーブ補間（easeInOut）
声ブレ防止（履歴補正）
微揺らぎ（自然さ）
セグメント分割再生
⑧ トラブル対処
音が出ない
VOICEVOX起動確認
ポート50021確認
感情が変
Pythonスクリプト確認
エラー表示
ダイアログ内容をコピー
■ 完成状態

このアプリは：

✔ 漫画・テキストから
✔ 感情を理解し
✔ キャラクターらしく
✔ 自然に話す

AI音声再生システムです

■ チーム最終判断

🎯 開発完了

■ 次にやると良い拡張（任意）
UI強化（タイムライン）
ボイス変換（RVC）
音声キャッシュ
字幕同期


■ 解決②：VOICEVOX起動（必須手順）
🔴 これはやらないと音が出ません
■ 手順（完全版）
① ダウンロード

👉 https://voicevox.hiroshiba.jp/

「Windows版」DL
zip解凍
② 起動
VOICEVOX.exe をダブルクリック
③ 確認（超重要）

ブラウザで開く：

http://localhost:50021/docs

👉 Swagger UI が出ればOK

④ エンジン起動確認（必須）

以下にアクセス：

http://localhost:50021/speakers

👉 JSONが返れば成功


├ bin/
 │   ├ Debug/
 │   │   ├ net8.0-windows/
 │   │   │   └ voice_config.json ←ここに必要