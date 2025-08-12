using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AIuniTalk.Network;

namespace AIuniTalk.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private bool autoStartServer = true;
        [SerializeField] private string pythonPath = "python";
        [SerializeField] private string serverScriptPath = "server/app.py";
        
        [Header("Environment")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Gradient dayNightGradient;
        [SerializeField] private float dayNightCycleDuration = 300f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private GUIStyle debugStyle;
        
        private float currentTimeOfDay = 0.7f;
        private bool isServerRunning = false;
        private System.Diagnostics.Process serverProcess;
        
        private static GameManager instance;
        public static GameManager Instance => instance;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            
            if (autoStartServer)
            {
                StartPythonServer();
            }
            
            StartCoroutine(DayNightCycle());
        }
        
        private void StartPythonServer()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            try
            {
                serverProcess = new System.Diagnostics.Process();
                serverProcess.StartInfo.FileName = pythonPath;
                serverProcess.StartInfo.Arguments = serverScriptPath;
                serverProcess.StartInfo.UseShellExecute = false;
                serverProcess.StartInfo.RedirectStandardOutput = true;
                serverProcess.StartInfo.RedirectStandardError = true;
                serverProcess.StartInfo.CreateNoWindow = true;
                
                serverProcess.OutputDataReceived += (sender, args) => Debug.Log($"[Server]: {args.Data}");
                serverProcess.ErrorDataReceived += (sender, args) => Debug.LogError($"[Server Error]: {args.Data}");
                
                serverProcess.Start();
                serverProcess.BeginOutputReadLine();
                serverProcess.BeginErrorReadLine();
                
                isServerRunning = true;
                Debug.Log("Python server started");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start Python server: {e.Message}");
                Debug.LogWarning("Please start the server manually: python server/app.py");
            }
#else
            Debug.LogWarning("Auto-start server is only available in Editor and Windows Standalone");
#endif
        }
        
        private IEnumerator DayNightCycle()
        {
            while (true)
            {
                currentTimeOfDay += Time.deltaTime / dayNightCycleDuration;
                if (currentTimeOfDay > 1f) currentTimeOfDay -= 1f;
                
                UpdateLighting();
                
                yield return null;
            }
        }
        
        private void UpdateLighting()
        {
            if (directionalLight != null)
            {
                float sunAngle = Mathf.Lerp(30f, 150f, currentTimeOfDay);
                directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
                
                if (dayNightGradient != null)
                {
                    directionalLight.color = dayNightGradient.Evaluate(currentTimeOfDay);
                }
                
                float intensity = Mathf.Clamp01(Mathf.Cos((currentTimeOfDay - 0.5f) * 2f * Mathf.PI) * 0.5f + 0.5f);
                directionalLight.intensity = Mathf.Lerp(0.3f, 1.2f, intensity);
            }
            
            RenderSettings.ambientIntensity = Mathf.Lerp(0.4f, 1f, GetDayProgress());
        }
        
        private float GetDayProgress()
        {
            if (currentTimeOfDay < 0.25f || currentTimeOfDay > 0.75f)
                return 0f;
            else if (currentTimeOfDay < 0.5f)
                return (currentTimeOfDay - 0.25f) * 4f;
            else
                return 1f - (currentTimeOfDay - 0.5f) * 4f;
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ResetSystem();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleTimeScale();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                showDebugInfo = !showDebugInfo;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMenu();
            }
        }
        
        private void ResetSystem()
        {
            Debug.Log("System Reset");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        private void ToggleTimeScale()
        {
            if (Time.timeScale == 1f)
            {
                Time.timeScale = 2f;
                Debug.Log("Time Scale: 2x");
            }
            else if (Time.timeScale == 2f)
            {
                Time.timeScale = 0.5f;
                Debug.Log("Time Scale: 0.5x");
            }
            else
            {
                Time.timeScale = 1f;
                Debug.Log("Time Scale: 1x");
            }
        }
        
        private void ShowMenu()
        {
            Debug.Log("Menu (not implemented)");
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.box);
                debugStyle.normal.background = Texture2D.whiteTexture;
                debugStyle.normal.textColor = Color.white;
                debugStyle.fontSize = 12;
                debugStyle.padding = new RectOffset(10, 10, 10, 10);
            }
            
            GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical(debugStyle);
            
            GUILayout.Label($"FPS: {1f / Time.deltaTime:F1}");
            GUILayout.Label($"Time Scale: {Time.timeScale}x");
            GUILayout.Label($"Time of Day: {currentTimeOfDay * 24f:F1}:00");
            GUILayout.Label($"Server: {(isServerRunning ? "Running" : "Not Started")}");
            
            var connection = ServerConnection.Instance;
            if (connection != null)
            {
                GUILayout.Label($"Connection: {(connection.enabled ? "Connected" : "Disconnected")}");
            }
            
            GUILayout.Label($"\nControls:");
            GUILayout.Label("F1: Reset System");
            GUILayout.Label("F2: Toggle Time Scale");
            GUILayout.Label("F3: Toggle Debug Info");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnApplicationQuit()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                try
                {
                    serverProcess.Kill();
                    serverProcess.Dispose();
                    Debug.Log("Python server stopped");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error stopping server: {e.Message}");
                }
            }
        }
    }
}