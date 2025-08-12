#!/usr/bin/env python
"""
本当にAIかどうか確認用テスト
"""

import requests
import json

def proof_test():
    url = "http://localhost:5000"
    
    print("=== AI Proof Test ===")
    
    # 変な質問でAIの反応を確認
    weird_requests = [
        {
            "agent_ids": ["alpha", "beta"],
            "turn": 1,
            "context": "突然宇宙人が現れました",
            "location": "火星"
        },
        {
            "agent_ids": ["alpha", "gamma"],
            "turn": 1,
            "context": "魔法使いになりました",
            "location": "魔法学校"
        },
        {
            "agent_ids": ["beta", "gamma"],
            "turn": 1,
            "context": "時間が逆行しています",
            "location": "タイムマシン内"
        }
    ]
    
    for i, payload in enumerate(weird_requests, 1):
        print(f"\n{i}. Weird Test - Context: {payload['context']}")
        try:
            r = requests.post(f"{url}/dialog/turn", json=payload, timeout=15)
            if r.status_code == 200:
                result = r.json()
                print(f"   {result['speaker_name']}: {result['text']}")
                print(f"   Emotion: {result['emotion']}")
            else:
                print(f"   Failed: {r.status_code}")
        except Exception as e:
            print(f"   Error: {e}")

if __name__ == "__main__":
    proof_test()