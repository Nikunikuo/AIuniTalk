using System.Collections.Generic;
using UnityEngine;

namespace AIuniTalk.Core
{
    public class FestivalWaypointManager : MonoBehaviour
    {
        [Header("Festival Layout")]
        [SerializeField] private Transform festivalCenter;
        [SerializeField] private float festivalRadius = 12f;  // 32インチディスプレイ用（コンパクト）
        
        [Header("Waypoint Settings")]
        [SerializeField] private int waypointCount = 7;      // 32インチディスプレイ用（適度な密度）
        [SerializeField] private GameObject waypointPrefab;
        [SerializeField] private bool autoCreateWaypoints = true;
        
        [Header("Festival Areas")]
        [SerializeField] private FestivalArea[] festivalAreas;
        
        private List<Transform> allWaypoints = new List<Transform>();
        
        [System.Serializable]
        public class FestivalArea
        {
            public string areaName;
            public Vector3 localPosition;
            public float radius = 2f;
            public string[] associatedTopics;
            public Color debugColor = Color.white;
        }
        
        private void Start()
        {
            if (festivalCenter == null)
            {
                festivalCenter = transform;
            }
            
            if (autoCreateWaypoints)
            {
                CreateFestivalWaypoints();
            }
            else
            {
                CollectExistingWaypoints();
            }
        }
        
        private void CreateFestivalWaypoints()
        {
            // 既存のウェイポイントをクリア
            ClearExistingWaypoints();
            
            GameObject waypointParent = new GameObject("Festival_Waypoints");
            waypointParent.transform.parent = transform;
            
            // 定義されたエリアがある場合はそれを使用
            if (festivalAreas != null && festivalAreas.Length > 0)
            {
                CreateAreaBasedWaypoints(waypointParent);
            }
            else
            {
                CreateDefaultFestivalLayout(waypointParent);
            }
            
            Debug.Log($"Created {allWaypoints.Count} festival waypoints");
        }
        
        private void CreateAreaBasedWaypoints(GameObject parent)
        {
            for (int i = 0; i < festivalAreas.Length; i++)
            {
                var area = festivalAreas[i];
                Vector3 worldPosition = festivalCenter.position + area.localPosition;
                
                GameObject waypoint = CreateWaypoint(parent, $"Waypoint_{area.areaName}", worldPosition, i);
                
                // エリア情報をコンポーネントに追加
                var areaInfo = waypoint.AddComponent<FestivalAreaInfo>();
                areaInfo.areaName = area.areaName;
                areaInfo.associatedTopics = area.associatedTopics;
                areaInfo.radius = area.radius;
            }
        }
        
        private void CreateDefaultFestivalLayout(GameObject parent)
        {
            // 32インチディスプレイ用のコンパクトレイアウト
            FestivalArea[] defaultAreas = {
                new FestivalArea { areaName = "Central_Plaza", localPosition = Vector3.zero, associatedTopics = new[]{"夏祭り", "賑やか"} },
                new FestivalArea { areaName = "Takoyaki_Stand", localPosition = new Vector3(-6, 0, 4), associatedTopics = new[]{"たこ焼き", "ソース"} },
                new FestivalArea { areaName = "Cotton_Candy", localPosition = new Vector3(6, 0, 4), associatedTopics = new[]{"わたあめ", "甘い"} },
                new FestivalArea { areaName = "Goldfish_Scooping", localPosition = new Vector3(-5, 0, -6), associatedTopics = new[]{"金魚すくい", "ポイ"} },
                new FestivalArea { areaName = "Shooting_Gallery", localPosition = new Vector3(5, 0, -6), associatedTopics = new[]{"射的", "景品"} },
                new FestivalArea { areaName = "Stage_Front", localPosition = new Vector3(0, 0, 8), associatedTopics = new[]{"ステージ", "音楽"} },
                new FestivalArea { areaName = "Fireworks_Spot", localPosition = new Vector3(0, 0, -10), associatedTopics = new[]{"花火", "夜空"} }
            };
            
            for (int i = 0; i < defaultAreas.Length && i < waypointCount; i++)
            {
                var area = defaultAreas[i];
                Vector3 worldPosition = festivalCenter.position + area.localPosition;
                
                GameObject waypoint = CreateWaypoint(parent, $"Waypoint_{area.areaName}", worldPosition, i);
                
                var areaInfo = waypoint.AddComponent<FestivalAreaInfo>();
                areaInfo.areaName = area.areaName;
                areaInfo.associatedTopics = area.associatedTopics;
                areaInfo.radius = 2f;
            }
        }
        
