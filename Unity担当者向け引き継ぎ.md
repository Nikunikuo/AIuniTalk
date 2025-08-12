# Unity担当者向け引き継ぎ資料

## 🎯 あなたがやること

### 1. 重要：NavMesh設定
- **Window > AI > Navigation** でNavigation Windowを開く
- 地面を選択して「Navigation Static」にチェック
- **Bake**ボタンを押してNavMeshを構築
- 障害物（屋台等）も「Navigation Static」で歩行不可エリアを設定

### 2. スクリプトをゲームオブジェクトに貼り付け
- `Unity/Scripts/` 内の`.cs`ファイルを全部インポート
- 以下のコンポーネントを適切なオブジェクトにアタッチ

### 3. 必要なゲームオブジェクト構成
```
Hierarchy:
├── GameManager (空のGameObject)
│   └── GameManager.cs
├── ServerConnection (空のGameObject)  
│   └── ServerConnection.cs
├── DialogManager (空のGameObject)
│   └── DialogManager.cs
├── FestivalWaypointManager (空のGameObject)
│   └── FestivalWaypointManager.cs
├── FestivalGround (Plane等) ★Navigation Static
├── FestivalStalls/ (屋台等の障害物) ★Navigation Static
├── Characters/
│   ├── Character_Alpha
│   │   ├── 3Dモデル
│   │   ├── NavMeshAgent (自動追加)
│   │   └── AdvancedCharacterController.cs ★新スクリプト
│   ├── Character_Beta  
│   │   └── (同上)
│   └── Character_Gamma
│       └── (同上)
└── UI/
    ├── Canvas
    └── SpeechBubblePrefab
```

### 4. 各キャラクターのInspector設定
`AdvancedCharacterController.cs`で：
- Character Id: "alpha", "beta", "gamma"
- Character Name: "アルファ", "ベータ", "ガンマ"  
- Base Walk Speed: 3.5
- Personal Space Radius: 1.5 (他キャラとの最低距離)
- Conversation Radius: 2.5 (会話開始距離)
- Character Layer: Characters レイヤー作成推奨

### 5. FestivalWaypointManagerの設定
- Festival Center: 会場の中心点を指定
- Festival Radius: 20 (会場サイズ)
- Waypoint Count: 9 (お祭りエリア数)
- Auto Create Waypoints: ✓ (自動生成)

### 6. NavMesh設定詳細
- **Agent Radius**: 0.5 (キャラサイズ)
- **Agent Height**: 2.0 (キャラの高さ)  
- **Max Slope**: 45 (登れる坂の角度)
- **Step Height**: 0.4 (段差の高さ)

### 4. UI設定
- Canvas作成
- SpeechBubble用のプレハブ作成（TextMeshProUGUI使用推奨）

## 🚨 重要な設定

### サーバー接続設定
`ServerConnection.cs`の：
- Server Url: "http://localhost:5000"

### 動作確認
1. Play押す前にPythonサーバー起動: `python server/app.py`
2. Unity Playボタン
3. F1, F2, F3キーでデバッグ

## 📋 チェックリスト
- [ ] 全スクリプト正常にコンパイル
- [ ] キャラが3体表示される
- [ ] キャラが歩き回る
- [ ] 近づくとサーバーに通信（Console確認）
- [ ] 吹き出しが表示される
- [ ] F1でリセットが効く

## 🆘 トラブル時
- Console見る
- Pythonサーバーのログ確認: `server/logs/app.log`
- 通信できない → ファイアウォール確認

## 🎯 会話トリガーの仕様

### **自動会話開始条件**
1. **距離**: キャラ同士が2.5m以内に近づく
2. **状態**: 両方とも会話中でない
3. **クールダウン**: 前回の会話から30秒経過
4. **自動チェック**: 0.1秒ごとに距離を監視

### **会話の流れ**
```
移動中 → 接近 → 会話開始 → 6ターン会話（約18秒） → 別れて移動
```

### **設定変更方法**
各キャラの `AdvancedCharacterController.cs` のInspectorで：
- `Conversation Radius`: 会話開始距離（現在2.5m）
- `Conversation Cooldown`: 再会話までの時間（現在30秒）

**📖 詳細は『会話トリガー設定ガイド.md』を参照してください**

---
**サーバー側（Python）は完璧に動きます！**
Unity側だけよろしくお願いします🙏