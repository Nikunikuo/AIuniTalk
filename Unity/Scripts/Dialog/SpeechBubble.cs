using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIuniTalk.Dialog
{
    public class SpeechBubble : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI textComponent;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float scaleAnimationDuration = 0.2f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Transform targetCharacter;
        private float heightOffset;
        private Camera mainCamera;
        private RectTransform rectTransform;
        private Coroutine currentAnimation;
        
        private void Awake()
        {
            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent == null)
                {
                    CreateDefaultComponents();
                }
            }
            
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            rectTransform = GetComponent<RectTransform>();
            mainCamera = Camera.main;
            
            canvasGroup.alpha = 0;
            transform.localScale = Vector3.zero;
        }
        
        private void CreateDefaultComponents()
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(transform);
            backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(1, 1, 1, 0.95f);
            
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(200, 80);
            bgRect.anchoredPosition = Vector2.zero;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(transform);
            textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.1f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        public void Initialize(Transform character, float height)
        {
            targetCharacter = character;
            heightOffset = height;
        }
        
        public void SetText(string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
                AdjustBubbleSize();
            }
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
            
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentAnimation = StartCoroutine(ShowAnimation());
        }
        
        public void Hide()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            currentAnimation = StartCoroutine(HideAnimation());
        }
        
        private IEnumerator ShowAnimation()
        {
            float elapsed = 0;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                
                canvasGroup.alpha = Mathf.Lerp(0, 1, t);
                
                float scaleT = scaleCurve.Evaluate(t);
                transform.localScale = Vector3.one * scaleT;
                
                yield return null;
            }
            
            canvasGroup.alpha = 1;
            transform.localScale = Vector3.one;
        }
        
        private IEnumerator HideAnimation()
        {
            float elapsed = 0;
            float startAlpha = canvasGroup.alpha;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
                
                float scaleT = 1 - scaleCurve.Evaluate(t);
                transform.localScale = startScale * scaleT;
                
                yield return null;
            }
            
            canvasGroup.alpha = 0;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
        
        private void Update()
        {
            if (targetCharacter != null && mainCamera != null)
            {
                UpdatePosition();
            }
        }
        
        private void UpdatePosition()
        {
            Vector3 worldPosition = targetCharacter.position + Vector3.up * heightOffset;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            
            if (screenPosition.z > 0)
            {
                rectTransform.position = screenPosition;
                
                if (!gameObject.activeSelf && canvasGroup.alpha > 0)
                {
                    gameObject.SetActive(true);
                }
            }
            else
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }
        }
        
        private void AdjustBubbleSize()
        {
            if (textComponent == null || backgroundImage == null) return;
            
            float textWidth = textComponent.preferredWidth;
            float textHeight = textComponent.preferredHeight;
            
            float padding = 20f;
            float minWidth = 100f;
            float maxWidth = 300f;
            
            float bubbleWidth = Mathf.Clamp(textWidth + padding * 2, minWidth, maxWidth);
            float bubbleHeight = textHeight + padding * 2;
            
            RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
        }
        
        public void SetStyle(Color backgroundColor, Color textColor)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
            
            if (textComponent != null)
            {
                textComponent.color = textColor;
            }
        }
    }
}