using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 범용 씬 전환 UI 매니저
/// StartScene, ResultScene 등 다양한 씬에서 사용 가능
/// </summary>
public class StartSceneUI : MonoBehaviour
{
    private const string SelectedThemeKey = "SelectedTheme";
    private const string ThemeAwakeningAI = "AwakeningAI";
    private const string ThemeConcreteUtopia = "ConcreteUtopia";
    private const string ThemeDevilsAdvocate = "DevilsAdvocate";

    [Header("버튼 설정")]
    [Tooltip("게임 시작/재시작 버튼")]
    [SerializeField] private Button _startButton;

    [Tooltip("메인 메뉴로 돌아가기 버튼")]
    [SerializeField] private Button _mainMenuButton;

    [Tooltip("게임 종료 버튼 (선택사항)")]
    [SerializeField] private Button _quitButton;

    [Header("임무 선택 패널")]
    [Tooltip("임무 선택 팝업 패널")]
    [SerializeField] private GameObject _missionPanel;

    [Tooltip("임무 선택 패널 닫기 버튼")]
    [SerializeField] private Button _closeMissionPanelButton;

    [Tooltip("The Awakening AI 시나리오 버튼")]
    [SerializeField] private Button _awakeningAIMissionButton;

    [Tooltip("Concrete Utopia 시나리오 버튼")]
    [SerializeField] private Button _concreteUtopiaMissionButton;

    [Tooltip("The Devil's Advocate 시나리오 버튼")]
    [SerializeField] private Button _devilsAdvocateMissionButton;

    [Tooltip("랜덤 시나리오 버튼")]
    [SerializeField] private Button _randomMissionButton;

    [Header("씬 설정")]
    [Tooltip("시작 버튼 클릭 시 로드할 씬 이름")]
    [SerializeField] private string _targetSceneName = "GameScene";

    [Tooltip("메인 메뉴 버튼 클릭 시 로드할 씬 이름")]
    [SerializeField] private string _mainMenuSceneName = "StartScene";

    private void Start()
    {
        // 버튼 이벤트 연결
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        if (_missionPanel != null)
        {
            _missionPanel.SetActive(false);
        }

        if (_closeMissionPanelButton != null)
        {
            _closeMissionPanelButton.onClick.AddListener(OnCloseMissionPanelClicked);
        }

        if (_awakeningAIMissionButton != null)
        {
            _awakeningAIMissionButton.onClick.AddListener(() => OnMissionSelected(ThemeAwakeningAI));
        }

        if (_concreteUtopiaMissionButton != null)
        {
            _concreteUtopiaMissionButton.onClick.AddListener(() => OnMissionSelected(ThemeConcreteUtopia));
        }

        if (_devilsAdvocateMissionButton != null)
        {
            _devilsAdvocateMissionButton.onClick.AddListener(() => OnMissionSelected(ThemeDevilsAdvocate));
        }

        if (_randomMissionButton != null)
        {
            _randomMissionButton.onClick.AddListener(() => OnMissionSelected("Random"));
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }

        if (_closeMissionPanelButton != null)
        {
            _closeMissionPanelButton.onClick.RemoveListener(OnCloseMissionPanelClicked);
        }

        if (_awakeningAIMissionButton != null)
        {
            _awakeningAIMissionButton.onClick.RemoveAllListeners();
        }

        if (_concreteUtopiaMissionButton != null)
        {
            _concreteUtopiaMissionButton.onClick.RemoveAllListeners();
        }

        if (_devilsAdvocateMissionButton != null)
        {
            _devilsAdvocateMissionButton.onClick.RemoveAllListeners();
        }

        if (_randomMissionButton != null)
        {
            _randomMissionButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 시작/재시작 버튼 클릭 시 호출
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (_missionPanel != null)
        {
            _missionPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("임무 선택 패널이 설정되지 않아 바로 게임을 시작합니다.");
            LoadScene(_targetSceneName);
        }
    }

    /// <summary>
    /// 임무 선택 패널 닫기 버튼 클릭 시 호출
    /// </summary>
    private void OnCloseMissionPanelClicked()
    {
        if (_missionPanel != null)
        {
            _missionPanel.SetActive(false);
        }
    }

    private void OnMissionSelected(string themeKey)
    {
        string resolvedTheme = string.IsNullOrEmpty(themeKey) ? "Random" : themeKey;
        
        PlayerPrefs.SetString(SelectedThemeKey, resolvedTheme);
        PlayerPrefs.Save();

        Debug.Log($"임무 선택: {resolvedTheme}");

        if (_missionPanel != null)
        {
            _missionPanel.SetActive(false);
        }

        LoadScene(_targetSceneName);
    }

    /// <summary>
    /// 메인 메뉴 버튼 클릭 시 호출
    /// </summary>
    private void OnMainMenuButtonClicked()
    {
        Debug.Log($"메인 메뉴로 이동: {_mainMenuSceneName}");
        
        // 저장된 게임 상태 초기화 (결산 씬에서 돌아갈 때)
        PlayerPrefs.DeleteKey("FinalGameState");
        
        LoadScene(_mainMenuSceneName);
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
    /// 씬 로드 (페이드 효과 포함)
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            // 페이드 효과와 함께 씬 전환
            if (SceneFadeManager.Instance != null)
            {
                SceneFadeManager.Instance.LoadSceneWithFade(sceneName);
            }
            else
            {
                // 페이드 매니저가 없으면 일반 로드
                Debug.LogWarning("⚠️ SceneFadeManager가 없습니다. 일반 씬 전환을 수행합니다.");
                SceneManager.LoadScene(sceneName);
            }
        }
        else
        {
            Debug.LogError("❌ 씬 이름이 설정되지 않았습니다!");
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
    /// 지정된 타겟 씬으로 이동
    /// </summary>
    public void StartGame()
    {
        LoadScene(_targetSceneName);
    }

    /// <summary>
    /// 외부에서 호출 가능한 public 메서드 (Unity Event용)
    /// 메인 메뉴로 이동
    /// </summary>
    public void GoToMainMenu()
    {
        PlayerPrefs.DeleteKey("FinalGameState");
        LoadScene(_mainMenuSceneName);
    }

    /// <summary>
    /// 외부에서 호출 가능한 public 메서드 (Unity Event용)
    /// 특정 씬으로 직접 이동
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        LoadScene(sceneName);
    }

    /// <summary>
    /// 외부에서 호출 가능한 public 메서드 (Unity Event용)
    /// 게임 재시작 (게임 상태 초기화 포함)
    /// </summary>
    public void RestartGame()
    {
        PlayerPrefs.DeleteKey("FinalGameState");
        LoadScene("GameScene");
    }

    /// <summary>
    /// 외부에서 호출 가능한 public 메서드 (Unity Event용)
    /// 게임 종료
    /// </summary>
    public void ExitGame()
    {
        QuitGame();
    }
}
