import json
import random
import logging
from typing import Dict, List, Optional
from datetime import datetime
from services.location_service import LocationService

logger = logging.getLogger(__name__)

class DialogService:
    def __init__(self):
        self.agents = self._load_agents()
        self.location_service = LocationService()
        from services.llm_service import LLMService
        self.llm_service = LLMService()
    
    def _load_agents(self) -> Dict:
        """エージェント設定を読み込み"""
        try:
            with open('config/agents.json', 'r', encoding='utf-8') as f:
                data = json.load(f)
                return {agent['id']: agent for agent in data['agents']}
        except FileNotFoundError:
            logger.warning("agents.json not found, using default agents")
            return self._get_default_agents()
        except Exception as e:
            logger.error(f"Failed to load agents: {e}")
            return self._get_default_agents()
    
    def _get_default_agents(self) -> Dict:
        """デフォルトのエージェント設定"""
        return {
            "miku": {
                "id": "miku",
                "name": "ミク",
                "personality": "明るく元気で好奇心旺盛",
                "speaking_style": "です・ます調",
                "walking_speed": 1.0,
                "topics": ["音楽", "歌", "ダンス", "夏祭り"]
            },
            "rin": {
                "id": "rin",
                "name": "リン",
                "personality": "クールで少しツンデレ",
                "speaking_style": "タメ口",
                "walking_speed": 1.2,
                "topics": ["ゲーム", "アニメ", "お菓子", "花火"]
            },
            "len": {
                "id": "len",
                "name": "レン",
                "personality": "優しくて思いやりがある",
                "speaking_style": "です・ます調",
                "walking_speed": 0.8,
                "topics": ["料理", "読書", "星空", "屋台"]
            }
        }
    
    def generate_turn(
        self,
        agent_ids: List[str],
        turn: int,
        context: str,
        location: str,
        history: List[Dict]
    ) -> Dict:
        """会話の1ターンを生成"""
        
        try:
            speaker_id = self._select_speaker(agent_ids, turn, history)
            
            if speaker_id not in self.agents:
                logger.error(f"Unknown agent: {speaker_id}")
                speaker_id = agent_ids[0]
            
            agent = self.agents[speaker_id]
            
            conversation_context = self._build_context(
                agent, context, location, history
            )
            
            response_text = self.llm_service.generate_response(
                agent_data=agent,
                context=conversation_context,
                history=history,
                location=location
            )
            
            emotion = self._detect_emotion(response_text)
            
            response = {
                "speaker": speaker_id,
                "speaker_name": agent['name'],
                "text": response_text,
                "emotion": emotion,
                "turn": turn,
                "timestamp": datetime.now().isoformat()
            }
            
            logger.info(f"Turn {turn}: {agent['name']} says: {response_text}")
            return response
            
        except Exception as e:
            logger.error(f"Failed to generate turn: {e}")
            return self._generate_fallback_response(agent_ids[0], turn)
    
    def _select_speaker(
        self,
        agent_ids: List[str],
        turn: int,
        history: List[Dict]
    ) -> str:
        """話者を選択"""
        if not history:
            return random.choice(agent_ids)
        
        last_speaker = history[-1].get('speaker')
        available = [aid for aid in agent_ids if aid != last_speaker]
        
        if not available:
            return agent_ids[turn % len(agent_ids)]
        
        return random.choice(available)
    
    def _build_context(
        self,
        agent: Dict,
        context: str,
        location: str,
        history: List[Dict]
    ) -> str:
        """会話のコンテキストを構築"""
        context_parts = []
        
        if context:
            context_parts.append(context)
        
        if history:
            last_message = history[-1]
            context_parts.append(
                f"{last_message.get('speaker_name', '相手')}が「{last_message.get('text', '')}」と言いました。"
            )
        
        # 場所に応じたコンテキストを追加
        location_context = self._get_location_context(location)
        context_parts.append(location_context)
        
        time_context = self._get_time_context()
        if time_context:
            context_parts.append(time_context)
        
        return " ".join(context_parts)
    
    def _get_location_context(self, location: str) -> str:
        """場所に応じたコンテキストを生成"""
        return self.location_service.build_location_context(location)
    
    def _get_time_context(self) -> str:
        """時間帯に応じたコンテキスト"""
        hour = datetime.now().hour
        
        if 17 <= hour < 19:
            return "夕方で、空がオレンジ色に染まっています。"
        elif 19 <= hour < 22:
            return "夜になり、提灯の明かりが綺麗です。"
        elif 22 <= hour or hour < 5:
            return "深夜で、お祭りも終わりに近づいています。"
        else:
            return "昼間で、お祭りの準備が進んでいます。"
    
    def _detect_emotion(self, text: str) -> str:
        """テキストから感情を推定"""
        if any(word in text for word in ['嬉しい', '楽しい', 'わぁ', '♪']):
            return 'happy'
        elif any(word in text for word in ['悲しい', '寂しい', 'つらい']):
            return 'sad'
        elif any(word in text for word in ['怒', 'むか', 'イライラ']):
            return 'angry'
        elif any(word in text for word in ['びっくり', 'え？', '！？']):
            return 'surprised'
        else:
            return 'neutral'
    
    def _generate_fallback_response(self, agent_id: str, turn: int) -> Dict:
        """エラー時のフォールバック応答"""
        return {
            "speaker": agent_id,
            "speaker_name": self.agents.get(agent_id, {}).get('name', 'Unknown'),
            "text": "そうですね...",
            "emotion": "neutral",
            "turn": turn,
            "timestamp": datetime.now().isoformat()
        }