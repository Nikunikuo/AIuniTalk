#!/usr/bin/env python
"""
AIuniTalkサーバーテストスクリプト
このスクリプトを実行してサーバーの動作を確認します
"""

import requests
import json
import time
import sys
from colorama import init, Fore, Style

init(autoreset=True)

SERVER_URL = "http://localhost:5000"

def print_success(message):
    print(f"{Fore.GREEN}✓ {message}{Style.RESET_ALL}")

def print_error(message):
    print(f"{Fore.RED}✗ {message}{Style.RESET_ALL}")

def print_info(message):
    print(f"{Fore.CYAN}ℹ {message}{Style.RESET_ALL}")

def test_health_check():
    """ヘルスチェックのテスト"""
    print_info("Testing health check...")
    try:
        response = requests.get(f"{SERVER_URL}/healthz", timeout=5)
        if response.status_code == 200:
            data = response.json()
            print_success(f"Server is healthy: {data}")
            return True
        else:
            print_error(f"Health check failed: {response.status_code}")
            return False
    except requests.exceptions.ConnectionError:
        print_error("Cannot connect to server. Is it running?")
        print_info("Start server with: python server/app.py")
        return False
    except Exception as e:
        print_error(f"Error: {e}")
        return False

def test_get_agents():
    """エージェント設定取得のテスト"""
    print_info("Testing agent configuration...")
    try:
        response = requests.get(f"{SERVER_URL}/config/agents", timeout=5)
        if response.status_code == 200:
            data = response.json()
            print_success(f"Loaded {len(data['agents'])} agents:")
            for agent in data['agents']:
                print(f"  - {agent['name']} ({agent['id']}): {agent['personality']}")
            return True
        else:
            print_error(f"Failed to get agents: {response.status_code}")
            return False
    except Exception as e:
        print_error(f"Error: {e}")
        return False

def test_dialog_generation():
    """会話生成のテスト"""
    print_info("Testing dialog generation...")
    
    request_data = {
        "agent_ids": ["miku", "rin"],
        "turn": 1,
        "context": "夏祭りで出会いました",
        "location": "たこ焼き屋台"
    }
    
    try:
        print_info(f"Request: {json.dumps(request_data, ensure_ascii=False)}")
        
        response = requests.post(
            f"{SERVER_URL}/dialog/turn",
            json=request_data,
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            print_success("Dialog generated successfully:")
            print(f"  Speaker: {data.get('speaker_name', 'Unknown')}")
            print(f"  Text: {data.get('text', '')}")
            print(f"  Emotion: {data.get('emotion', 'neutral')}")
            return True
        else:
            print_error(f"Failed to generate dialog: {response.status_code}")
            print(f"Response: {response.text}")
            return False
    except Exception as e:
        print_error(f"Error: {e}")
        return False

def test_multiple_turns():
    """複数ターンの会話テスト"""
    print_info("Testing multiple turn conversation...")
    
    agent_ids = ["miku", "rin", "len"]
    history = []
    
    for turn in range(1, 7):
        print_info(f"Turn {turn}/6")
        
        context = "夏祭りで楽しく過ごしています" if not history else history[-1]
        
        request_data = {
            "agent_ids": agent_ids[:2] if turn <= 3 else agent_ids,
            "turn": turn,
            "context": context,
            "location": "花火観覧スポット"
        }
        
        try:
            response = requests.post(
                f"{SERVER_URL}/dialog/turn",
                json=request_data,
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                message = f"{data.get('speaker_name', '?')}: {data.get('text', '')}"
                history.append(message)
                print(f"  {message}")
                time.sleep(1)
            else:
                print_error(f"Failed at turn {turn}")
                return False
                
        except Exception as e:
            print_error(f"Error at turn {turn}: {e}")
            return False
    
    print_success("Multi-turn conversation completed!")
    return True

def test_session_reset():
    """セッションリセットのテスト"""
    print_info("Testing session reset...")
    
    request_data = {
        "session_id": "test_session_123"
    }
    
    try:
        response = requests.post(
            f"{SERVER_URL}/dialog/reset",
            json=request_data,
            timeout=5
        )
        
        if response.status_code == 200:
            print_success("Session reset successful")
            return True
        else:
            print_error(f"Failed to reset session: {response.status_code}")
            return False
    except Exception as e:
        print_error(f"Error: {e}")
        return False

def main():
    print(f"\n{Fore.YELLOW}{'='*50}")
    print(f"{Fore.YELLOW}AIuniTalk Server Test Suite")
    print(f"{Fore.YELLOW}{'='*50}{Style.RESET_ALL}\n")
    
    tests = [
        ("Health Check", test_health_check),
        ("Get Agents", test_get_agents),
        ("Dialog Generation", test_dialog_generation),
        ("Multiple Turns", test_multiple_turns),
        ("Session Reset", test_session_reset),
    ]
    
    passed = 0
    failed = 0
    
    for test_name, test_func in tests:
        print(f"\n{Fore.BLUE}[{test_name}]{Style.RESET_ALL}")
        if test_func():
            passed += 1
        else:
            failed += 1
        print()
    
    print(f"\n{Fore.YELLOW}{'='*50}")
    print(f"Results: {Fore.GREEN}{passed} passed{Style.RESET_ALL}, ", end="")
    print(f"{Fore.RED if failed > 0 else Fore.GREEN}{failed} failed{Style.RESET_ALL}")
    print(f"{Fore.YELLOW}{'='*50}{Style.RESET_ALL}\n")
    
    if failed == 0:
        print_success("All tests passed! Server is working correctly.")
        return 0
    else:
        print_error(f"{failed} test(s) failed. Please check the server logs.")
        return 1

if __name__ == "__main__":
    sys.exit(main())