using UnityEngine;
using GenerativeAI;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;

public class GeminiManager : MonoBehaviour
{
    [Header("Gemini 설정")]
    [SerializeField] private GeminiApiKeySO apiKey;

    [Header("UI")]
    [Tooltip("현재 상황(내레이션) 표시용 텍스트")]
    public TextMeshProUGUI situationText;

    [Tooltip("선택지 4개를 표시할 텍스트들 (버튼 텍스트 등)")]
    public TextMeshProUGUI[] choiceTexts; // 0~3 인덱스 사용

    [Tooltip("남은 턴 수 표시용 텍스트")]
    public TextMeshProUGUI turnsRemainingText;

    [Header("안정성 UI")]
    [Tooltip("안정도 게이지 (0~100)")]
    public Slider StabilitySlider;

    [Tooltip("안정도 값 텍스트 (예: 70/100)")]
    public TextMeshProUGUI stabilityValueText;

    [Header("자원 카운터 UI")]
    [Tooltip("식량 비축량 텍스트")]
    public TextMeshProUGUI foodCountText;

    [Header("로딩 UI")]
    [Tooltip("로딩 패널 (API 호출 중 표시)")]
    public GameObject loadingPanel;

    [Tooltip("선택지 버튼들 (로딩 중 비활성화)")]
    public Button[] choiceButtons; // 0~3 인덱스 사용

    // 간단한 게임 상태 구조체
    private GameState gameState;
    private GenerativeModel flashModel;
    private bool _isProcessing = false; // API 처리 중 플래그
    private bool _isFirstTurn = true; // 첫 번째 턴 여부 (첫 턴은 상태 변화 없음)

    private const string SelectedThemeKey = "SelectedTheme";

    private async void Start()
    {
        if (string.IsNullOrEmpty(apiKey.apiKey))
        {
            Debug.LogError("GeminiTest: API 키가 비어 있습니다.");
            if (situationText != null)
            {
                situationText.text = "오류: API 키가 설정되지 않았습니다.";
            }
            return;
        }

        // 로딩 패널 초기 상태 설정
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // 슬라이더 범위 초기화 (0~100)
        InitializeSliders();

        // llm 모델 지정
        flashModel = new GenerativeModel(apiKey.apiKey, "gemini-2.5-flash-lite");

        // 게임 시작 시 GameState 초기화
        InitGameState();

        // 초기 UI 업데이트
        UpdateStatsUI();

        // 첫 턴 실행
        await RunTurnAsync();
    }

    private void InitGameState()
    {
        string selectedTheme = PlayerPrefs.GetString(SelectedThemeKey, "Random");

        // Random이 선택된 경우 실제 테마 중 하나를 무작위로 선택
        if (selectedTheme == "Random")
        {
            string[] availableThemes = { "AwakeningAI", "ConcreteUtopia", "DevilsAdvocate" };
            selectedTheme = availableThemes[UnityEngine.Random.Range(0, availableThemes.Length)];
            Debug.Log($"🎲 무작위 테마 선택됨: {selectedTheme}");
        }

        // AI가 완전히 자유롭게 시작 상황을 만들도록 최소한의 초기 상태만 제공
        gameState = new GameState
        {
            scene = "미정",
            objective = "목표를 달성하라",
            resources = new ResourcesState { food = 20 },
            survivorGroups = new SurvivorGroupsState { doctors = 0, patients = 0, guards = 0 },
            plotSummary = "새로운 딜레마 상황이 시작되었다. 당신은 중요한 결정을 내려야 한다.",
            lastPlayerAction = "GameStart",
            turnsRemaining = 14, // 7일 = 14턴
            stability = new StabilityState { stability = 100 },
            selectedTheme = selectedTheme
        };

        Debug.Log($"🎮 게임 시작 - 선택된 테마: {selectedTheme}");
    }

    /// <summary>
    /// 슬라이더 범위 초기화 (0~100)
    /// </summary>
    private void InitializeSliders()
    {
        // 안정성 슬라이더
        if (StabilitySlider != null)
        {
            StabilitySlider.minValue = 0;
            StabilitySlider.maxValue = 100;
        }

        Debug.Log("✅ 슬라이더 범위 초기화 완료 (0~100)");
    }

