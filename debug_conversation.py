#!/usr/bin/env python
"""
Unity無しでAI会話をデバッグするスクリプト
リアルな会話フローをシミュレート
"""

import requests
import json
import time
import random
from colorama import init, Fore, Style

init(autoreset=True)
SERVER_URL = "http://localhost:5000"

def print_dialog(speaker_name, text, emotion):
    """会話を見やすく表示"""
    color = {
        "happy": Fore.GREEN,
        "sad": Fore.BLUE, 
        "angry": Fore.RED,
        "surprised": Fore.YELLOW,
        "neutral": Fore.WHITE
    }.get(emotion, Fore.WHITE)
    
    print(f"{color}💬 {speaker_name}: {text} [{emotion}]{Style.RESET_ALL}")

def simulate_conversation():
    """リアルな会話をシミュレート"""
    print(f"\n{Fore.CYAN}🎭 AI Character Conversation Simulator{Style.RESET_ALL}")
    print("=" * 50)
    
    # キャラクター設定を取得
    try:
        response = requests.get(f"{SERVER_URL}/config/agents")
        if response.status_code != 200:
            print(f"{Fore.RED}❌ Cannot get character config{Style.RESET_ALL}")
            return
        
        agents_data = response.json()
        agents = agents_data['agents']
        print(f"{Fore.GREEN}✅ Loaded {len(agents)} characters{Style.RESET_ALL}")
        
        for agent in agents:
            print(f"  - {agent['name']} ({agent['personality']})")
        
    except Exception as e:
        print(f"{Fore.RED}❌ Error: {e}{Style.RESET_ALL}")
        return
    
    print("\n" + "=" * 50)
    
    # ランダムに2人選択
    char1, char2 = random.sample(agents, 2)
    print(f"{Fore.YELLOW}🎬 Starting conversation between:")
    print(f"  {char1['name']} vs {char2['name']}{Style.RESET_ALL}")
    
    locations = ["たこ焼き屋台", "金魚すくい", "射的", "わたあめ屋台", "花火観覧スポット"]
    location = random.choice(locations)
    print(f"{Fore.CYAN}📍 Location: {location}{Style.RESET_ALL}\n")
    
    agent_ids = [char1['id'], char2['id']]
    context_history = []
    
    # 6ターンの会話
    for turn in range(1, 7):
        print(f"{Fore.MAGENTA}--- Turn {turn} ---{Style.RESET_ALL}")
        
        # コンテキスト作成
        if context_history:
            context = " ".join(context_history[-2:])  # 直近2つの発言
        else:
            context = f"{location}で偶然出会いました"
        
        # API呼び出し
        request_data = {
            "agent_ids": agent_ids,
            "turn": turn,
            "context": context,
            "location": location
        }
        
        try:
            response = requests.post(
                f"{SERVER_URL}/dialog/turn",
                json=request_data,
                timeout=15
            )
            
            if response.status_code == 200:
                dialog = response.json()
                speaker_name = dialog['speaker_name']
                text = dialog['text']
                emotion = dialog['emotion']
                
                print_dialog(speaker_name, text, emotion)
                context_history.append(f"{speaker_name}「{text}」")
                
                # Unity風の待機時間
                time.sleep(2)
            else:
                print(f"{Fore.RED}❌ Turn {turn} failed: {response.status_code}{Style.RESET_ALL}")
                print(f"Response: {response.text}")
                break
                
        except Exception as e:
            print(f"{Fore.RED}❌ Error at turn {turn}: {e}{Style.RESET_ALL}")
            break
    
    print(f"\n{Fore.GREEN}🎉 Conversation completed!{Style.RESET_ALL}")
    print("=" * 50)

def interactive_mode():
    """インタラクティブモード"""
    print(f"\n{Fore.BLUE}🔧 Interactive Debug Mode{Style.RESET_ALL}")
    print("Commands: 'talk', 'agents', 'health', 'quit'")
    
    while True:
        cmd = input(f"\n{Fore.CYAN}> {Style.RESET_ALL}").strip().lower()
        
        if cmd == 'quit':
            break
        elif cmd == 'talk':
            simulate_conversation()
        elif cmd == 'agents':
            show_agents()
        elif cmd == 'health':
            check_health()
        else:
            print("Unknown command. Use: talk, agents, health, quit")

def show_agents():
    """エージェント一覧表示"""
    try:
        response = requests.get(f"{SERVER_URL}/config/agents")
        if response.status_code == 200:
            data = response.json()
            print(f"\n{Fore.GREEN}📋 Character List:{Style.RESET_ALL}")
            for agent in data['agents']:
                print(f"  ID: {agent['id']}")
                print(f"  Name: {agent['name']}")
                print(f"  Personality: {agent['personality']}")
                print(f"  Style: {agent['speaking_style']}")
                print(f"  Speed: {agent['walking_speed']}x")
                print(f"  Topics: {', '.join(agent['topics'])}")
                print()
        else:
            print(f"{Fore.RED}Failed to get agents{Style.RESET_ALL}")
    except Exception as e:
        print(f"{Fore.RED}Error: {e}{Style.RESET_ALL}")

def check_health():
    """ヘルスチェック"""
    try:
        response = requests.get(f"{SERVER_URL}/healthz")
        if response.status_code == 200:
            data = response.json()
            print(f"\n{Fore.GREEN}💚 Server Status:{Style.RESET_ALL}")
            print(f"  Status: {data['status']}")
            print(f"  Online Mode: {data['online_mode']}")
            print(f"  Active Sessions: {data['active_sessions']}")
            print(f"  Timestamp: {data['timestamp']}")
        else:
            print(f"{Fore.RED}Health check failed{Style.RESET_ALL}")
    except Exception as e:
        print(f"{Fore.RED}Cannot connect to server: {e}{Style.RESET_ALL}")

if __name__ == "__main__":
    print(f"{Fore.YELLOW}🚀 AIuniTalk Debug Console{Style.RESET_ALL}")
    
    # 接続確認
    try:
        response = requests.get(f"{SERVER_URL}/healthz", timeout=3)
        if response.status_code == 200:
            print(f"{Fore.GREEN}✅ Server is running{Style.RESET_ALL}")
        else:
            print(f"{Fore.RED}❌ Server returned {response.status_code}{Style.RESET_ALL}")
            exit(1)
    except:
        print(f"{Fore.RED}❌ Server not running. Start with: python server/app.py{Style.RESET_ALL}")
        exit(1)
    
    # メニュー表示
    print("\nOptions:")
    print("1. Run automatic conversation simulation")
    print("2. Interactive debug mode")
    choice = input("\nChoice (1 or 2): ").strip()
    
    if choice == "1":
        simulate_conversation()
    elif choice == "2":
        interactive_mode()
    else:
        print("Invalid choice")