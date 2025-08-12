# AIuniTalk - Unity AI Character Conversation System

## 🎯 Project Overview

AIuniTalk is a Unity-based system where multiple AI characters autonomously converse using OpenAI API. Designed for exhibition use (target: 2025/09/06) with focus on easy operation by non-technical users.

## 🛠 Technology Stack

### Server (Python)
- **Flask**: REST API + WebSocket
- **OpenAI API**: Conversation generation (gpt-4o-mini)  
- **Socket.IO**: Real-time communication

### Client (Unity)
- **Unity 2022 LTS**
- **NavMesh**: Character movement
- **Canvas UI**: Speech bubble system

## 📁 Project Structure

```
AIuniTalk/
├── server/                           # Python Flask server
│   ├── app.py                       # Main server application
│   ├── services/
│   │   ├── llm_service.py          # OpenAI API integration
│   │   ├── dialog_service.py       # Conversation management
│   │   └── location_service.py     # Location context service
│   ├── config/
│   │   ├── agents.json             # Character configurations
│   │   └── locations.json          # Festival location settings
│   └── requirements.txt            # Python dependencies
├── Unity/                           # Unity project
│   ├── Scripts/
│   │   ├── Character/              # Character controllers
│   │   │   └── AdvancedCharacterController.cs
│   │   ├── Dialog/                 # Conversation system
│   │   │   ├── DialogManager.cs
│   │   │   └── SpeechBubble.cs
│   │   ├── Network/                # Server communication
│   │   │   └── ServerConnection.cs
│   │   └── Core/                   # Core systems
│   │       └── FestivalWaypointManager.cs
│   └── Scenes/                     # Unity scenes
├── 32インチ展示用微調整ガイド.md      # 32-inch display adjustment guide
├── 会場設定ガイド.md                 # Venue setup guide
├── README.md                       # Setup instructions
└── Teema.md                       # Original project specification
```

## 🎮 Core Features

### 1. Autonomous Character Movement
- NavMesh-based pathfinding with obstacle avoidance
- Random waypoint selection in festival venue
- Character-to-character collision avoidance

### 2. Proximity-Based Conversations  
- Auto-start conversations when characters within 2m
- 6-turn conversation system (~18 seconds)
- Location-aware context injection

### 3. OpenAI Integration
- gpt-4o-mini model with fallback to template responses
- Offline operation capability
- Automatic API quota handling

## 👥 Character System

**3 Characters Available:**
- **Alpha (アルファ)**: Bright and sociable  
- **Beta (ベータ)**: Calm and intellectual
- **Gamma (ガンマ)**: Gentle and kind

Each character has unique personality, speech patterns, and walking speeds configurable in `server/config/agents.json`.

## 🎪 Festival Venue System  

**7 Festival Areas:**
1. Central Plaza (中央広場)
2. Takoyaki Stand (たこ焼き屋台)
3. Cotton Candy (わたあめ屋台) 
4. Goldfish Scooping (金魚すくい)
5. Shooting Gallery (射的)
6. Stage Front (ステージ前)
7. Fireworks Spot (花火スポット)

Each location has unique context (sounds, smells, atmosphere) defined in `server/config/locations.json`.

## 📺 Exhibition Configuration (32-inch Display Optimized)

### Target Environment
- **Display**: 32-inch (portrait/landscape)
- **Resolution**: 1920x1080  
- **Exhibition Date**: 2025/09/06
- **Operators**: Non-technical users

### Optimized Settings
- **Festival Radius**: 12m (compact layout)
- **Character Walk Speed**: 2.8m/s (observable pace)
- **Idle Time**: 2-6 seconds (active movement)
- **Conversation Range**: 2.0m
- **Waypoint Count**: 7 locations

## 🚀 Quick Start

### 1. Server Setup
```bash
cd server
pip install -r requirements.txt
```

Create `.env` file:
```bash
OPENAI_API_KEY=your_openai_api_key_here
```

Run server:
```bash  
python app.py
```