    /// <summary>
    /// 한 턴: Flash 한 번 호출로 상황 + 선택지를 JSON으로 받아 파싱
    /// </summary>
    public async Task RunTurnAsync()
    {
        // 로딩 시작
        SetLoadingState(true);

        try
        {
            // GeminiPromptBuilder로 통합 프롬프트 생성 (JSON 응답 기대)
            string prompt = GeminiPromptBuilder.BuildUnifiedPrompt(gameState);
            var response = await flashModel.GenerateContentAsync(prompt);
            string rawText = response.Text;

            Debug.Log($"[Gemini Raw Response]\n{rawText}");

            // JSON 파싱
            GeminiResponse geminiResponse = ParseGeminiResponse(rawText);

            if (geminiResponse == null)
            {
                throw new System.Exception("JSON 파싱 실패: 응답 형식이 올바르지 않습니다.");
            }

            // UI 업데이트를 메인 스레드에서 확실히 실행
            UnityEngine.Debug.Log("[파싱 성공] UI 업데이트 시작...");
            UpdateUI(geminiResponse);
            UpdateTurnsUI(); // 남은 턴 UI 업데이트
            
            // 첫 번째 턴이 아닐 때만 상태 업데이트 적용
            if (!_isFirstTurn)
            {
                ApplyStateUpdate(geminiResponse); // 상태 업데이트 적용
            }
            else
            {
                Debug.Log("🎮 첫 번째 턴: 상태 변화 없음 (초기 상황만 표시)");
            }
            
            UpdateStatsUI(); // 안정성, 신뢰도, 자원 UI 업데이트

            // 안정성 체크 (게임오버 조건)
            if (gameState.stability.stability <= 0)
            {
                Debug.Log("안정성이 0이 되었습니다! 게임오버!");
                
                // 로딩 종료
                SetLoadingState(false);
                
                // 게임오버 메시지 표시
                if (situationText != null)
                {
                    situationText.text = "안정성이 바닥났습니다. 모든 것이 무너졌습니다...";
                }
                
                // 잠시 대기 후 결산 씬으로
                await System.Threading.Tasks.Task.Delay(2000);
                GoToResultScene();
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"턴 진행 중 에러 발생: {e.Message}");
            if (situationText != null)
            {
                string userMessage = "오류가 발생했습니다.";
                if (e.Message != null && e.Message.Contains("503"))
                {
                    userMessage = "서버가 잠시 과부하 상태입니다.\n잠시 후 다시 시도해 주세요.";
                }
                situationText.text = userMessage;
            }
        }
        finally
        {
            // 로딩 종료
            SetLoadingState(false);
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출 (유니티 이벤트에서 index 0~3 전달)
    /// </summary>
    public async void OnChoiceSelected(int index)
    {
        // 이미 처리 중이면 무시
        if (_isProcessing)
        {
            Debug.LogWarning("이미 선택 처리 중입니다.");
            return;
        }

        if (choiceTexts == null || index < 0 || index >= choiceTexts.Length || choiceTexts[index] == null)
            return;

        // 마지막 플레이어 행동 갱신
        gameState.lastPlayerAction = choiceTexts[index].text;

        // 첫 번째 턴이었다면 이제 두 번째 턴으로 전환
        if (_isFirstTurn)
        {
            _isFirstTurn = false;
            Debug.Log("✅ 첫 번째 선택 완료! 다음 턴부터 상태 변화가 적용됩니다.");
        }

        // 턴 감소
        gameState.turnsRemaining--;

        // 간단한 규칙 예시: 선택에 따라 리소스 변경 (필요 시 확장)
        gameState.resources.food = Mathf.Max(0, gameState.resources.food - 1);

        // plotSummary 갱신
        gameState.plotSummary = $"플레이어의 최근 선택: {gameState.lastPlayerAction}";

        // 안정성 0 확인 (즉시 게임오버)
        if (gameState.stability.stability <= 0)
        {
            Debug.Log("안정성 0! 게임오버!");
            GoToResultScene();
            return;
        }

        // 14턴(Day 7 오후) 종료 확인
        if (gameState.turnsRemaining <= 0)
        {
            // 게임 종료 - 결산 씬으로 이동
            GoToResultScene();
            return;
        }

        // 다음 턴 진행
        await RunTurnAsync();
    }

    /// <summary>
    /// 결산 씬으로 이동
    /// </summary>
    private void GoToResultScene()
    {
        // GameState를 PlayerPrefs에 저장하여 결산 씬에서 사용
        string gameStateJson = JsonUtility.ToJson(gameState);
        PlayerPrefs.SetString("FinalGameState", gameStateJson);
        PlayerPrefs.Save();

        Debug.Log("게임 종료! 결산 씬으로 이동합니다.");

        // SceneFadeManager를 통해 결산 씬으로 전환
        if (SceneFadeManager.Instance != null)
        {
            SceneFadeManager.Instance.LoadSceneWithFade("ResultScene");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("ResultScene");
        }
    }

    /// <summary>
    /// 로딩 상태 설정 (로딩 패널 표시/숨김, 버튼 활성화/비활성화)
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        _isProcessing = isLoading;

        // 로딩 패널 표시/숨김
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(isLoading);
        }

        // 선택지 버튼 활성화/비활성화
        if (choiceButtons != null)
        {
            foreach (var button in choiceButtons)
            {
                if (button != null)
                {
                    button.interactable = !isLoading;
                }
            }
        }

        Debug.Log($"로딩 상태: {(isLoading ? "로딩 중..." : "로딩 완료")}");
    }

