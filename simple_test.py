#!/usr/bin/env python
"""
シンプルなサーバーテスト（Unicode問題回避版）
"""

import requests
import json

def test_server():
    url = "http://localhost:5000"
    
    print("=== AIuniTalk Server Test ===")
    
    # 1. ヘルスチェック
    print("\n1. Health Check...")
    try:
        r = requests.get(f"{url}/healthz")
        if r.status_code == 200:
            data = r.json()
            print(f"   OK: {data['status']}, Online: {data['online_mode']}")
        else:
            print(f"   FAILED: {r.status_code}")
    except Exception as e:
        print(f"   ERROR: {e}")
    
    # 2. キャラクター設定
    print("\n2. Characters...")
    try:
        r = requests.get(f"{url}/config/agents")
        if r.status_code == 200:
            agents = r.json()['agents']
            for a in agents:
                print(f"   ID: {a['id']}, Name: {a['name']}")
                print(f"      Personality: {a['personality'][:30]}...")
        else:
            print(f"   FAILED: {r.status_code}")
    except Exception as e:
        print(f"   ERROR: {e}")
    
    # 3. 会話テスト
    print("\n3. Dialog Test...")
    payload = {
        "agent_ids": ["alpha", "beta"],
        "turn": 1,
        "context": "夏祭りで出会った",
        "location": "たこ焼き屋台"
    }
    
    try:
        r = requests.post(f"{url}/dialog/turn", json=payload, timeout=15)
        if r.status_code == 200:
            result = r.json()
            print(f"   Speaker: {result['speaker_name']}")
            print(f"   Text: {result['text']}")
            print(f"   Emotion: {result['emotion']}")
            print("   SUCCESS!")
        else:
            print(f"   FAILED: {r.status_code}")
            print(f"   Response: {r.text}")
    except Exception as e:
        print(f"   ERROR: {e}")
    
    # 4. 複数ターンテスト
    print("\n4. Multi-turn Test...")
    for turn in range(1, 4):
        try:
            payload['turn'] = turn
            r = requests.post(f"{url}/dialog/turn", json=payload, timeout=15)
            if r.status_code == 200:
                result = r.json()
                print(f"   Turn {turn}: {result['speaker_name']} says: {result['text']}")
            else:
                print(f"   Turn {turn} FAILED")
                break
        except Exception as e:
            print(f"   Turn {turn} ERROR: {e}")
            break
    
    print("\n=== Test Complete ===")

if __name__ == "__main__":
    test_server()