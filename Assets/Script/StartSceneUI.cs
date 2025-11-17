using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneUI : MonoBehaviour
{
    [Header("버튼 설정")]
    [Tooltip("게임 시작 버튼")]
    [SerializeField] private Button _startButton;

    [Tooltip("게임 종료 버튼 (선택사항)")]
    [SerializeField] private Button _quitButton;

    [Header("씬 설정")]
    [Tooltip("로드할 게임 씬 이름")]
    [SerializeField] private string _gameSceneName = "GameScene";

    private void Start()
    {
        // 버튼 이벤트 연결
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            Debug.LogWarning("⚠️ Start Button이 연결되지 않았습니다!");
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }
    }

    /// <summary>
    /// 게임 시작 버튼 클릭 시 호출
    /// </summary>
    private void OnStartButtonClicked()
    {
        Debug.Log($"게임 시작: {_gameSceneName} 씬으로 이동");
        LoadGameScene();
    }

    /// <summary>
    /// 게임 종료 버튼 클릭 시 호출
    /// </summary>
    private void OnQuitButtonClicked()
    {
        Debug.Log("게임 종료");
        QuitGame();
    }

    /// <summary>
    /// 게임 씬 로드
    /// </summary>
    private void LoadGameScene()
    {
        if (!string.IsNullOrEmpty(_gameSceneName))
        {
            // 페이드 효과와 함께 씬 전환
            if (SceneFadeManager.Instance != null)
            {
                SceneFadeManager.Instance.LoadSceneWithFade(_gameSceneName);
            }
            else
            {
                // 페이드 매니저가 없으면 일반 로드
                Debug.LogWarning("⚠️ SceneFadeManager가 없습니다. 일반 씬 전환을 수행합니다.");
                SceneManager.LoadScene(_gameSceneName);
            }
        }
        else
        {
            Debug.LogError("❌ 게임 씬 이름이 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 외부에서 호출 가능한 public 메서드 (Unity Event용)
    /// </summary>
    public void StartGame()
    {
        LoadGameScene();
    }

    /// <summary>
    /// 외부에서 호출 가능한 public 메서드 (Unity Event용)
    /// </summary>
    public void ExitGame()
    {
        QuitGame();
    }
}
