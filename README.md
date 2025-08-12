# AIuniTalk - Unity AIキャラクター会話システム

## 概要
Unity上で複数のAIキャラクターが自動的に会話するデモンストレーションシステムです。
OpenAI APIを使用してリアルタイムで自然な会話を生成します。

## 必要環境
- **Unity**: 2022 LTS以上
- **Python**: 3.11以上
- **OpenAI API Key**: [OpenAI Platform](https://platform.openai.com)で取得

## クイックスタート

### 1. セットアップ
```bash
# リポジトリをクローン
git clone [repository-url]
cd AIuniTalk

# Pythonパッケージをインストール
pip install -r server/requirements.txt
```

### 2. APIキーの設定
`server/.env`ファイルを編集：
```
OPENAI_API_KEY=your-actual-api-key-here
ONLINE=true
```

### 3. サーバー起動

#### Windows
```batch
start_server.bat
```

#### Mac/Linux
```bash
python server/app.py
```

### 4. Unity起動
1. Unity Hubでプロジェクトを開く
2. `MainScene`を開く
3. Playボタンを押す

## 使い方

### 基本操作
- **F1**: システムリセット
- **F2**: 強制会話開始
- **F3**: デバッグ情報表示

### テスト実行
```bash
python test_server.py
```

## ディレクトリ構成
```
AIuniTalk/
├── server/                 # Pythonサーバー
│   ├── app.py             # メインサーバー
│   ├── .env               # 環境変数
│   ├── config/
│   │   └── agents.json    # キャラクター設定
│   └── services/
│       ├── llm_service.py # OpenAI連携
│       └── dialog_service.py
├── Unity/                  # Unityスクリプト
│   └── Scripts/
│       ├── Network/       # 通信処理
│       ├── Character/     # キャラ制御
│       ├── Dialog/        # 会話UI
│       └── Core/          # ゲーム管理
├── test_server.py         # テストスクリプト
├── start_server.bat       # 起動バッチ
└── README.md             # このファイル
```

## トラブルシューティング

### サーバーが起動しない
- Pythonのバージョンを確認: `python --version`
- 依存パッケージを再インストール: `pip install -r server/requirements.txt`

### APIエラーが出る
- `.env`ファイルのAPIキーを確認
- OpenAIのクレジット残高を確認
- `ONLINE=false`でオフラインモードを試す

### キャラクターが動かない
- F1キーでシステムリセット
- サーバーのログを確認
- Unity Consoleでエラーを確認

## カスタマイズ

### キャラクターの追加
`server/config/agents.json`を編集：
```json
{
  "id": "new_character",
  "name": "新キャラ",
  "personality": "性格の説明",
  "speaking_style": "話し方",
  "walking_speed": 1.0,
  "topics": ["好きな話題"]
}
```

### 会話の調整
- 会話ターン数: `DialogManager`の`maxTurns`
- 会話速度: `DialogManager`の`turnDuration`
- 近接距離: `AICharacterController`の`interactionRadius`

## 開発者向け

### デバッグモード
`server/.env`:
```
DEBUG=true
LOG_LEVEL=DEBUG
```

### オフラインモード
APIを使わずにテスト：
```
ONLINE=false
```

### ログ確認
```bash
# サーバーログ
tail -f server/logs/app.log

# Unity Console
F3キーでデバッグ表示
```

## ライセンス
MIT License

## サポート
問題が発生した場合は、Issueを作成してください。

## クレジット
- OpenAI GPT API
- Unity 2022 LTS
- Flask Framework