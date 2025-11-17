using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    [Header("페이드 설정")]
    [Tooltip("페이드 효과에 사용할 Image (검은색 전체 화면)")]
    [SerializeField] private Image _fadeImage;

    [Tooltip("페이드 인 시간 (초)")]
    [SerializeField] private float _fadeInDuration = 1f;

    [Tooltip("페이드 아웃 시간 (초)")]
    [SerializeField] private float _fadeOutDuration = 1f;

    [Tooltip("페이드 색상")]
    [SerializeField] private Color _fadeColor = Color.black;

    [Header("씬별 설정")]
    [Tooltip("씬 시작 시 자동으로 페이드 인할지 여부")]
    [SerializeField] private bool _fadeInOnStart = true;

    private static SceneFadeManager _instance;
    public static SceneFadeManager Instance => _instance;

    private void Awake()
    {
        // 싱글톤 패턴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 페이드 이미지 초기화
        if (_fadeImage != null)
        {
            // 런타임에만 이미지 활성화 (에디터에서는 비활성화 상태로 두기)
            _fadeImage.enabled = true;

            // FadeImage의 부모 Canvas도 DontDestroyOnLoad 처리
            Canvas fadeCanvas = _fadeImage.GetComponentInParent<Canvas>();
            if (fadeCanvas != null && fadeCanvas.transform.parent == null)
            {
                DontDestroyOnLoad(fadeCanvas.gameObject);
            }

            // 에디터에서도 보이지 않도록 초기에는 완전히 투명하게 설정
            Color color = _fadeColor;
            color.a = 0f;
            _fadeImage.color = color;
            _fadeImage.raycastTarget = false; // 클릭 방지는 페이드 중에만
        }
    }

    private void Start()
    {
        // 씬 시작 시 페이드 인 (설정에 따라)
        if (_fadeInOnStart)
        {
            // 페이드 인이 필요한 경우, 먼저 불투명하게 만들고 시작
            if (_fadeImage != null)
            {
                Color color = _fadeColor;
                color.a = 1f;
                _fadeImage.color = color;
            }
            StartCoroutine(FadeIn());
        }
        else
        {
            // 페이드 인하지 않는 경우 이미지를 완전히 투명하게 유지 (Awake에서 이미 설정됨)
            if (_fadeImage != null)
            {
                _fadeImage.raycastTarget = false;
            }
        }
    }

    /// <summary>
    /// 씬을 페이드 아웃 후 로드
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    /// <summary>
    /// 씬을 페이드 아웃 후 로드 (빌드 인덱스)
    /// </summary>
    public void LoadSceneWithFade(int sceneIndex)
    {
        StartCoroutine(FadeOutAndLoadScene(sceneIndex));
    }

    /// <summary>
    /// 페이드 인 (화면이 밝아짐)
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (_fadeImage == null)
        {
            Debug.LogWarning("⚠️ Fade Image가 설정되지 않았습니다!");
            yield break;
        }

        _fadeImage.raycastTarget = true; // 페이드 중 클릭 방지
        float elapsed = 0f;
        Color color = _fadeColor;

        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / _fadeInDuration);
            color.a = alpha;
            _fadeImage.color = color;
            yield return null;
        }

        // 완전히 투명하게
        color.a = 0f;
        _fadeImage.color = color;
        _fadeImage.raycastTarget = false; // 클릭 방지 해제
    }

    /// <summary>
    /// 페이드 아웃 (화면이 어두워짐)
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (_fadeImage == null)
        {
            Debug.LogWarning("⚠️ Fade Image가 설정되지 않았습니다!");
            yield break;
        }

        _fadeImage.raycastTarget = true; // 페이드 중 클릭 방지
        float elapsed = 0f;
        Color color = _fadeColor;

        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / _fadeOutDuration;
            color.a = alpha;
            _fadeImage.color = color;
            yield return null;
        }

        // 완전히 불투명하게
        color.a = 1f;
        _fadeImage.color = color;
    }

    /// <summary>
    /// 페이드 아웃 후 씬 로드 (씬 이름)
    /// </summary>
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
        // 씬 로드 후 페이드 인
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(FadeIn());
    }

    /// <summary>
    /// 페이드 아웃 후 씬 로드 (빌드 인덱스)
    /// </summary>
    private IEnumerator FadeOutAndLoadScene(int sceneIndex)
    {
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneIndex);
        // 씬 로드 후 페이드 인
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(FadeIn());
    }
}
