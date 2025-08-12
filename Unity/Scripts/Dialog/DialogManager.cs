using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AIuniTalk.Character;
using AIuniTalk.Network;

namespace AIuniTalk.Dialog
{
    public class DialogManager : MonoBehaviour
    {
        [Header("Dialog Settings")]
        [SerializeField] private int maxTurns = 6;
        [SerializeField] private float turnDuration = 3f;
        [SerializeField] private float dialogEndDelay = 2f;
        
        [Header("UI References")]
        [SerializeField] private GameObject speechBubblePrefab;
        [SerializeField] private Transform uiCanvas;
        [SerializeField] private float bubbleHeight = 2.5f;
        
        private Dictionary<string, SpeechBubble> activeBubbles = new Dictionary<string, SpeechBubble>();
        private List<DialogSession> activeSessions = new List<DialogSession>();
        
        private void Start()
        {
            if (speechBubblePrefab == null)
            {
                Debug.LogWarning("Speech bubble prefab not assigned, creating default");
                CreateDefaultSpeechBubblePrefab();
            }
            
            ServerConnection.Instance.OnDialogReceived += OnDialogReceived;
        }
        
        public void StartDialog(AICharacterController character1, AICharacterController character2)
        {
            string sessionId = $"{character1.CharacterId}_{character2.CharacterId}_{Time.time}";
            
            DialogSession session = new DialogSession
            {
                sessionId = sessionId,
                character1 = character1,
                character2 = character2,
                currentTurn = 0,
                maxTurns = maxTurns
            };
            
            activeSessions.Add(session);
            
            ShowSpeechBubble(character1);
            ShowSpeechBubble(character2);
            
            StartCoroutine(ManageDialog(session));
        }
        
        private IEnumerator ManageDialog(DialogSession session)
        {
            string[] agentIds = { session.character1.CharacterId, session.character2.CharacterId };
            List<string> conversationHistory = new List<string>();
            
            for (int turn = 1; turn <= session.maxTurns; turn++)
            {
                session.currentTurn = turn;
                
                string context = conversationHistory.Count > 0 
                    ? string.Join(" ", conversationHistory) 
                    : "夏祭りで出会いました。";
                
                string location = GetNearestLocation(session.character1.transform.position);
                
                ServerConnection.Instance.RequestDialog(agentIds, turn, context, location);
                
                yield return new WaitForSeconds(turnDuration);
            }
            
            yield return new WaitForSeconds(dialogEndDelay);
            
            EndDialog(session);
        }
        
        private void OnDialogReceived(DialogResponse response)
        {
            AICharacterController speaker = FindCharacterById(response.speaker);
            if (speaker != null)
            {
                UpdateSpeechBubble(speaker, response.text);
                speaker.ShowEmotion(response.emotion);
                
                HideOtherBubbles(response.speaker);
            }
        }
        
        private void ShowSpeechBubble(AICharacterController character)
        {
            if (!activeBubbles.ContainsKey(character.CharacterId))
            {
                GameObject bubbleObj = Instantiate(speechBubblePrefab, uiCanvas);
                SpeechBubble bubble = bubbleObj.GetComponent<SpeechBubble>();
                
                if (bubble == null)
                {
                    bubble = bubbleObj.AddComponent<SpeechBubble>();
                }
                
                bubble.Initialize(character.transform, bubbleHeight);
                activeBubbles[character.CharacterId] = bubble;
            }
            
            activeBubbles[character.CharacterId].Show();
        }
        
        private void UpdateSpeechBubble(AICharacterController character, string text)
        {
            if (activeBubbles.ContainsKey(character.CharacterId))
            {
                activeBubbles[character.CharacterId].SetText(text);
                activeBubbles[character.CharacterId].Show();
            }
        }
        
        private void HideSpeechBubble(AICharacterController character)
        {
            if (activeBubbles.ContainsKey(character.CharacterId))
            {
                activeBubbles[character.CharacterId].Hide();
            }
        }
        
        private void HideOtherBubbles(string speakerId)
        {
            foreach (var kvp in activeBubbles)
            {
                if (kvp.Key != speakerId)
                {
                    kvp.Value.Hide();
                }
            }
        }
        
