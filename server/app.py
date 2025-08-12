import os
import json
import logging
from datetime import datetime
from flask import Flask, request, jsonify
from flask_socketio import SocketIO, emit
from flask_cors import CORS
from dotenv import load_dotenv
import orjson

load_dotenv()

app = Flask(__name__)
app.config['SECRET_KEY'] = os.getenv('SECRET_KEY', 'dev-secret-key-change-in-production')
CORS(app, resources={r"/*": {"origins": "*"}})
socketio = SocketIO(app, cors_allowed_origins="*", async_mode='threading')

os.makedirs('logs', exist_ok=True)
logging.basicConfig(
    level=getattr(logging, os.getenv('LOG_LEVEL', 'INFO')),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('logs/app.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

from services.dialog_service import DialogService
from services.llm_service import LLMService

dialog_service = DialogService()
llm_service = LLMService()

active_sessions = {}

@app.route('/healthz', methods=['GET'])
def health_check():
    """ヘルスチェックエンドポイント"""
    return jsonify({
        'status': 'healthy',
        'timestamp': datetime.now().isoformat(),
        'online_mode': os.getenv('ONLINE', 'true') == 'true',
        'active_sessions': len(active_sessions)
    })

@app.route('/config/agents', methods=['GET'])
def get_agents():
    """キャラクター設定を取得"""
    try:
        with open('config/agents.json', 'r', encoding='utf-8') as f:
            agents = json.load(f)
        return jsonify(agents)
    except Exception as e:
        logger.error(f"Failed to load agents config: {e}")
        return jsonify({'error': 'Failed to load agents configuration'}), 500

@app.route('/dialog/turn', methods=['POST'])
def generate_dialog_turn():
    """会話の1ターンを生成"""
    try:
        data = request.json
        agent_ids = data.get('agent_ids', [])
        turn = data.get('turn', 1)
        context = data.get('context', '')
        location = data.get('location', '夏祭り会場')
        
        if len(agent_ids) < 2:
            return jsonify({'error': 'At least 2 agents required'}), 400
        
        session_id = f"{'-'.join(agent_ids)}_{datetime.now().timestamp()}"
        
        if session_id not in active_sessions:
            active_sessions[session_id] = {
                'agents': agent_ids,
                'history': [],
                'turn': 0
            }
        
        session = active_sessions[session_id]
        session['turn'] = turn
        
        response = dialog_service.generate_turn(
            agent_ids=agent_ids,
            turn=turn,
            context=context,
            location=location,
            history=session['history']
        )
        
        session['history'].append(response)
        
        socketio.emit('dialog_update', {
            'session_id': session_id,
            'response': response
        })
        
        logger.info(f"Generated dialog turn {turn} for session {session_id}")
        return jsonify(response)
        
    except Exception as e:
        logger.error(f"Failed to generate dialog turn: {e}")
        return jsonify({'error': str(e)}), 500

@app.route('/dialog/reset', methods=['POST'])
def reset_dialog():
    """会話セッションをリセット"""
    data = request.json
    session_id = data.get('session_id')
    
    if session_id in active_sessions:
        del active_sessions[session_id]
        logger.info(f"Reset session: {session_id}")
    
    return jsonify({'status': 'reset', 'session_id': session_id})

@socketio.on('connect')
def handle_connect():
    """WebSocket接続時"""
    logger.info(f"Client connected: {request.sid}")
    emit('connected', {'data': 'Connected to server'})

@socketio.on('disconnect')
def handle_disconnect():
    """WebSocket切断時"""
    logger.info(f"Client disconnected: {request.sid}")

@socketio.on('proximity_detected')
def handle_proximity(data):
    """キャラクター近接検知"""
    agent_ids = data.get('agent_ids', [])
    distance = data.get('distance', 0)
    
    logger.info(f"Proximity detected: {agent_ids} at distance {distance}")
    
    if distance < 2.0:
        emit('start_conversation', {
            'agent_ids': agent_ids,
            'trigger': 'proximity'
        }, broadcast=True)

@socketio.on('conversation_ended')
def handle_conversation_end(data):
    """会話終了通知"""
    session_id = data.get('session_id')
    if session_id in active_sessions:
        del active_sessions[session_id]
    
    logger.info(f"Conversation ended: {session_id}")
    emit('agents_separate', {'session_id': session_id}, broadcast=True)

if __name__ == '__main__':
    os.makedirs('logs', exist_ok=True)
    os.makedirs('data', exist_ok=True)
    
    port = int(os.getenv('PORT', 5000))
    debug = os.getenv('DEBUG', 'true') == 'true'
    
    logger.info(f"Starting server on port {port}, debug={debug}")
    socketio.run(app, host='0.0.0.0', port=port, debug=debug)