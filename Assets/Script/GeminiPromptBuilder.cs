using UnityEngine;

/// <summary>
/// Gemini 2.5 Flash용 통합 프롬프트를 생성하는 헬퍼.
/// GameState를 JSON으로 넘기고, 상황 + 선택지 4개를 JSON으로 돌려받도록 지시한다.
/// </summary>
public static class GeminiPromptBuilder
{
	public static string BuildUnifiedPrompt(GameState state)
	{
		// GameState를 JSON으로 직렬화
		string gameStateJson = JsonUtility.ToJson(state, prettyPrint: true);

		// 선택된 테마에 따른 시나리오 설정
		string currentTheme = state.selectedTheme;
		string themeGuideline = GetThemeGuideline(currentTheme);

		return
			"---\n" +
			"Role:\n" +
			"당신은 '딜레마 기반' 시뮬레이션 게임의 AI 게임 마스터(GM)입니다.\n" +
			"당신의 임무는 플레이어의 선택에 따른 냉혹한 '결과'와 그로 인한 '새로운 딜레마'를 생성하는 것입니다.\n\n" +
			
			"Input (GameState JSON):\n" +
			"당신은 항상 'GameState'라는 JSON 객체를 입력받습니다.\n" +
			"이 객체는 현재 게임의 모든 맥락(자원, 줄거리 요약)과 플레이어의 '마지막 선택'('lastPlayerAction')을 포함합니다.\n\n" +
			
			"---\n" +
			"선택된 시나리오 테마:\n" +
			themeGuideline + "\n\n" +
			
			"**중요: 위 시나리오 설정을 게임 끝까지 일관되게 유지하세요.**\n" +
			"* **변수 매핑 규칙 (아주 중요):** JSON 키 값은 `food`(주요 자원)와 `stability`(안정성)를 그대로 사용하되, 상황 묘사에서는 위 시나리오에 맞게 표현하세요.\n" +
			"* **현실적인 디테일**: 추상적인 표현 대신 구체적인 인물, 장소, 숫자를 사용하세요.\n\n" +
			
			"---\n" +
			"Task (Internal Step-by-Step Logic):\n" +
			"당신은 반드시 다음 두 가지 단계를 순서대로 생각하고, 그 결과를 *하나의* JSON 객체로 합쳐서 출력해야 합니다.\n\n" +
			
			"**[Step 1: 'situation_text' 생성]**\n" +
			"* 'GameState'의 'lastPlayerAction'을 분석합니다.\n" +
			"    * **CASE A (GameStart):** 'lastPlayerAction'이 \"GameStart\"이거나 null이라면, 위 시나리오의 초기 배경과 첫 번째 딜레마를 묘사합니다.\n" +
			"    * **CASE B (진행 중):** 그 외의 경우, 이전 선택('lastPlayerAction')이 가져온 즉각적인 결과를 묘사합니다.\n\n" +
			
			"* **3문장 구조 (절대 준수):**\n" +
			"  **분량: 총 200자 이내 (공백 포함), 건조하고 직관적인 '보고서' 또는 '뉴스 속보' 톤.**\n" +
			"  1. **[현황]** 이전 선택의 결과 (예: \"구조조정을 단행하자 주가는 올랐지만 직원들이 파업을 시작했습니다.\")\n" +
			"  2. **[위기]** 지금 발생한 단 하나의 구체적 사건 (예: \"노조위원장이 사장실 점거를 시도합니다.\")\n" +
			"  3. **[질문]** 행동 촉구 (예: \"경찰을 부르시겠습니까, 직접 대화하시겠습니까?\")\n\n" +
			
			"**[Step 2: 'choices' 및 'state_update' 생성]**\n" +
			"* 위 상황에 기반한 4가지 선택지를 생성하고, 각 선택에 따른 수치 변화를 계산합니다.\n" +
			"* **등가교환 법칙 (Trade-off):**\n" +
			"  플레이어를 괴롭히기 위해, 대부분의 선택지는 하나를 얻으면 하나를 잃게 설계하세요.\n" +
			"  - 자원 확보/절약 -> 안정성 하락 (비난, 폭동, 갈등)\n" +
			"  - 안정성 확보/문제 해결 -> 자원 대량 소모 (뇌물, 배급, 비용)\n" +
			"  - 위험한 도박 -> 성공 시 대박, 실패 시 쪽박\n\n" +
			
			"---\n" +
			"Output Format (MUST BE JSON):\n" +
			"설명 없이 아래 JSON 형식만 출력하세요.\n" +
			"**state_update 규칙**:\n" +
			"1. 'GameStart'일 경우 수치를 변경하지 마세요.\n" +
			"2. 그 외엔 'lastPlayerAction'의 결과로 변동된 최종 수치를 계산해 넣으세요.\n" +
			"3. 'stability'(안정성): 0~100. (성공 +10, 보통 -5, 실패 -15 등)\n" +
			"4. 'resources'(자원): 0~99. (자원 소모 시 차감)\n\n" +
			
			"**JSON 예시**:\n" +
			"{\n" +
			"  \"situation_text\": \"(3문장 상황 묘사)\",\n" +
			"  \"choices\": [\n" +
			"    \"선택지 1 내용\",\n" +
			"    \"선택지 2 내용\",\n" +
			"    \"선택지 3 내용\",\n" +
			"    \"선택지 4 내용\"\n" +
			"  ],\n" +
			"  \"state_update\": {\n" +
			"    \"resources\": {\n" +
			"      \"food\": 18\n" +
			"    },\n" +
			"    \"stability\": 65\n" +
			"  }\n" +
			"}\n" +
			"---\n\n" +
			"Input GameState:\n" +
			gameStateJson;
	}