        private void EndDialog(DialogSession session)
        {
            HideSpeechBubble(session.character1);
            HideSpeechBubble(session.character2);
            
            session.character1.EndConversation();
            session.character2.EndConversation();
            
            activeSessions.Remove(session);
            
            ServerConnection.Instance.ResetSession(session.sessionId);
        }
        
        private AICharacterController FindCharacterById(string characterId)
        {
            AICharacterController[] allCharacters = FindObjectsOfType<AICharacterController>();
            foreach (var character in allCharacters)
            {
                if (character.CharacterId == characterId)
                {
                    return character;
                }
            }
            return null;
        }
        
        private string GetNearestLocation(Vector3 position)
        {
            // FestivalWaypointManagerから場所情報を取得
            var waypointManager = FindObjectOfType<FestivalWaypointManager>();
            if (waypointManager != null)
            {
                var allWaypoints = waypointManager.GetAllWaypoints();
                
                Transform nearestWaypoint = null;
                float nearestDistance = float.MaxValue;
                
                foreach (Transform waypoint in allWaypoints)
                {
                    if (waypoint == null) continue;
                    
                    float distance = Vector3.Distance(position, waypoint.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestWaypoint = waypoint;
                    }
                }
                
                // 最も近いウェイポイントの場所情報を取得
                if (nearestWaypoint != null)
                {
                    var areaInfo = nearestWaypoint.GetComponent<FestivalAreaInfo>();
                    if (areaInfo != null)
                    {
                        return areaInfo.areaName;
                    }
                    else
                    {
                        // ウェイポイント名から場所を推定
                        return ConvertWaypointNameToLocation(nearestWaypoint.name);
                    }
                }
            }
            
            // フォールバック
            return "夏祭り会場";
        }
        
        private string ConvertWaypointNameToLocation(string waypointName)
        {
            // ウェイポイント名から日本語の場所名に変換
            if (waypointName.Contains("Takoyaki")) return "たこ焼き屋台";
            if (waypointName.Contains("Cotton_Candy")) return "わたあめ屋台";
            if (waypointName.Contains("Goldfish")) return "金魚すくい";
            if (waypointName.Contains("Shooting")) return "射的";
            if (waypointName.Contains("Stage")) return "ステージ前";
            if (waypointName.Contains("Rest")) return "休憩所";
            if (waypointName.Contains("Drink")) return "ドリンク屋台";
            if (waypointName.Contains("Fireworks")) return "花火観覧スポット";
            if (waypointName.Contains("Central")) return "中央広場";
            
            return "夏祭り会場";
        }
        
        private void CreateDefaultSpeechBubblePrefab()
        {
            GameObject prefab = new GameObject("SpeechBubble");
            SpeechBubble bubble = prefab.AddComponent<SpeechBubble>();
            speechBubblePrefab = prefab;
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ResetAllDialogs();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                ForceStartRandomDialog();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                ToggleDebugInfo();
            }
        }
        
        private void ResetAllDialogs()
        {
            foreach (var session in activeSessions.ToArray())
            {
                EndDialog(session);
            }
            Debug.Log("All dialogs reset");
        }
        
        private void ForceStartRandomDialog()
        {
            AICharacterController[] characters = FindObjectsOfType<AICharacterController>();
            if (characters.Length >= 2)
            {
                int idx1 = UnityEngine.Random.Range(0, characters.Length);
                int idx2 = UnityEngine.Random.Range(0, characters.Length);
                while (idx2 == idx1)
                {
                    idx2 = UnityEngine.Random.Range(0, characters.Length);
                }
                
                characters[idx1].StartConversation(characters[idx2]);
                Debug.Log($"Forced dialog between {characters[idx1].CharacterName} and {characters[idx2].CharacterName}");
            }
        }
        
        private void ToggleDebugInfo()
        {
            Debug.Log($"Active sessions: {activeSessions.Count}");
            Debug.Log($"Active bubbles: {activeBubbles.Count}");
            foreach (var session in activeSessions)
            {
                Debug.Log($"Session: {session.sessionId}, Turn: {session.currentTurn}/{session.maxTurns}");
            }
        }
    }
    
    [Serializable]
    public class DialogSession
    {
        public string sessionId;
        public AICharacterController character1;
        public AICharacterController character2;
        public int currentTurn;
        public int maxTurns;
    }
}