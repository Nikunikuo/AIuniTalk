#!/usr/bin/env python
"""
超簡単なワンライナーテスト
"""

import requests
import json

def quick_test():
    url = "http://localhost:5000"
    
    # 1. ヘルス
    print("1. Health Check...")
    r = requests.get(f"{url}/healthz")
    print(f"   Status: {r.status_code} - {r.json() if r.status_code == 200 else 'Failed'}")
    
    # 2. キャラ一覧
    print("\n2. Characters...")
    r = requests.get(f"{url}/config/agents")
    if r.status_code == 200:
        agents = r.json()['agents']
        for a in agents:
            print(f"   {a['name']} ({a['id']})")
    
    # 3. 1回だけ会話テスト
    print("\n3. Dialog Test...")
    payload = {
        "agent_ids": ["alpha", "beta"],  # 変更後の名前
        "turn": 1,
        "context": "夏祭りで出会った",
        "location": "たこ焼き屋台"
    }
    r = requests.post(f"{url}/dialog/turn", json=payload)
    if r.status_code == 200:
        result = r.json()
        print(f"   💬 {result['speaker_name']}: {result['text']} [{result['emotion']}]")
    else:
        print(f"   Failed: {r.status_code}")

if __name__ == "__main__":
    quick_test()