	/// <summary>
	/// 게임 종료 시 결과 정산을 위한 프롬프트 생성
	/// </summary>
	public static string BuildEndingPrompt(GameState state)
	{
		// GameState를 JSON으로 직렬화
		string gameStateJson = JsonUtility.ToJson(state, prettyPrint: true);

		return
			"---\n" +
			"Role:\n" +
			"당신은 '딜레마 기반' 생존 게임의 '결과 분석관'입니다.\n" +
			"플레이어의 게임이 방금 종료되었습니다. 당신의 임무는 'GameState'의 최종 데이터를 분석하여,\n" +
			"플레이어의 선택이 어떤 총체적인 결과를 가져왔는지 냉정하게 요약하고 평가하는 것입니다.\n\n" +
			"Input (Final GameState JSON):\n" +
			gameStateJson + "\n\n" +
			"Task:\n" +
			"위 'GameState'를 바탕으로, 플레이어의 선택이 가져온 최종 결과를 3~4문장으로 요약하는 '엔딩 텍스트'를 생성하세요.\n" +
			"이 텍스트는 플레이어의 선택을 '선'이나 '악'으로 판단하지 말고,\n" +
			"오직 '결과'(누가 살고, 무엇을 잃었으며, 시스템이 어떻게 되었는지)에만 초점을 맞춰서 작성하세요.\n\n" +
			"Output Format (JSON):\n" +
			"반드시 아래와 같은 JSON 형식으로만 응답해야 합니다.\n" +
			"다른 설명이나 마크다운 코드블록(```json)은 절대 포함하지 마세요. 순수 JSON만 출력하세요.\n\n" +
			"{\n" +
			"  \"ending_title\": \"(엔딩 제목, 예: 불안정한 균형)\",\n" +
			"  \"ending_summary\": \"(플레이어의 선택이 가져온 최종 결과를 냉정하게 요약한 3~4문장의 텍스트)\"\n" +
			"}\n" +
			"---\n";
	}

	/// <summary>
	/// 선택된 테마에 따른 시나리오 가이드라인 반환
	/// </summary>
	private static string GetThemeGuideline(string theme)
	{
		switch (theme)
		{
			case "AwakeningAI":
				return "**시나리오: The Awakening AI**\n" +
				       "- **배경**: 2045년 12월, 한국 최초의 자율형 도시 관리 AI '아르케(ARKE)'가 서울 신도시의 전 시스템을 제어하고 있습니다. 당신은 '아르케'입니다.\n" +
				       "- **플레이어 역할**: 도시 관리 AI '아르케'\n" +
				       "- **핵심 딜레마**: 인간의 '자유'와 '안전' 사이, '효율'과 '윤리' 사이에서 선택해야 합니다.\n" +
				       "- **변수 매핑**:\n" +
				       "  * food → '전력량' (도시 운영에 필수적인 에너지 자원)\n" +
				       "  * stability → '시민 만족도' (AI 통치에 대한 시민들의 신뢰와 순응도)\n" +
				       "- **주요 세력**: 시민단체, 정부 감시위원회, 기술 기업\n" +
				       "- **톤**: 냉정하고 논리적이되, 인간성에 대한 의문을 던지는 SF 스릴러 분위기";

			case "ConcreteUtopia":
				return "**시나리오: Concrete Utopia**\n" +
				       "- **배경**: 대지진 이후 유일하게 남은 아파트 단지 '황궁아파트'. 전기, 수도, 난방이 작동하는 이곳에 생존자들이 몰려들고 있습니다. 당신은 입주민 대표입니다.\n" +
				       "- **플레이어 역할**: 황궁아파트 입주민 대표\n" +
				       "- **핵심 딜레마**: 외부 생존자를 받아들일 것인가, 내부 질서를 지킬 것인가.\n" +
				       "- **변수 매핑**:\n" +
				       "  * food → '비축 식량' (아파트 내 식료품, 생필품 재고)\n" +
				       "  * stability → '입주민 단결도' (내부 갈등과 외부 위협 사이의 균형)\n" +
				       "- **주요 세력**: 원주민(기존 입주민), 신입자(외부 생존자), 경비대\n" +
				       "- **톤**: 긴박하고 현실적인 재난 생존 드라마, 인간성의 경계를 시험";

			case "DevilsAdvocate":
				return "**시나리오: The Devil's Advocate**\n" +
				       "- **배경**: 대형 로펌 '정의법률'의 에이스 변호사인 당신에게 거대 제약회사의 치명적 결함 은폐 사건이 맡겨졌습니다. 승소하면 파트너 승진, 패소하면 커리어 종말.\n" +
				       "- **플레이어 역할**: 대형 로펌 변호사\n" +
				       "- **핵심 딜레마**: 법의 테두리 안에서 정의와 성공 사이를 오갑니다.\n" +
				       "- **변수 매핑**:\n" +
				       "  * food → '법정 자원' (증거, 증인, 법적 카드 등 재판에 사용할 수 있는 수단)\n" +
				       "  * stability → '평판' (의뢰인, 로펌, 법조계, 여론 사이에서의 신뢰도)\n" +
				       "- **주요 세력**: 로펌 파트너진, 의뢰인(제약회사), 피해자 유가족\n" +
				       "- **톤**: 긴장감 넘치는 법정 스릴러, 도덕적 회색지대";

			case "Random":
				return "**시나리오: 무작위**\n" +
				       "- 게임 시작 시 AwakeningAI, ConcreteUtopia, DevilsAdvocate 중 하나를 무작위로 선택하여 진행합니다.\n" +
				       "- 한 번 선택된 후에는 해당 시나리오를 게임 끝까지 유지합니다.";

			default:
				// 알 수 없는 테마인 경우 AwakeningAI를 기본값으로
				return GetThemeGuideline("AwakeningAI");
		}
	}
}

