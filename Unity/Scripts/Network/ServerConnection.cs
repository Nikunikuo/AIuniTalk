using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AIuniTalk.Network
{
    public class ServerConnection : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "http://localhost:5000";
        [SerializeField] private float connectionTimeout = 10f;
        
        public static ServerConnection Instance { get; private set; }
        
        public event Action<bool> OnConnectionStatusChanged;
        public event Action<DialogResponse> OnDialogReceived;
        public event Action<AgentConfig> OnAgentsConfigReceived;
        
        private bool isConnected = false;
        private Coroutine healthCheckCoroutine;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartHealthCheck();
            StartCoroutine(LoadAgentsConfig());
        }
        
        private void StartHealthCheck()
        {
            if (healthCheckCoroutine != null)
            {
                StopCoroutine(healthCheckCoroutine);
            }
            healthCheckCoroutine = StartCoroutine(HealthCheckLoop());
        }
        
        private IEnumerator HealthCheckLoop()
        {
            while (true)
            {
                yield return StartCoroutine(CheckServerHealth());
                yield return new WaitForSeconds(5f);
            }
        }
        
        private IEnumerator CheckServerHealth()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/healthz"))
            {
                request.timeout = (int)connectionTimeout;
                yield return request.SendWebRequest();
                
                bool wasConnected = isConnected;
                isConnected = request.result == UnityWebRequest.Result.Success;
                
                if (wasConnected != isConnected)
                {
                    Debug.Log($"Server connection status changed: {isConnected}");
                    OnConnectionStatusChanged?.Invoke(isConnected);
                }
                
                if (!isConnected && request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Server health check failed: {request.error}");
                }
            }
        }
        
        public IEnumerator LoadAgentsConfig()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/config/agents"))
            {
                request.timeout = (int)connectionTimeout;
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    AgentConfig config = JsonUtility.FromJson<AgentConfig>(json);
                    OnAgentsConfigReceived?.Invoke(config);
                    Debug.Log("Agents config loaded successfully");
                }
                else
                {
                    Debug.LogError($"Failed to load agents config: {request.error}");
                }
            }
        }
        
        public void RequestDialog(string[] agentIds, int turn, string context, string location)
        {
            StartCoroutine(RequestDialogCoroutine(agentIds, turn, context, location));
        }
        
        private IEnumerator RequestDialogCoroutine(string[] agentIds, int turn, string context, string location)
        {
            DialogRequest requestData = new DialogRequest
            {
                agent_ids = agentIds,
                turn = turn,
                context = context,
                location = location
            };
            
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/dialog/turn", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)connectionTimeout;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = request.downloadHandler.text;
                    DialogResponse response = JsonUtility.FromJson<DialogResponse>(responseJson);
                    OnDialogReceived?.Invoke(response);
                    Debug.Log($"Dialog received: {response.speaker_name}: {response.text}");
                }
                else
                {
                    Debug.LogError($"Failed to get dialog: {request.error}");
                }
            }
        }
        
        public void ResetSession(string sessionId)
        {
            StartCoroutine(ResetSessionCoroutine(sessionId));
        }
        
        private IEnumerator ResetSessionCoroutine(string sessionId)
        {
            var requestData = new { session_id = sessionId };
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/dialog/reset", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Session reset: {sessionId}");
                }
            }
        }
        
        private void OnDestroy()
        {
            if (healthCheckCoroutine != null)
            {
                StopCoroutine(healthCheckCoroutine);
            }
        }
    }
    
    [Serializable]
    public class DialogRequest
    {
        public string[] agent_ids;
        public int turn;
        public string context;
        public string location;
    }
    
    [Serializable]
    public class DialogResponse
    {
        public string speaker;
        public string speaker_name;
        public string text;
        public string emotion;
        public int turn;
        public string timestamp;
    }
    
    [Serializable]
    public class AgentConfig
    {
        public Agent[] agents;
        public ConversationRules conversation_rules;
        public Location[] locations;
    }
    
    [Serializable]
    public class Agent
    {
        public string id;
        public string name;
        public string personality;
        public string speaking_style;
        public float walking_speed;
        public string color;
        public string[] topics;
        public string[] greeting_patterns;
        public string[] idle_actions;
    }
    
    [Serializable]
    public class ConversationRules
    {
        public int max_turns;
        public float turn_duration;
        public float proximity_radius;
        public float separation_distance;
        public float conversation_cooldown;
    }
    
    [Serializable]
    public class Location
    {
        public string id;
        public string name;
        public string[] topics;
    }
}