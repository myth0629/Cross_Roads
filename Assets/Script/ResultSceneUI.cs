using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GenerativeAI;
using System.Threading.Tasks;

/// <summary>
/// 게임 결산 씬 UI 관리
/// </summary>
public class ResultSceneUI : MonoBehaviour
{
    [Header("Gemini API")]
    [SerializeField] private GeminiApiKeySO apiKey;

    [Header("결과 텍스트")]
    [Tooltip("게임 결과 제목 (예: '생존 성공!' 또는 '게임 오버')")]
    public TextMeshProUGUI resultTitleText;

    [Tooltip("최종 상태 요약 텍스트")]
    public TextMeshProUGUI summaryText;

    [Header("최종 스탯")]
    [Tooltip("안정도 슬라이더")]
    public Slider finalStabilitySlider;

    [Tooltip("안정도 값 텍스트")]
    public TextMeshProUGUI stabilityValueText;

    [Header("자원")]
    [Tooltip("최종 식량")]
    public TextMeshProUGUI finalFoodText;

    [Header("버튼")]
    [Tooltip("메인 메뉴로 버튼 (StartSceneUI 컴포넌트 사용)")]
    public StartSceneUI sceneNavigator;

    [Header("로딩 UI")]
    [Tooltip("로딩 패널 (API 호출 중 표시)")]
    public GameObject loadingPanel;

    private GameState _finalGameState;
    private GenerativeModel _flashModel;

    private async void Start()
    {
        // 로딩 패널 표시
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // Gemini API 초기화
        if (string.IsNullOrEmpty(apiKey.apiKey))
        {
            Debug.LogError("ResultSceneUI: API 키가 비어 있습니다.");
            if (summaryText != null)
                summaryText.text = "오류: API 키가 설정되지 않았습니다.";
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            return;
        }

        _flashModel = new GenerativeModel(apiKey.apiKey, "gemini-2.5-flash-lite");

        // PlayerPrefs에서 최종 게임 상태 로드
        string gameStateJson = PlayerPrefs.GetString("FinalGameState", "");
        
        if (string.IsNullOrEmpty(gameStateJson))
        {
            Debug.LogError("최종 게임 상태를 찾을 수 없습니다!");
            if (summaryText != null)
                summaryText.text = "게임 상태를 불러올 수 없습니다.";
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            return;
        }

        _finalGameState = JsonUtility.FromJson<GameState>(gameStateJson);
        
        // 비동기 메서드 호출 (await 없이 Task 반환)
        _ = DisplayResults();
    }

