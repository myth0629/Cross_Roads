using UnityEngine;
using GenerativeAI;
using System.Threading.Tasks;
using TMPro;

public class GeminiTest : MonoBehaviour
{
    [Header("Gemini 설정")]
    [SerializeField] private string apiKey; // 일단 직렬화 필드로 입력, 나중에 SO로 교체 가능

    [Header("게임 진행 설정")]
    [Tooltip("게임 시작 시마다 완전히 새로운 초기 상황을 만들지 여부")]
    [SerializeField] private bool randomizeStartScenario = true;

    [Header("UI")]
    [Tooltip("현재 상황(내레이션) 표시용 텍스트")]
    public TextMeshProUGUI situationText;

    [Tooltip("선택지 4개를 표시할 텍스트들 (버튼 텍스트 등)")]
    public TextMeshProUGUI[] choiceTexts; // 0~3 인덱스 사용

    // 간단한 게임 상태 구조체
    private GameState gameState;
    private GenerativeModel flashModel;

    private async void Start()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("GeminiTest: API 키가 비어 있습니다.");
            if (situationText != null)
            {
                situationText.text = "오류: API 키가 설정되지 않았습니다.";
            }
            return;
        }

        // 모델은 flash 하나만 사용
        flashModel = new GenerativeModel(apiKey, "gemini-1.5-flash");

        // 게임 시작 시 GameState 초기화
        InitGameState();

        // 첫 턴 실행
        await RunTurnAsync();
    }

    private void InitGameState()
    {
        if (randomizeStartScenario)
        {
            // 간단한 랜덤 프리셋 예시
            int rand = Random.Range(0, 3);
            switch (rand)
            {
                case 0:
                    gameState = new GameState
                    {
                        scene = "병원 로비",
                        objective = "병원의 안정성을 유지하고 최대한 많은 생존자를 확보한다.",
                        resources = new ResourcesState { medicine = 10, food = 20 },
                        survivorGroups = new SurvivorGroupsState { doctors = 3, patients = 10, guards = 5 },
                        plotSummary = "방금 전염병에 대한 '실험적인 치료제' 1인분이 도착했다.",
                        lastPlayerAction = null
                    };
                    break;
                case 1:
                    gameState = new GameState
                    {
                        scene = "응급실 복도",
                        objective = "폭증하는 환자들을 통제하고 의료진을 보호한다.",
                        resources = new ResourcesState { medicine = 5, food = 15 },
                        survivorGroups = new SurvivorGroupsState { doctors = 2, patients = 20, guards = 3 },
                        plotSummary = "격리 구역의 문이 망가져 감염자들이 밀려들 수 있는 상황이다.",
                        lastPlayerAction = null
                    };
                    break;
                default:
                    gameState = new GameState
                    {
                        scene = "옥상 헬리패드",
                        objective = "구조 헬기를 기다리며 병원 내 질서를 유지한다.",
                        resources = new ResourcesState { medicine = 3, food = 25 },
                        survivorGroups = new SurvivorGroupsState { doctors = 4, patients = 8, guards = 4 },
                        plotSummary = "마지막 구조 헬기가 오고 있지만, 탑승 가능한 인원은 제한적이다.",
                        lastPlayerAction = null
                    };
                    break;
            }
        }
        else
        {
            // 고정된 시작 상황
            gameState = new GameState
            {
                scene = "병원 로비",
                objective = "병원의 안정성을 유지하고 최대한 많은 생존자를 확보한다.",
                resources = new ResourcesState { medicine = 10, food = 20 },
                survivorGroups = new SurvivorGroupsState { doctors = 3, patients = 10, guards = 5 },
                plotSummary = "방금 전염병에 대한 '실험적인 치료제' 1인분이 도착했다.",
                lastPlayerAction = null
            };
        }
    }

    /// <summary>
    /// 한 턴: flash 하나만 사용해서 상황 + 선택지까지 한 번에 생성
    /// </summary>
    public async Task RunTurnAsync()
    {
        try
        {
            string prompt = BuildTurnPrompt(gameState);
            var response = await flashModel.GenerateContentAsync(prompt);
            string text = response.Text;

            // 응답을 "상황"과 "선택지"로 나누기 위한 아주 단순한 프로토콜 예시
            // 프롬프트에서 "### 선택지" / "### 선택지" 형식을 강제할 것임
            string situationPart = text;
            string[] choicesPart = new string[0];

            var marker = "### 선택지";
            int idx = text.IndexOf(marker);
            if (idx >= 0)
            {
                situationPart = text.Substring(0, idx).Trim();
                string choicesText = text.Substring(idx + marker.Length).Trim();
                choicesPart = ParseChoices(choicesText);
            }

            if (situationText != null)
            {
                situationText.text = situationPart;
            }

            for (int i = 0; i < choiceTexts.Length; i++)
            {
                if (i < choicesPart.Length && choiceTexts[i] != null)
                {
                    choiceTexts[i].text = choicesPart[i];
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"턴 진행 중 에러 발생: {e.Message}");
            if (situationText != null)
            {
                situationText.text = "오류: 상황을 생성하지 못했습니다.";
            }
        }
    }

    // 버튼 클릭 시 호출할 메서드 (유니티 이벤트에서 index 전달)
    public async void OnChoiceSelected(int index)
    {
        if (choiceTexts == null || index < 0 || index >= choiceTexts.Length || choiceTexts[index] == null)
            return;

        // 마지막 플레이어 행동 갱신
        gameState.lastPlayerAction = choiceTexts[index].text;
        // 간단하게 요약에 누적
        gameState.plotSummary = $"플레이어의 최근 선택: {gameState.lastPlayerAction}";

        await RunTurnAsync();
    }

    private string BuildTurnPrompt(GameState state)
    {
        // flash 한 번 호출로 상황 + 선택지까지 한 번에 생성하도록 지시
        return
            "너는 전염병 이후 붕괴된 도시의 마지막 병원에서 벌어지는 딜레마 게임의 내러티브 AI야.\n" +
            "아래 GameState를 바탕으로, 먼저 플레이어에게 보여줄 '현재 상황'을 2~4문장 정도 한국어로 묘사하고,\n" +
            "그 다음 줄에 '### 선택지' 라는 구분자를 적은 뒤, 플레이어가 선택할 수 있는 4개의 선택지를 한 줄에 하나씩 만들어라.\n" +
            "각 선택지는 번호(1., 2.) 없이 자연스러운 문장 형태여야 한다.\n" +
            "반드시 다음 형식을 지켜라:\n" +
            "[상황 설명 여러 문장]\n" +
            "### 선택지\n" +
            "[선택지1]\n" +
            "[선택지2]\n" +
            "[선택지3]\n" +
            "[선택지4]\n" +
            "추가 설명은 쓰지 마라.\n\n" +
            $"Scene: {state.scene}\n" +
            $"Objective: {state.objective}\n" +
            $"Resources: medicine={state.resources.medicine}, food={state.resources.food}\n" +
            $"Survivors: doctors={state.survivorGroups.doctors}, patients={state.survivorGroups.patients}, guards={state.survivorGroups.guards}\n" +
            $"Plot Summary: {state.plotSummary}\n" +
            $"Last Player Action: {state.lastPlayerAction}\n";
    }

    private string[] ParseChoices(string choicesText)
    {
        var lines = choicesText.Split('\n');
        var list = new System.Collections.Generic.List<string>();
        foreach (var l in lines)
        {
            var t = l.Trim();
            if (!string.IsNullOrEmpty(t))
                list.Add(t);
            if (list.Count >= 4) break;
        }
        return list.ToArray();
    }
}

// --- 간단한 GameState 정의 ---

[System.Serializable]
public class GameState
{
    public string scene;
    public string objective;
    public ResourcesState resources;
    public SurvivorGroupsState survivorGroups;
    public string plotSummary;
    public string lastPlayerAction;
}

[System.Serializable]
public class ResourcesState
{
    public int medicine;
    public int food;
}

[System.Serializable]
public class SurvivorGroupsState
{
    public int doctors;
    public int patients;
    public int guards;
}

