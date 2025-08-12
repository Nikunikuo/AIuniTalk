import json
import logging
from typing import Dict, List, Optional
from datetime import datetime

logger = logging.getLogger(__name__)

class LocationService:
    def __init__(self):
        self.locations = self._load_locations()
        
    def _load_locations(self) -> Dict:
        """場所設定を読み込み"""
        try:
            with open('config/locations.json', 'r', encoding='utf-8') as f:
                data = json.load(f)
                return {loc['id']: loc for loc in data['locations']}
        except FileNotFoundError:
            logger.warning("locations.json not found, using default locations")
            return self._get_default_locations()
        except Exception as e:
            logger.error(f"Failed to load locations: {e}")
            return self._get_default_locations()
    
    def _get_default_locations(self) -> Dict:
        """デフォルトの場所設定"""
        return {
            "central_plaza": {
                "id": "central_plaza",
                "display_name": "中央広場",
                "context_description": "お祭りの中央広場にいます。周りには様々な屋台があり、人々の楽しそうな声が聞こえます。",
                "topics": ["賑やか", "人々", "屋台", "お祭り"],
                "atmosphere": "活気に満ちている"
            }
        }
    
    def get_location_by_waypoint_name(self, waypoint_name: str) -> Optional[Dict]:
        """ウェイポイント名から場所情報を取得"""
        waypoint_name = waypoint_name.lower()
        
        for location_id, location in self.locations.items():
            keywords = location.get('waypoint_keywords', [])
            for keyword in keywords:
                if keyword.lower() in waypoint_name:
                    return location
        
        # デフォルト
        return self.locations.get('central_plaza')
    
    def get_location_by_display_name(self, display_name: str) -> Optional[Dict]:
        """表示名から場所情報を取得"""
        for location_id, location in self.locations.items():
            if location.get('display_name') == display_name:
                return location
        return None
    
    def build_location_context(self, location_name: str) -> str:
        """場所に応じた詳細なコンテキストを生成"""
        
        # 場所情報を取得
        location = self.get_location_by_display_name(location_name)
        if not location:
            location = self.get_location_by_waypoint_name(location_name)
        
        if not location:
            return f"場所は{location_name}です。"
        
        context_parts = []
        
        # 基本の場所説明
        context_parts.append(location['context_description'])
        
        # 音の情報
        sounds = location.get('sounds', [])
        if sounds:
            context_parts.append(f"周りからは{' 、'.join(sounds)}が聞こえます。")
        
        # 匂いの情報
        smells = location.get('smells', [])
        if smells:
            context_parts.append(f"{' 、'.join(smells)}。")
        
        return " ".join(context_parts)
    
    def get_time_of_day(self) -> str:
        """現在の時間帯を取得"""
        hour = datetime.now().hour
        
        if 6 <= hour < 17:
            return "afternoon"
        elif 17 <= hour < 19:
            return "evening"  
        else:
            return "night"
    
    def get_location_topics(self, location_name: str) -> List[str]:
        """場所に応じた話題リストを取得"""
        location = self.get_location_by_display_name(location_name)
        if location:
            return location.get('topics', [])
        return []
    
    def get_all_locations(self) -> Dict:
        """全ての場所情報を取得"""
        return self.locations
    
    def reload_locations(self):
        """場所設定を再読み込み（設定変更後用）"""
        logger.info("Reloading location configuration...")
        self.locations = self._load_locations()
        logger.info(f"Loaded {len(self.locations)} locations")
    
    def validate_location_config(self) -> List[str]:
        """設定ファイルの妥当性をチェック"""
        issues = []
        
        for location_id, location in self.locations.items():
            # 必須フィールドのチェック
            required_fields = ['display_name', 'context_description']
            for field in required_fields:
                if not location.get(field):
                    issues.append(f"Location '{location_id}' missing required field: {field}")
            
            # waypoint_keywordsのチェック
            if not location.get('waypoint_keywords'):
                issues.append(f"Location '{location_id}' has no waypoint_keywords")
        
        return issues