    /// <summary>
    /// 최종 결과 표시
    /// </summary>
    private async Task DisplayResults()
    {
        if (_finalGameState == null) return;

        // 생존 성공 여부 판단
        bool survived = _finalGameState.stability.stability >= 30 && _finalGameState.resources.food > 0;

        // 제목 설정
        if (resultTitleText != null)
        {
            resultTitleText.text = survived ? "임무 완수!" : "임무 실패...";
            resultTitleText.color = survived ? Color.green : Color.red;
        }

        // 안정도
        if (finalStabilitySlider != null)
        {
            finalStabilitySlider.value = _finalGameState.stability.stability;
        }
        if (stabilityValueText != null)
        {
            stabilityValueText.text = $"{_finalGameState.stability.stability}/100";
        }

        // 최종 자원
        if (finalFoodText != null)
        {
            float daysOfFood = _finalGameState.resources.food / 3f;
            finalFoodText.text = $"식량: {_finalGameState.resources.food} ({daysOfFood:F1}일치)";
        }

        // AI로 디테일한 요약 텍스트 생성
        if (summaryText != null)
        {
            try
            {
                string aiSummary = await GenerateAISummary(survived);
                summaryText.text = aiSummary;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AI 요약 생성 실패: {e.Message}");
                // 실패 시 기본 요약 사용
                summaryText.text = GenerateDetailedSummary(survived);
            }
        }

        // 로딩 패널 숨김
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// AI를 통해 최종 결산 요약 생성
    /// </summary>
    private async Task<string> GenerateAISummary(bool survived)
    {
        int stability = _finalGameState.stability.stability;
        int food = _finalGameState.resources.food;
        int turnsPlayed = 14 - _finalGameState.turnsRemaining;
        float daysOfFood = food / 3f;

        string prompt = $@"당신은 게임 결산 리포트 작성 전문가입니다.

플레이어의 게임 결과를 분석하여 상세하고 몰입감 있는 최종 평가를 작성하세요.

[게임 결과 데이터]
- 최종 결과: {(survived ? "임무 완수" : "임무 실패")}
- 최종 안정성: {stability}/100
- 최종 식량: {food}개 (약 {daysOfFood:F1}일치)
- 진행한 턴 수: {turnsPlayed}턴
- 마지막 선택: {_finalGameState.lastPlayerAction}
- 게임 배경: {_finalGameState.scene}
- 스토리 요약: {_finalGameState.plotSummary}

[작성 지침]
1. **등급 평가**: S~F 등급 중 하나를 부여하고 간단한 설명 추가
   - S (90+): 완벽한 성공
   - A (75-89): 우수한 성공
   - B (60-74): 양호한 성공
   - C (45-59): 간신히 성공
   - D (30-44): 위태로운 성공
   - F (30 미만): 실패
   - 종합 점수 = (안정성 × 0.7) + (min(식량×2, 60) × 0.3)

2. **안정성 분석**: 최종 안정성 상태에 대한 평가 (1~2문장)

3. **자원 관리 평가**: 식량 비축 상태에 대한 평가 (1~2문장)

4. **종합 평가**: 플레이어의 전체 플레이에 대한 총평 (2~3문장)
   - 성공 시: 칭찬과 긍정적 평가
   - 실패 시: 실패 원인 분석과 개선점 제시

[출력 형식]
<size=18><b>[ 결과 등급: [등급] ]</b></size>

<b>안정성 분석 ({stability}/100)</b>
[안정성 평가 내용]

<b>자원 관리 평가</b>
• 식량: {food}개 (약 {daysOfFood:F1}일치)
[자원 평가 내용]

<b>종합 평가</b>
[전체 플레이에 대한 총평]

[주의사항]
- 문학적 과장 표현 금지
- 객관적이고 명확한 평가
- 게임 배경({_finalGameState.scene})과 스토리({_finalGameState.plotSummary})를 고려한 맥락적 평가
- 한글로 작성
- 반드시 출력 형식을 정확히 따를 것";

        var response = await _flashModel.GenerateContentAsync(prompt);
        return response.Text.Trim();
    }

    /// <summary>
    /// 디테일한 최종 요약 생성
    /// </summary>
    private string GenerateDetailedSummary(bool survived)
    {
        System.Text.StringBuilder summary = new System.Text.StringBuilder();

        // === 1. 결과 등급 평가 ===
        string grade = GetResultGrade();
        summary.AppendLine($"<size=18><b>[ 결과 등급: {grade} ]</b></size>\n");

        // === 2. 안정도 평가 ===
        int stability = _finalGameState.stability.stability;
        summary.AppendLine($"<b>안정도 평가 ({stability}/100)</b>");
        summary.AppendLine(GetStabilityEvaluation(stability));
        summary.AppendLine();

        // === 3. 자원 상황 ===
        int food = _finalGameState.resources.food;
        float daysOfFood = food / 3f;
        summary.AppendLine($"<b>최종 자원 상황</b>");
        summary.AppendLine($"• 식량: {food}개 (약 {daysOfFood:F1}일치)");
        summary.AppendLine(GetFoodEvaluation(food));
        summary.AppendLine();

        // === 4. 최종 종합 평가 ===
        summary.AppendLine("<b>종합 평가</b>");
        summary.AppendLine(GetFinalComment(survived, stability, food));

        return summary.ToString();
    }

    /// <summary>
    /// 결과 등급 산정 (S, A, B, C, D, F)
    /// </summary>
    private string GetResultGrade()
    {
        int stability = _finalGameState.stability.stability;
        int food = _finalGameState.resources.food;

        // 종합 점수 (0~100)
        int totalScore = (stability * 7 + Mathf.Min(food * 2, 60) * 3) / 10;

        if (totalScore >= 90) return "S (완벽한 성공)";
        if (totalScore >= 75) return "A (우수한 성공)";
        if (totalScore >= 60) return "B (양호한 성공)";
        if (totalScore >= 45) return "C (간신히 성공)";
        if (totalScore >= 30) return "D (위태로운 성공)";
        return "F (실패)";
    }

    /// <summary>
    /// 안정도 평가 코멘트
    /// </summary>
    private string GetStabilityEvaluation(int stability)
    {
        if (stability >= 80) return "• 상황이 매우 안정적입니다. 모든 것이 순조롭게 진행되고 있습니다.";
        if (stability >= 60) return "• 상황이 안정적인 편입니다. 대부분의 문제가 해결되었습니다.";
        if (stability >= 40) return "• 상황이 불안정합니다. 여전히 많은 문제가 남아있습니다.";
        if (stability >= 20) return "• 상황이 매우 위태롭습니다. 붕괴 직전입니다.";
        if (stability > 0) return "• 상황이 거의 무너졌습니다. 혼란이 지배하고 있습니다.";
        return "• 상황이 완전히 붕괴했습니다. 모든 것이 끝났습니다.";
    }

    /// <summary>
    /// 식량 상황 평가
    /// </summary>
    private string GetFoodEvaluation(int food)
    {
        if (food >= 30) return "• 충분한 비축량입니다. 당분간 걱정이 없습니다.";
        if (food >= 15) return "• 적당한 비축량입니다. 며칠은 버틸 수 있습니다.";
        if (food >= 5) return "• 부족한 상태입니다. 곧 고갈될 위험이 있습니다.";
        if (food > 0) return "• 거의 바닥났습니다. 즉각적인 조치가 필요합니다.";
        return "• 식량이 완전히 고갈되었습니다.";
    }

    /// <summary>
    /// 최종 종합 코멘트
    /// </summary>
    private string GetFinalComment(bool survived, int stability, int food)
    {
        if (survived && stability >= 70 && food >= 20)
        {
            return "현명한 판단 덕분에 모든 상황을 훌륭하게 해결했습니다. " +
                   "안정적인 상태를 유지하며 목표를 달성했습니다.";
        }
        else if (survived && stability >= 50)
        {
            return "어려운 상황 속에서도 포기하지 않았습니다. " +
                   "비록 완벽하지는 않지만, 임무를 완수했습니다.";
        }
        else if (survived)
        {
            return "간신히 버텨냈습니다. 많은 희생과 어려움이 있었지만, " +
                   "그래도 목표를 달성했습니다. 앞으로가 더 중요합니다.";
        }
        else if (stability <= 0)
        {
            return "안정성이 붕괴되면서 모든 것이 무너졌습니다. " +
                   "상황을 통제할 수 없게 되었습니다.";
        }
        else if (food <= 0)
        {
            return "자원이 바닥나면서 임무 수행이 불가능해졌습니다. " +
                   "자원 관리의 실패가 파국을 불러왔습니다.";
        }
        else
        {
            return "여러 요인이 복합적으로 작용하며 임무에 실패했습니다. " +
                   "더 신중한 선택이 필요했습니다.";
        }
    }
}