### 2. Unity Setup
1. Open Unity project in Unity 2022 LTS
2. Ensure server is running (http://localhost:5000)
3. Play main scene
4. Characters will start moving and conversing automatically

## ⌨️ Controls

### Unity Shortcuts
- **F1**: System reset
- **F2**: Force conversation start (testing)  
- **F3**: Toggle debug information

### Server Endpoints
- `GET /healthz`: Health check
- `GET /config/agents`: Get character configs
- `POST /dialog/turn`: Generate conversation turn
- `POST /dialog/reset`: Reset conversation state

## 🔧 Configuration Files

### Character Settings (`server/config/agents.json`)
```json
{
  "id": "alpha",
  "name": "アルファ", 
  "personality": "明るく社交的",
  "speaking_style": "フレンドリーでカジュアル",
  "walking_speed": 1.1,
  "topics": ["音楽", "食べ物", "お祭り"]
}
```

### Location Settings (`server/config/locations.json`)  
```json
{
  "name": "Takoyaki_Stand",
  "display_name": "たこ焼き屋台",
  "context_description": "たこ焼きの良い香りが漂う屋台",
  "sounds": ["ジュージューという音"],
  "smells": ["ソースの香り", "たこ焼きの香り"]
}
```

## 📊 Adjustment Guides

### For 32-inch Display Fine-tuning
- See `32インチ展示用微調整ガイド.md` for detailed parameter adjustment
- Common adjustments: walk speed, conversation frequency, venue size

### For Venue Setup
- See `会場設定ガイド.md` for coordinate system and layout configuration
- Supports different venue sizes and character densities

## 🛡️ Error Handling & Fallbacks

### OpenAI API Issues
- Automatic fallback to template responses
- Graceful handling of rate limits and quota exceeded
- Offline operation capability

### Unity Issues  
- NavMesh validation and error recovery
- Character collision handling
- UI failsafe mechanisms

## 📝 Logs & Debugging

### Server Logs
```bash
server/logs/app.log           # Server application logs
```

### Unity Console  
- Character movement states
- Conversation triggers  
- API communication status

## ⚠️ Important Notes

### Security
- Store OpenAI API key in `.env` file only
- Never commit `.env` to repository
- Use template responses for demo without API key

### Performance  
- Optimized for 3 concurrent characters
- Designed for extended exhibition operation
- NavMesh optimization for smooth movement

### Deployment
- Requires internet connection for OpenAI API
- Fallback system ensures operation during network issues
- Exhibition-ready stability focus

## 🔄 System Architecture

```
┌─────────────────┐    HTTP/WebSocket    ┌─────────────────┐
│                 │ ◄─────────────────► │                 │
│   Unity Client  │                     │  Flask Server   │
│                 │                     │                 │
│ • Character AI  │                     │ • Dialog Mgmt   │  
│ • Movement      │                     │ • OpenAI API    │
│ • UI Display    │                     │ • Location Ctx  │
│                 │                     │                 │
└─────────────────┘                     └─────────────────┘
                                                 │
                                                 ▼
                                        ┌─────────────────┐
                                        │   OpenAI API    │
                                        │   (gpt-4o-mini) │
                                        └─────────────────┘
```

## 📈 Development Status

### ✅ Completed Features
- Full server-client communication system
- Character movement with NavMesh integration
- Conversation system with OpenAI API
- 32-inch display optimization
- Comprehensive documentation
- Error handling and fallback systems

### 🔮 Future Enhancements  
- Voice synthesis integration
- Character animation improvements
- Additional venue layouts
- Multi-language support

---

**🎯 This system prioritizes stable exhibition operation. All major settings can be modified through JSON configuration files for easy adjustment by non-technical users.**

## 💡 Claude Development Notes

When working on this project:
1. **Server runs on `http://localhost:5000`** by default
2. **Key files to modify**: `agents.json`, `locations.json` for behavior changes
3. **Unity scripts** follow namespace pattern `AIuniTalk.*`
4. **Movement parameters** optimized for 32-inch display viewing
5. **Conversation system**: 6-turn conversations with automatic termination
6. **Fallback responses** available in `llm_service.py` for offline operation
7. **File locations**:
   - Character control: `Unity/Scripts/Character/AdvancedCharacterController.cs`
   - Dialog management: `Unity/Scripts/Dialog/DialogManager.cs` 
   - Server communication: `Unity/Scripts/Network/ServerConnection.cs`
   - Venue setup: `Unity/Scripts/Core/FestivalWaypointManager.cs`
8. **Optimization settings**: All values in scripts are pre-configured for 32-inch exhibition display
9. **Configuration changes**: Modify JSON files in `server/config/` for character/location adjustments
10. **Troubleshooting**: Use adjustment guides for common exhibition setup issues