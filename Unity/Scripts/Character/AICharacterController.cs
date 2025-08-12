using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AIuniTalk.Network;
using AIuniTalk.Dialog;

namespace AIuniTalk.Character
{
    public class AICharacterController : MonoBehaviour
    {
        [Header("Character Settings")]
        [SerializeField] private string characterId = "miku";
        [SerializeField] private string characterName = "ミク";
        [SerializeField] private float baseWalkSpeed = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        
        [Header("Movement")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointReachDistance = 0.5f;
        [SerializeField] private float idleTimeMin = 2f;
        [SerializeField] private float idleTimeMax = 5f;
        
        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private LayerMask characterLayer;
        [SerializeField] private float conversationCooldown = 30f;
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        
        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public bool IsInConversation { get; private set; }
        public AICharacterController ConversationPartner { get; private set; }
        
        private Agent agentData;
        private Transform currentTarget;
        private int currentWaypointIndex;
        private float walkSpeed;
        private bool isMoving;
        private float lastConversationTime;
        private Vector3 originalPosition;
        
        private void Start()
        {
            originalPosition = transform.position;
            walkSpeed = baseWalkSpeed;
            
            if (waypoints == null || waypoints.Length == 0)
            {
                CreateDefaultWaypoints();
            }
            
            SelectRandomWaypoint();
            StartCoroutine(MovementLoop());
            
            ServerConnection.Instance.OnAgentsConfigReceived += OnAgentsConfigLoaded;
        }
        
        private void CreateDefaultWaypoints()
        {
            GameObject waypointParent = new GameObject($"{characterName}_Waypoints");
            waypoints = new Transform[5];
            
            for (int i = 0; i < waypoints.Length; i++)
            {
                GameObject wp = new GameObject($"Waypoint_{i}");
                wp.transform.parent = waypointParent.transform;
                wp.transform.position = originalPosition + UnityEngine.Random.insideUnitSphere * 10f;
                wp.transform.position = new Vector3(
                    wp.transform.position.x,
                    originalPosition.y,
                    wp.transform.position.z
                );
                waypoints[i] = wp.transform;
            }
        }
        
        private void OnAgentsConfigLoaded(AgentConfig config)
        {
            foreach (var agent in config.agents)
            {
                if (agent.id == characterId)
                {
                    agentData = agent;
                    walkSpeed = baseWalkSpeed * agent.walking_speed;
                    Debug.Log($"Loaded config for {characterName}: speed={walkSpeed}");
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
                    if (currentTarget != null)
                    {
                        MoveTowardsTarget();
                        
                        if (Vector3.Distance(transform.position, currentTarget.position) < waypointReachDistance)
                        {
                            float idleTime = UnityEngine.Random.Range(idleTimeMin, idleTimeMax);
                            SetMoving(false);
                            yield return new WaitForSeconds(idleTime);
                            SelectRandomWaypoint();
                        }
                    }
                    
                    CheckForNearbyCharacters();
                }
                
                yield return null;
            }
        }
        
        private void MoveTowardsTarget()
        {
            if (currentTarget == null) return;
            
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0;
            
            transform.position += direction * walkSpeed * Time.deltaTime;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            SetMoving(true);
        }
        
        private void SelectRandomWaypoint()
        {
            if (waypoints.Length == 0) return;
            
            int newIndex = UnityEngine.Random.Range(0, waypoints.Length);
            while (newIndex == currentWaypointIndex && waypoints.Length > 1)
            {
                newIndex = UnityEngine.Random.Range(0, waypoints.Length);
            }
            
            currentWaypointIndex = newIndex;
            currentTarget = waypoints[currentWaypointIndex];
        }
        
        private void CheckForNearbyCharacters()
        {
            if (Time.time - lastConversationTime < conversationCooldown) return;
            
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius, characterLayer);
            
            foreach (var collider in nearbyColliders)
            {
                if (collider.gameObject == gameObject) continue;
                
                AICharacterController otherCharacter = collider.GetComponent<AICharacterController>();
                if (otherCharacter != null && !otherCharacter.IsInConversation && !IsInConversation)
                {
                    float distance = Vector3.Distance(transform.position, otherCharacter.transform.position);
                    if (distance < interactionRadius)
                    {
                        StartConversation(otherCharacter);
                        break;
                    }
                }
            }
        }
        
        public void StartConversation(AICharacterController partner)
        {
            if (IsInConversation || partner.IsInConversation) return;
            
            IsInConversation = true;
            ConversationPartner = partner;
            partner.IsInConversation = true;
            partner.ConversationPartner = this;
            
            SetMoving(false);
            partner.SetMoving(false);
            
            Vector3 directionToPartner = (partner.transform.position - transform.position).normalized;
            directionToPartner.y = 0;
            transform.rotation = Quaternion.LookRotation(directionToPartner);
            
            Vector3 directionToThis = -directionToPartner;
            partner.transform.rotation = Quaternion.LookRotation(directionToThis);
            
            DialogManager dialogManager = FindObjectOfType<DialogManager>();
            if (dialogManager != null)
            {
                dialogManager.StartDialog(this, partner);
            }
            
            Debug.Log($"Conversation started between {characterName} and {partner.characterName}");
        }
        
        public void EndConversation()
        {
            if (ConversationPartner != null)
            {
                ConversationPartner.IsInConversation = false;
                ConversationPartner.ConversationPartner = null;
            }
            
            IsInConversation = false;
            ConversationPartner = null;
            lastConversationTime = Time.time;
            
            SelectRandomWaypoint();
            Debug.Log($"{characterName} ended conversation");
        }
        
        private void SetMoving(bool moving)
        {
            isMoving = moving;
            if (animator != null)
            {
                animator.SetBool("IsWalking", moving);
                animator.SetBool("IsTalking", IsInConversation);
            }
        }
        
        public void ShowEmotion(string emotion)
        {
            if (animator != null)
            {
                animator.SetTrigger($"Emotion_{emotion}");
            }
            
            Debug.Log($"{characterName} shows emotion: {emotion}");
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
            
            if (waypoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var waypoint in waypoints)
                {
                    if (waypoint != null)
                    {
                        Gizmos.DrawWireSphere(waypoint.position, 0.5f);
                    }
                }
            }
            
            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
    }
}