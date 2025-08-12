using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AIuniTalk.Network;
using AIuniTalk.Dialog;

namespace AIuniTalk.Character
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AdvancedCharacterController : MonoBehaviour
    {
        [Header("Character Settings")]
        [SerializeField] private string characterId = "alpha";
        [SerializeField] private string characterName = "アルファ";
        [SerializeField] private float baseWalkSpeed = 2.8f;      // 32インチ用（少しゆっくり観察しやすく）
        [SerializeField] private float personalSpaceRadius = 1.2f; // 32インチ用（密度アップで活気演出）
        
        [Header("Festival Waypoints")]
        [SerializeField] private Transform[] festivalWaypoints; // お祭り会場のポイント
        [SerializeField] private float waypointReachDistance = 1f;
        [SerializeField] private float idleTimeMin = 2f;          // 32インチ用（短めで活発に）
        [SerializeField] private float idleTimeMax = 6f;          // 32インチ用（短めで活発に）
        
        [Header("Interaction")]
        [SerializeField] private float conversationRadius = 2.0f; // 32インチ用（標準的な距離）
        [SerializeField] private LayerMask characterLayer = -1;
        [SerializeField] private float conversationCooldown = 30f;
        
        [Header("Avoidance")]
        [SerializeField] private float obstacleAvoidanceRadius = 2f;
        [SerializeField] private float characterAvoidanceRadius = 2f;
        [SerializeField] private LayerMask obstacleLayer = -1;
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        
        // Components
        private NavMeshAgent navAgent;
        private Rigidbody rb;
        private SphereCollider personalSpaceCollider;
        
        // State
        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public bool IsInConversation { get; private set; }
        public AdvancedCharacterController ConversationPartner { get; private set; }
        
        private Agent agentData;
        private Transform currentTarget;
        private int currentWaypointIndex = -1;
        private float lastConversationTime;
        private bool isWaitingAtWaypoint;
        private List<AdvancedCharacterController> nearbyCharacters = new List<AdvancedCharacterController>();
        
        // Manager references
        private static List<AdvancedCharacterController> allCharacters = new List<AdvancedCharacterController>();
        
        private void Awake()
        {
            SetupComponents();
            allCharacters.Add(this);
        }
        
        private void Start()
        {
            SetupNavMeshAgent();
            StartCoroutine(MovementLoop());
            ServerConnection.Instance.OnAgentsConfigReceived += OnAgentsConfigLoaded;
            
            // 初期ウェイポイント設定
            if (festivalWaypoints == null || festivalWaypoints.Length == 0)
            {
                FindFestivalWaypoints();
            }
            
            SelectRandomWaypoint();
        }
        
        private void SetupComponents()
        {
            // NavMeshAgent設定
            navAgent = GetComponent<NavMeshAgent>();
            
            // 個人スペース用コライダー
            personalSpaceCollider = gameObject.AddComponent<SphereCollider>();
            personalSpaceCollider.radius = personalSpaceRadius;
            personalSpaceCollider.isTrigger = true;
            
            // Rigidbody（軽量設定）
            if (!GetComponent<Rigidbody>())
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb = GetComponent<Rigidbody>();
            }
        }
        
        private void SetupNavMeshAgent()
        {
            navAgent.speed = baseWalkSpeed;
            navAgent.acceleration = 8f;
            navAgent.angularSpeed = 120f;
            navAgent.stoppingDistance = waypointReachDistance;
            navAgent.autoBraking = true;
            navAgent.avoidancePriority = Random.Range(10, 90); // 回避優先度をランダム化
        }
        
        private void FindFestivalWaypoints()
        {
            // "FestivalWaypoint" タグのオブジェクトを自動検索
            GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("FestivalWaypoint");
            
            if (waypointObjects.Length > 0)
            {
                festivalWaypoints = new Transform[waypointObjects.Length];
                for (int i = 0; i < waypointObjects.Length; i++)
                {
                    festivalWaypoints[i] = waypointObjects[i].transform;
                }
                Debug.Log($"Found {festivalWaypoints.Length} festival waypoints for {characterName}");
            }
            else
            {
                // フォールバック：手動でウェイポイント作成
                CreateDefaultFestivalWaypoints();
            }
        }
        
        private void CreateDefaultFestivalWaypoints()
        {
            GameObject waypointParent = new GameObject($"Festival_Waypoints_{characterName}");
            List<Transform> waypoints = new List<Transform>();
            
            // お祭り会場っぽい配置（屋台を想定）
            Vector3[] festivalPositions = {
                new Vector3(0, 0, 0),      // 中央広場
                new Vector3(-8, 0, 5),     // たこ焼き屋台
                new Vector3(8, 0, 5),      // わたあめ屋台
                new Vector3(-5, 0, -8),    // 金魚すくい
                new Vector3(5, 0, -8),     // 射的
                new Vector3(0, 0, 10),     // ステージ前
                new Vector3(-10, 0, -2),   // 休憩所
                new Vector3(10, 0, -2),    // ドリンク屋台
                new Vector3(0, 0, -15),    // 花火観覧スポット
            };
            
            for (int i = 0; i < festivalPositions.Length; i++)
            {
                GameObject wp = new GameObject($"Waypoint_Festival_{i}");
                wp.transform.parent = waypointParent.transform;
                wp.transform.position = transform.position + festivalPositions[i];
                wp.tag = "FestivalWaypoint";
                waypoints.Add(wp.transform);
            }
            
            festivalWaypoints = waypoints.ToArray();
            Debug.Log($"Created {festivalWaypoints.Length} default festival waypoints for {characterName}");
        }
        
        private void OnAgentsConfigLoaded(AgentConfig config)
        {
            foreach (var agent in config.agents)
            {
                if (agent.id == characterId)
                {
                    agentData = agent;
                    float speedMultiplier = agent.walking_speed;
                    navAgent.speed = baseWalkSpeed * speedMultiplier;
                    Debug.Log($"Loaded config for {characterName}: speed={navAgent.speed:F1}");
                    break;
                }
            }
        }
        
        private IEnumerator MovementLoop()
        {
            while (true)
            {
                if (!IsInConversation)
                {
                    HandleMovement();
                    CheckForNearbyCharacters();
                }
                
                yield return new WaitForSeconds(0.1f); // 軽量化のため0.1秒間隔
            }
        }
        
        private void HandleMovement()
        {
            // 目標地点に到達したかチェック
            if (currentTarget != null && !navAgent.pathPending)
            {
                if (navAgent.remainingDistance < waypointReachDistance)
                {
                    if (!isWaitingAtWaypoint)
                    {
                        StartCoroutine(WaitAtWaypoint());
                    }
                }
            }
            
            // 他キャラとの距離チェック（重なり防止）
            AvoidOtherCharacters();
            
            // アニメーション更新
            UpdateAnimation();
        }
        
        private IEnumerator WaitAtWaypoint()
        {
            isWaitingAtWaypoint = true;
            navAgent.isStopped = true;
            
            float waitTime = Random.Range(idleTimeMin, idleTimeMax);
            Debug.Log($"{characterName} waiting at waypoint for {waitTime:F1} seconds");
            
            yield return new WaitForSeconds(waitTime);
            
            SelectRandomWaypoint();
            isWaitingAtWaypoint = false;
        }
        
        private void SelectRandomWaypoint()
        {
            if (festivalWaypoints == null || festivalWaypoints.Length == 0) return;
            
            int attempts = 0;
            int newIndex;
            
            do
            {
                newIndex = Random.Range(0, festivalWaypoints.Length);
                attempts++;
            }
            while (newIndex == currentWaypointIndex && attempts < 5);
            
            currentWaypointIndex = newIndex;
            currentTarget = festivalWaypoints[currentWaypointIndex];
            
            // NavMeshAgentで移動開始
            navAgent.isStopped = false;
            navAgent.SetDestination(currentTarget.position);
            
            Debug.Log($"{characterName} heading to waypoint {currentWaypointIndex}: {currentTarget.name}");
        }
        
        private void AvoidOtherCharacters()
        {
            foreach (var other in allCharacters)
            {
                if (other == this || other.IsInConversation) continue;
                
                float distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance < characterAvoidanceRadius && distance > 0.1f)
                {
                    // 他キャラが近すぎる場合、少し迂回
                    Vector3 avoidDirection = (transform.position - other.transform.position).normalized;
                    Vector3 avoidPosition = transform.position + avoidDirection * 0.5f;
                    
                    // NavMeshで有効な位置かチェック
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(avoidPosition, out hit, 2f, NavMesh.AllAreas))
                    {
                        navAgent.SetDestination(hit.position);
                    }
                }
            }
        }
        
        private void CheckForNearbyCharacters()
        {
            if (Time.time - lastConversationTime < conversationCooldown) return;
            
            foreach (var other in allCharacters)
            {
                if (other == this || other.IsInConversation || IsInConversation) continue;
                
                float distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance < conversationRadius)
                {
                    StartConversation(other);
                    break;
                }
            }
        }
        
        public void StartConversation(AdvancedCharacterController partner)
        {
            if (IsInConversation || partner.IsInConversation) return;
            
            // 移動停止
            navAgent.isStopped = true;
            partner.navAgent.isStopped = true;
            
            // 会話状態設定
            IsInConversation = true;
            ConversationPartner = partner;
            partner.IsInConversation = true;
            partner.ConversationPartner = this;
            
            // 向き合う
            Vector3 directionToPartner = (partner.transform.position - transform.position).normalized;
            transform.LookAt(partner.transform.position);
            partner.transform.LookAt(transform.position);
            
            // DialogManager に通知
            DialogManager dialogManager = FindObjectOfType<DialogManager>();
            if (dialogManager != null)
            {
                dialogManager.StartDialog(this, partner);
            }
            
            Debug.Log($"Conversation started: {characterName} ↔ {partner.characterName}");
        }
        
        public void EndConversation()
        {
            if (ConversationPartner != null)
            {
                ConversationPartner.IsInConversation = false;
                ConversationPartner.ConversationPartner = null;
                ConversationPartner.navAgent.isStopped = false;
            }
            
            IsInConversation = false;
            ConversationPartner = null;
            lastConversationTime = Time.time;
            navAgent.isStopped = false;
            
            // 新しい目標地点を選択
            StartCoroutine(DelayedWaypointSelection());
            
            Debug.Log($"{characterName} ended conversation");
        }
        
        private IEnumerator DelayedWaypointSelection()
        {
            yield return new WaitForSeconds(2f); // 少し待ってから移動再開
            SelectRandomWaypoint();
        }
        
        private void UpdateAnimation()
        {
            if (animator == null) return;
            
            bool isMoving = navAgent.velocity.magnitude > 0.1f && !IsInConversation;
            animator.SetBool("IsWalking", isMoving);
            animator.SetBool("IsTalking", IsInConversation);
            animator.SetFloat("Speed", navAgent.velocity.magnitude);
        }
        
        public void ShowEmotion(string emotion)
        {
            if (animator != null)
            {
                animator.SetTrigger($"Emotion_{emotion}");
            }
            Debug.Log($"{characterName} shows emotion: {emotion}");
        }
        
        // トリガーイベント（個人スペース）
        private void OnTriggerEnter(Collider other)
        {
            var otherChar = other.GetComponent<AdvancedCharacterController>();
            if (otherChar != null && !nearbyCharacters.Contains(otherChar))
            {
                nearbyCharacters.Add(otherChar);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            var otherChar = other.GetComponent<AdvancedCharacterController>();
            if (otherChar != null && nearbyCharacters.Contains(otherChar))
            {
                nearbyCharacters.Remove(otherChar);
            }
        }
        
        private void OnDestroy()
        {
            allCharacters.Remove(this);
        }
        
        // デバッグ表示
        private void OnDrawGizmosSelected()
        {
            // 会話範囲
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, conversationRadius);
            
            // 個人スペース
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, personalSpaceRadius);
            
            // キャラ回避範囲
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, characterAvoidanceRadius);
            
            // 現在の目標
            if (currentTarget != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, currentTarget.position);
                Gizmos.DrawWireSphere(currentTarget.position, 1f);
            }
            
            // NavMesh経路
            if (navAgent != null && navAgent.hasPath)
            {
                Gizmos.color = Color.magenta;
                Vector3[] corners = navAgent.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
        }
    }
}