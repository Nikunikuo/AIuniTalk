import os
import logging
import random
from typing import Dict, List, Optional
from openai import OpenAI
from dotenv import load_dotenv

load_dotenv()
logger = logging.getLogger(__name__)

class LLMService:
    def __init__(self):
        self.online_mode = os.getenv('ONLINE', 'true') == 'true'
        self.client = None
        
        if self.online_mode:
            api_key = os.getenv('OPENAI_API_KEY')
            if api_key and api_key != 'your-api-key-here':
                try:
                    self.client = OpenAI(api_key=api_key)
                    logger.info("OpenAI client initialized successfully")
                except Exception as e:
                    logger.error(f"Failed to initialize OpenAI client: {e}")
                    self.online_mode = False
            else:
                logger.warning("No valid API key found, switching to offline mode")
                self.online_mode = False
        else:
            logger.info("Running in offline mode")
    
    def generate_response(
        self,
        agent_data: Dict,
        context: str,
        history: List[Dict],
        location: str = "夏祭り会場"
    ) -> str:
        """AIキャラクターの応答を生成"""
        
        if not self.online_mode:
            return self._generate_offline_response(agent_data, location)
        
        try:
            system_prompt = self._create_system_prompt(agent_data, location)
            messages = self._prepare_messages(system_prompt, context, history)
            
            response = self.client.chat.completions.create(
                model="gpt-4o-mini",
                messages=messages,
                max_tokens=100,
                temperature=0.8,
                presence_penalty=0.6,
                frequency_penalty=0.3
            )
            
            text = response.choices[0].message.content.strip()
            
            if len(text) > 40:
                text = text[:40] + "..."
            
            logger.info(f"Generated response for {agent_data['name']}: {text}")
            return text
            
        except Exception as e:
            logger.error(f"Failed to generate response: {e}")
            return self._generate_offline_response(agent_data, location)
    
    def _create_system_prompt(self, agent_data: Dict, location: str) -> str:
        """システムプロンプトを作成"""
        topics = ', '.join(agent_data.get('topics', ['夏祭り']))
        
        return f"""あなたは「{agent_data['name']}」というキャラクターです。

性格：{agent_data.get('personality', '明るく元気')}
口調：{agent_data.get('speaking_style', 'です・ます調')}
好きな話題：{topics}
現在地：{location}

ルール：
1. 15〜40文字の短い返答をする
2. 自然な会話を心がける
3. 夏祭りの雰囲気を大切にする
4. キャラクターの個性を表現する
5. 相手の発言に適切に反応する"""
    
    def _prepare_messages(
        self,
        system_prompt: str,
        context: str,
        history: List[Dict]
    ) -> List[Dict]:
        """ChatGPT用のメッセージリストを準備"""
        messages = [{"role": "system", "content": system_prompt}]
        
        for h in history[-6:]:
            messages.append({
                "role": "assistant" if h.get('is_self') else "user",
                "content": h.get('text', '')
            })
        
        if context:
            messages.append({"role": "user", "content": context})
        
        return messages
    
    def _generate_offline_response(self, agent_data: Dict, location: str) -> str:
        """オフラインモード用のテンプレート応答"""
        templates = [
            f"わぁ、{location}は賑やかですね！",
            "屋台がたくさんあって楽しいです！",
            "花火が楽しみですね〜",
            "浴衣、とても似合ってますよ！",
            "たこ焼き食べたいなぁ...",
            "金魚すくい、やってみます？",
            "今日は涼しくていいですね",
            "お祭りの音楽が聞こえてきます♪",
            "りんご飴が美味しそう！",
            "一緒に回りましょうか？"
        ]
        
        response = random.choice(templates)
        
        if agent_data.get('speaking_style') == 'タメ口':
            response = response.replace('ですね', 'だね').replace('ます', 'るよ')
        
        return response