    /// <summary>
    /// UI 업데이트 (메인 스레드에서 확실히 실행)
    /// </summary>
    private void UpdateUI(GeminiResponse response)
    {
        if (response == null)
        {
            Debug.LogError("UpdateUI: response가 null입니다!");
            return;
        }

        // 상황 텍스트 업데이트
        if (situationText != null)
        {
            situationText.text = response.situation_text;
            Debug.Log($"✅ [UI 업데이트 완료] 상황 텍스트: {response.situation_text.Substring(0, Mathf.Min(50, response.situation_text.Length))}...");
        }
        else
        {
            Debug.LogWarning("⚠️ situationText가 null입니다. 인스펙터에서 TMP 텍스트를 연결하세요!");
        }

        // 선택지 텍스트 업데이트
        if (choiceTexts == null || choiceTexts.Length == 0)
        {
            Debug.LogWarning("⚠️ choiceTexts 배열이 비어있습니다. 인스펙터에서 Size=4로 설정하고 버튼 텍스트를 드래그하세요!");
            return;
        }

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            if (i < response.choices.Length && choiceTexts[i] != null)
            {
                choiceTexts[i].text = response.choices[i];
                Debug.Log($"✅ [UI 업데이트 완료] 선택지 {i}: {response.choices[i].Substring(0, Mathf.Min(40, response.choices[i].Length))}...");
            }
            else if (choiceTexts[i] != null)
            {
                choiceTexts[i].text = "";
            }
            else if (i < response.choices.Length)
            {
                Debug.LogWarning($"⚠️ choiceTexts[{i}]가 null입니다. 인스펙터에서 연결하세요!");
            }
        }
    }

    private GeminiResponse ParseGeminiResponse(string rawText)
    {
        try
        {
            // 혹시 모델이 ```json ... ``` 형태로 감싸서 반환하는 경우 제거
            string cleaned = rawText.Trim();
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            cleaned = cleaned.Trim();

            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(cleaned);
            return response;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 파싱 오류: {e.Message}\nRaw Text:\n{rawText}");
            return null;
        }
    }

    /// <summary>
    /// 남은 턴 수 UI 업데이트
    /// </summary>
    private void UpdateTurnsUI()
    {
        if (turnsRemainingText != null)
        {
            int daysRemaining = Mathf.CeilToInt(gameState.turnsRemaining / 2f);
            string timeOfDay = (gameState.turnsRemaining % 2 == 0) ? "오전" : "오후";
            turnsRemainingText.text = $"Day {8 - daysRemaining} {timeOfDay}";
        }
    }

    /// <summary>
    /// Gemini 응답에서 상태 업데이트 적용
    /// </summary>
    private void ApplyStateUpdate(GeminiResponse response)
    {
        if (response.state_update == null)
            return;

        // 자원 업데이트
        if (response.state_update.resources != null)
        {
            int oldFood = gameState.resources.food;
            gameState.resources.food = Mathf.Clamp(response.state_update.resources.food, 0, 99);
            Debug.Log($"[자원 업데이트] 식량: {oldFood} → {gameState.resources.food}");
        }

        // 안정성 업데이트
        if (response.state_update.stability != null)
        {
            int oldStability = gameState.stability.stability;
            gameState.stability.stability = Mathf.Clamp(response.state_update.stability.stability, 0, 100);
            Debug.Log($"[안정성 업데이트] {oldStability} → {gameState.stability.stability}");
        }
    }

    /// <summary>
    /// 안정성, 자원 UI 업데이트
    /// </summary>
    private void UpdateStatsUI()
    {
        // 안정성 게이지 업데이트
        if (StabilitySlider != null)
        {
            StabilitySlider.value = gameState.stability.stability;
            Debug.Log($"[UI 슬라이더] 안정성 슬라이더 = {gameState.stability.stability} (minValue={StabilitySlider.minValue}, maxValue={StabilitySlider.maxValue})");
        }
        else
        {
            Debug.LogWarning("⚠️ StabilitySlider가 null입니다!");
        }

        // 안정성 값 텍스트 업데이트
        if (stabilityValueText != null)
        {
            stabilityValueText.text = $"{gameState.stability.stability}/100";
        }

        // 자원 카운터 텍스트 업데이트
        if (foodCountText != null)
        {
            float daysOfFood = gameState.resources.food / 3f; // 하루 3개 소비 가정
            foodCountText.text = $"자원: {gameState.resources.food}개";
        }
    }
}

// --- GameState 및 응답 DTO 정의 ---

[System.Serializable]
public class GameState
{
    public string scene;
    public string objective;
    public ResourcesState resources;
    public SurvivorGroupsState survivorGroups;
    public string plotSummary;
    public string lastPlayerAction;
    public int turnsRemaining; // 남은 턴 수
    public StabilityState stability; // 안정성 지표
    public string selectedTheme;
}

[System.Serializable]
public class ResourcesState
{
    public int food;
}

[System.Serializable]
public class SurvivorGroupsState
{
    public int doctors;
    public int patients;
    public int guards;
}

[System.Serializable]
public class StabilityState
{
    public int stability; // 안정도 (0~100)
}

[System.Serializable]
public class GeminiResponse
{
    public string situation_text;
    public string[] choices;
    public GameStateUpdate state_update; // 상태 업데이트 정보
}

[System.Serializable]
public class GameStateUpdate
{
    public ResourcesUpdate resources;
    public StabilityUpdate stability;
}

[System.Serializable]
public class ResourcesUpdate
{
    public int food;
}

[System.Serializable]
public class StabilityUpdate
{
    public int stability;
}
