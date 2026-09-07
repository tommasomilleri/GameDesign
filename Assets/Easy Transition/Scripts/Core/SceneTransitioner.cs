namespace PixeLadder.EasyTransition
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine.Events;

    [DisallowMultipleComponent]
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance { get; private set; }

        [Header("Canvas Configuration")]
        [Tooltip("The UI Image prefab that will be instantiated to cover the screen.")]
        [SerializeField] private Image transitionImagePrefab;

        [Tooltip("Use ScreenSpaceOverlay for standard screens. Use ScreenSpaceCamera for VR.")]
        [SerializeField] private RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;

        [Tooltip("Required if RenderMode is ScreenSpaceCamera. Leave empty to automatically use Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("The sorting order of the Canvas. Ensure this is higher than your game's UI.")]
        [SerializeField] private int canvasSortingOrder = 999;

        [Tooltip("If true, automatically creates a CanvasGroup to block all player raycasts/clicks during the transition.")]
        [SerializeField] private bool blockUIInteraction = true;

        [Header("Default Settings")]
        [Tooltip("The effect used if a transition is called without explicitly passing one.")]
        [SerializeField] private TransitionEffect defaultTransition;

        [Header("Events")]
        public UnityEvent OnTransitionStart = new UnityEvent();
        public UnityEvent OnFadeOutComplete = new UnityEvent();
        public UnityEvent OnSceneLoaded = new UnityEvent();
        public UnityEvent OnFadeInStart = new UnityEvent();
        public UnityEvent OnTransitionFinished = new UnityEvent();

        private Canvas transitionCanvas;
        private Image transitionImageInstance;
        private CanvasGroup canvasGroup;

        public bool IsTransitioning { get; private set; }

        private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCanvas();
                InitializeEvents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeEvents()
        {
            OnTransitionStart ??= new UnityEvent();
            OnFadeOutComplete ??= new UnityEvent();
            OnSceneLoaded ??= new UnityEvent();
            OnFadeInStart ??= new UnityEvent();
            OnTransitionFinished ??= new UnityEvent();
        }

        private void InitializeCanvas()
        {
            GameObject canvasGO = new GameObject("TransitionCanvas");
            canvasGO.transform.SetParent(transform);

            transitionCanvas = canvasGO.AddComponent<Canvas>();
            transitionCanvas.renderMode = canvasRenderMode;
            transitionCanvas.sortingOrder = canvasSortingOrder;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            transitionImageInstance = Instantiate(transitionImagePrefab, canvasGO.transform);

            // --- LA MAGIA PER FREGARE LO SHADER BUGGATO ---
            RectTransform rectT = transitionImageInstance.rectTransform;

            // 1. Ancoriamo il perno al CENTRO ASSOLUTO dello schermo
            rectT.anchorMin = new Vector2(0.5f, 0.5f);
            rectT.anchorMax = new Vector2(0.5f, 0.5f);
            rectT.pivot = new Vector2(0.5f, 0.5f);
            rectT.anchoredPosition = Vector2.zero;

            // 2. MANTENIAMO I 600 PIXEL. Se mettiamo numeri diversi, 
            // lo shader va in panico e disegna nell'angolo.
            rectT.sizeDelta = new Vector2(600f, 600f);

            transitionImageInstance.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            UpdateVRCamera();
        }

        public void LoadScene(string sceneName, TransitionEffect effect = null)
        {
            StartTransitionRoutine(sceneName, -1, null, effect);
        }

        public void LoadScene(int buildIndex, TransitionEffect effect = null)
        {
            StartTransitionRoutine(null, buildIndex, null, effect);
        }

        public void PlayTransition(Action onMidPoint = null, TransitionEffect effect = null)
        {
            StartTransitionRoutine(null, -1, onMidPoint, effect);
        }

        public void PlayTransition()
        {
            StartTransitionRoutine(null, -1, null, defaultTransition);
        }

        private void StartTransitionRoutine(string sceneName, int sceneIndex, Action midPointAction, TransitionEffect effect)
        {
            if (IsTransitioning) return;

            TransitionEffect usedEffect = effect != null ? effect : defaultTransition;
            if (usedEffect == null)
            {
                Debug.LogError("[SceneTransitioner] No transition effect assigned.", this);
                return;
            }

            StartCoroutine(TransitionRoutine(sceneName, sceneIndex, midPointAction, usedEffect));
        }

        private IEnumerator TransitionRoutine(string sceneName, int sceneIndex, Action midPointAction, TransitionEffect effect)
        {
            IsTransitioning = true;
            transitionImageInstance.gameObject.SetActive(true);

            if (canvasGroup != null && blockUIInteraction)
                canvasGroup.blocksRaycasts = true;

            Canvas.ForceUpdateCanvases();

            // --- 3. IL CALCOLO DELLO ZOOM DINAMICO ---
            // Troviamo il lato più lungo dello schermo...
            float maxDim = Mathf.Max(Screen.width, Screen.height);
            if (transitionCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Rect canvasRect = transitionCanvas.GetComponent<RectTransform>().rect;
                maxDim = Mathf.Max(canvasRect.width, canvasRect.height);
            }
            if (maxDim <= 0) maxDim = 2000f; // Paracadute di emergenza

            // ...e ingrandiamo l'immagine di 600px finché non copre tutto in sicurezza (* 2f per gli angoli)
            float requiredScale = (maxDim / 600f) * 2f;
            transitionImageInstance.rectTransform.localScale = new Vector3(requiredScale, requiredScale, 1f);

            OnTransitionStart?.Invoke();

            Material materialInstance = new Material(effect.transitionMaterial);

            // Lo shader riceve 600x600 e funziona alla perfezione.
            Rect rect = transitionImageInstance.rectTransform.rect;
            materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));

            effect.SetEffectProperties(materialInstance);
            transitionImageInstance.material = materialInstance;

            // --- FADE OUT ---
            yield return effect.AnimateOut(transitionImageInstance);
            OnFadeOutComplete?.Invoke();

            // --- SCENE LOAD ---
            if (!string.IsNullOrEmpty(sceneName) || sceneIndex >= 0)
            {
                AsyncOperation op = !string.IsNullOrEmpty(sceneName)
                    ? SceneManager.LoadSceneAsync(sceneName)
                    : SceneManager.LoadSceneAsync(sceneIndex);

                yield return op;
                OnSceneLoaded?.Invoke();

                UpdateVRCamera();
            }

            // --- MIDPOINT ---
            midPointAction?.Invoke();

            if (effect.MiddleDelay > 0)
                yield return new WaitForSecondsRealtime(effect.MiddleDelay);
            else
                yield return null;

            OnFadeInStart?.Invoke();

            // --- FADE IN ---
            yield return effect.AnimateIn(transitionImageInstance);

            // --- CLEANUP ---
            transitionImageInstance.gameObject.SetActive(false);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            IsTransitioning = false;

            Destroy(materialInstance);
            OnTransitionFinished?.Invoke();
        }

        private void UpdateVRCamera()
        {
            if (transitionCanvas != null && (canvasRenderMode == RenderMode.ScreenSpaceCamera || canvasRenderMode == RenderMode.WorldSpace))
            {
                Camera cam = targetCamera != null ? targetCamera : Camera.main;
                if (cam != null && transitionCanvas.worldCamera != cam)
                {
                    transitionCanvas.worldCamera = cam;
                    transitionCanvas.planeDistance = cam.nearClipPlane + 0.01f;
                    Canvas.ForceUpdateCanvases();
                }
            }
        }

        public void PlayGlobalSound(AudioClip clip)
        {
            if (clip != null && TryGetComponent(out AudioSource source)) source.PlayOneShot(clip);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
    }
}