        private GameObject CreateWaypoint(GameObject parent, string name, Vector3 position, int index)
        {
            GameObject waypoint;
            
            if (waypointPrefab != null)
            {
                waypoint = Instantiate(waypointPrefab, position, Quaternion.identity, parent.transform);
                waypoint.name = name;
            }
            else
            {
                waypoint = new GameObject(name);
                waypoint.transform.parent = parent.transform;
                waypoint.transform.position = position;
                
                // デフォルトの見た目
                var renderer = waypoint.AddComponent<MeshRenderer>();
                var meshFilter = waypoint.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateSphereMesh();
                
                var material = new Material(Shader.Find("Standard"));
                material.color = GetAreaColor(index);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.5f);
                renderer.material = material;
                
                waypoint.transform.localScale = Vector3.one * 0.5f;
            }
            
            waypoint.tag = "FestivalWaypoint";
            waypoint.layer = LayerMask.NameToLayer("Default");
            
            allWaypoints.Add(waypoint.transform);
            return waypoint;
        }
        
        private void CollectExistingWaypoints()
        {
            GameObject[] existingWaypoints = GameObject.FindGameObjectsWithTag("FestivalWaypoint");
            allWaypoints.Clear();
            
            foreach (var wp in existingWaypoints)
            {
                allWaypoints.Add(wp.transform);
            }
            
            Debug.Log($"Found {allWaypoints.Count} existing festival waypoints");
        }
        
        private void ClearExistingWaypoints()
        {
            GameObject[] existingWaypoints = GameObject.FindGameObjectsWithTag("FestivalWaypoint");
            for (int i = existingWaypoints.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingWaypoints[i]);
                }
                else
                {
                    DestroyImmediate(existingWaypoints[i]);
                }
            }
            allWaypoints.Clear();
        }
        
        private Mesh CreateSphereMesh()
        {
            // Unity組み込みのSphere meshを取得
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(sphere);
            return mesh;
        }
        
        private Color GetAreaColor(int index)
        {
            Color[] colors = {
                Color.red, Color.blue, Color.green, Color.yellow,
                Color.magenta, Color.cyan, Color.white, new Color(1f, 0.5f, 0f), // オレンジ
                new Color(0.5f, 0f, 1f) // 紫
            };
            return colors[index % colors.Length];
        }
        
        public Transform GetRandomWaypoint()
        {
            if (allWaypoints.Count == 0) return null;
            return allWaypoints[Random.Range(0, allWaypoints.Count)];
        }
        
        public Transform GetWaypointByArea(string areaName)
        {
            foreach (var waypoint in allWaypoints)
            {
                var areaInfo = waypoint.GetComponent<FestivalAreaInfo>();
                if (areaInfo != null && areaInfo.areaName == areaName)
                {
                    return waypoint;
                }
            }
            return null;
        }
        
        public List<Transform> GetAllWaypoints()
        {
            return new List<Transform>(allWaypoints);
        }
        
        // エディタ用
        private void OnDrawGizmos()
        {
            if (festivalCenter == null) return;
            
            // 祭り全体の範囲
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(festivalCenter.position, festivalRadius);
            
            // エリア表示
            if (festivalAreas != null)
            {
                foreach (var area in festivalAreas)
                {
                    Gizmos.color = area.debugColor;
                    Vector3 worldPos = festivalCenter.position + area.localPosition;
                    Gizmos.DrawWireSphere(worldPos, area.radius);
                    
                    // エリア名表示（Unityエディタでのみ）
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(worldPos + Vector3.up * 2, area.areaName);
                    #endif
                }
            }
        }
        
        // エディタ用ボタン
        [ContextMenu("Recreate Waypoints")]
        public void RecreateWaypoints()
        {
            CreateFestivalWaypoints();
        }
        
        [ContextMenu("Clear All Waypoints")]
        public void ClearWaypoints()
        {
            ClearExistingWaypoints();
        }
    }
    
    // ウェイポイントのエリア情報
    public class FestivalAreaInfo : MonoBehaviour
    {
        public string areaName;
        public string[] associatedTopics;
        public float radius = 2f;
    }
}