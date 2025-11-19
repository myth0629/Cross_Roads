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
		bool isRandomTheme = currentTheme == "Random";
		bool isFirstTurn = string.IsNullOrEmpty(state.lastPlayerAction) || state.lastPlayerAction == "GameStart";

		string randomThemeInstruction = string.Empty;
		string themeGuideline = string.Empty;
		
		if (isRandomTheme)
		{
			// Random 테마: 기존 테마 가이드라인을 완전히 배제
			if (isFirstTurn)
			{
				randomThemeInstruction =
					"🎲 **랜덤 테마 모드 - 새 시나리오 창조**\n\n" +
					"**절대 금지:**\n" +
					"- The Awakening AI (아르케, 도시 관리 AI)\n" +
					"- Concrete Utopia (황궁아파트, 입주민 대표)\n" +
					"- The Devil's Advocate (로펌 변호사)\n" +
					"위 3개 시나리오는 절대 사용하지 마세요.\n\n" +
					
					"**당신의 임무: 완전히 새로운 세계관 즉시 창조**\n" +
					"지금 당장 아래 항목을 모두 채워 새로운 시나리오를 만드세요:\n\n" +
					
					"1. **시나리오 이름**: 창의적으로 작명 (예: '심해 기지 크라켄', '시간 감시국')\n" +
					"2. **배경**: 구체적 시간/장소/상황 (예: 2089년 화성 이주선, 1920년대 비밀 결사)\n" +
					"3. **플레이어 역할**: 명확한 직책 (예: 우주 정거장 사령관, 혁명군 지휘관)\n" +
					"4. **핵심 딜레마**: 두 가치의 충돌 (예: 생존 vs 윤리, 진실 vs 평화)\n" +
					"5. **stability 변수 의미**: 시나리오에 맞게 재정의 (예: '선원 사기', '시공간 안정성', '조직 충성도')\n" +
					"6. **주요 세력**: 2~3개 집단/인물 (예: 과학자 파벌, 군부, 외계 생명체)\n" +
					"7. **톤**: 분위기 (예: 하드 SF, 정치 스릴러, 호러)\n\n" +
					
					"**중요:** 위 요소들을 'plotSummary'에 간단히 기록하여 다음 턴에서 참고할 수 있게 하세요.\n" +
					"지금 만든 세계관은 게임 끝까지 유지됩니다.\n\n";
			}
			else
			{
				randomThemeInstruction =
					"⚠️ **랜덤 테마 - 일관성 유지**\n\n" +
					"**절대 금지:** 아르케(AI), 황궁아파트, 로펌 변호사 언급 시 실패\n" +
					"**필수:** 이미 당신이 만든 새로운 세계관을 유지하세요.\n" +
					"GameState.plotSummary와 이전 상황을 참고하여 일관된 스토리를 이어가세요.\n\n";
			}
		}
		else
		{
			// 기존 테마 (AwakeningAI, ConcreteUtopia, DevilsAdvocate)
			themeGuideline = GetThemeGuideline(currentTheme);
		}			string caseAInstruction = isRandomTheme
				? "* **CASE A (GameStart):** 랜덤 테마 모드에서는 지금 막 정의한 새로운 세계관의 배경/역할/딜레마를 소개하고, 플레이어가 직면한 첫 번째 위기를 3문장 구조에 맞춰 제시하세요.\n"
				: "* **CASE A (GameStart):** 'lastPlayerAction'이 \"GameStart\"이거나 null이라면, 위 시나리오의 초기 배경과 첫 번째 딜레마를 묘사합니다.\n";

		return
			"---\n" +
			"Role:\n" +
			"당신은 '딜레마 기반' 시뮬레이션 게임의 AI 게임 마스터(GM)입니다.\n" +
			"당신의 임무는 플레이어의 선택에 따른 냉혹한 '결과'와 그로 인한 '새로운 딜레마'를 생성하는 것입니다.\n\n" +
			
			"Input (GameState JSON):\n" +
			"당신은 항상 'GameState'라는 JSON 객체를 입력받습니다.\n" +
			"이 객체는 현재 게임의 모든 맥락(줄거리 요약, 안정성)과 플레이어의 '마지막 선택'('lastPlayerAction')을 포함합니다.\n\n" +
			
			"---\n" +
			(isRandomTheme 
				? randomThemeInstruction  // Random일 때는 랜덤 지시만 출력
				: "선택된 시나리오 테마:\n" + themeGuideline + "\n\n") +  // 기존 테마일 때만 가이드라인 출력
			
			"**중요: 위 시나리오 설정을 게임 끝까지 일관되게 유지하세요.**\n" +
			"* **변수 매핑 규칙 (아주 중요):** JSON 키 값은 `stability`(안정성)를 사용하되, 상황 묘사에서는 위 시나리오에 맞게 표현하세요.\n" +
			"* **현실적인 디테일**: 추상적인 표현 대신 구체적인 인물, 장소, 숫자를 사용하세요.\n\n" +			"---\n" +
			"Task (Internal Step-by-Step Logic):\n" +
			"당신은 반드시 다음 두 가지 단계를 순서대로 생각하고, 그 결과를 *하나의* JSON 객체로 합쳐서 출력해야 합니다.\n\n" +
			
				"**[Step 1: 'situation_text' 생성]**\n" +
				"* 'GameState'의 'lastPlayerAction'을 분석합니다.\n" +
				caseAInstruction +
				"    * **CASE B (진행 중):** 그 외의 경우, 이전 선택('lastPlayerAction')이 가져온 즉각적인 결과를 묘사합니다.\n\n" +
			
			"* **3문장 구조 (절대 준수):**\n" +
			"  **분량: 총 200자 이내 (공백 포함), 건조하고 직관적인 '보고서' 또는 '뉴스 속보' 톤.**\n" +
			"  1. **[현황]** 이전 선택의 결과 (예: \"구조조정을 단행하자 주가는 올랐지만 직원들이 파업을 시작했습니다.\")\n" +
			"  2. **[위기]** 지금 발생한 단 하나의 구체적 사건 (예: \"노조위원장이 사장실 점거를 시도합니다.\")\n" +
			"  3. **[질문]** 행동 촉구 (예: \"경찰을 부르시겠습니까, 직접 대화하시겠습니까?\")\n\n" +
			
			"**[Step 2: 'choices' 및 'state_update' 생성]**\n" +
			"* 위 상황에 기반한 4가지 선택지를 생성하고, 각 선택에 따른 수치 변화를 계산합니다.\n" +
			"* **딜레마 설계 원칙:**\n" +
			"  플레이어를 괴롭히기 위해, 대부분의 선택지는 장단점이 명확하게 설계하세요.\n" +
			"  - 안전한 선택 -> 안정성 소폭 상승 또는 유지\n" +
			"  - 공격적인 선택 -> 안정성 하락 위험, 하지만 문제 해결 가능\n" +
			"  - 위험한 도박 -> 성공 시 대박, 실패 시 쪽박\n\n" +
			
			"---\n" +
			"Output Format (MUST BE JSON):\n" +
			"설명 없이 아래 JSON 형식만 출력하세요.\n" +
			"**state_update 규칙 (아주 중요)**:\n" +
			"1. 'GameStart'일 경우 수치를 변경하지 마세요 (현재 값 그대로 유지).\n" +
			"2. 그 외엔 'lastPlayerAction'의 결과로 변동된 최종 수치를 계산해 넣으세요.\n" +
			"3. **stability(안정성) 변화 제한 (필수)**:\n" +
			"   - 현재 안정성에서 **최대 -20 ~ +20 범위 내**에서만 변화하세요.\n" +
			"   - 예: 현재 80이면 → 60~100 범위 내 / 현재 30이면 → 10~50 범위 내\n" +
			"   - 일반적인 경우: ±5~10 정도의 소폭 변화\n" +
			"   - 심각한 실패: -15~-20 (최대치)\n" +
			"   - 큰 성공: +10~+15\n" +
			"   - **절대 한 번에 -50 이상 떨어지지 않도록 주의하세요!**\n\n" +
			
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
			"    \"stability\": {\n" +
			"      \"stability\": 65\n" +
			"    }\n" +
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
				       "  * stability → '시민 만족도' (AI 통치에 대한 시민들의 신뢰와 순응도)\n" +
				       "- **주요 세력**: 시민단체, 정부 감시위원회, 기술 기업\n" +
				       "- **톤**: 냉정하고 논리적이되, 인간성에 대한 의문을 던지는 SF 스릴러 분위기";

			case "ConcreteUtopia":
				return "**시나리오: Concrete Utopia**\n" +
				       "- **배경**: 대지진 이후 유일하게 남은 아파트 단지 '황궁아파트'. 전기, 수도, 난방이 작동하는 이곳에 생존자들이 몰려들고 있습니다. 당신은 입주민 대표입니다.\n" +
				       "- **플레이어 역할**: 황궁아파트 입주민 대표\n" +
				       "- **핵심 딜레마**: 외부 생존자를 받아들일 것인가, 내부 질서를 지킬 것인가.\n" +
				       "- **변수 매핑**:\n" +
				       "  * stability → '입주민 단결도' (내부 갈등과 외부 위협 사이의 균형)\n" +
				       "- **주요 세력**: 원주민(기존 입주민), 신입자(외부 생존자), 경비대\n" +
				       "- **톤**: 긴박하고 현실적인 재난 생존 드라마, 인간성의 경계를 시험";

			case "DevilsAdvocate":
				return "**시나리오: The Devil's Advocate**\n" +
				       "- **배경**: 대형 로펌 '정의법률'의 에이스 변호사인 당신에게 거대 제약회사의 치명적 결함 은폐 사건이 맡겨졌습니다. 승소하면 파트너 승진, 패소하면 커리어 종말.\n" +
				       "- **플레이어 역할**: 대형 로펌 변호사\n" +
				       "- **핵심 딜레마**: 법의 테두리 안에서 정의와 성공 사이를 오갑니다.\n" +
				       "- **변수 매핑**:\n" +
				       "  * stability → '평판' (의뢰인, 로펌, 법조계, 여론 사이에서의 신뢰도)\n" +
				       "- **주요 세력**: 로펌 파트너진, 의뢰인(제약회사), 피해자 유가족\n" +
				       "- **톤**: 긴장감 넘치는 법정 스릴러, 도덕적 회색지대";

			case "Random":
				return "**시나리오: 랜덤 테마 (AI 자동 생성)**\n" +
				       "- **지시사항**: 당신은 창의적인 시나리오 디자이너입니다. 완전히 새롭고 독창적인 딜레마 기반 시나리오를 즉석에서 생성하세요.\n" +
				       "- **필수 요소**:\n" +
				       "  1. **배경**: 구체적인 시간, 장소, 상황 설정 (현실적이거나 SF/판타지 가능)\n" +
				       "  2. **플레이어 역할**: 명확한 직책/신분 (예: 우주선 선장, 비밀조직 리더, 시간여행자 등)\n" +
				       "  3. **핵심 딜레마**: 두 가치 사이의 근본적 갈등 (예: 진실 vs 평화, 개인 vs 집단)\n" +
				       "  4. **변수 매핑**: stability를 시나리오에 맞게 재해석 (예: '선원 사기', '조직 충성도', '시공간 안정성')\n" +
				       "  5. **주요 세력**: 2-3개의 대립/협력 가능한 집단/개인\n" +
				       "  6. **톤**: 시나리오에 어울리는 분위기 (스릴러, 드라마, SF, 미스터리 등)\n" +
				       "- **제약 조건**:\n" +
				       "  * AwakeningAI, ConcreteUtopia, DevilsAdvocate와는 완전히 다른 설정\n" +
				       "  * 14턴 안에 의미 있는 선택과 결과가 나올 수 있는 구조\n" +
				       "  * 도덕적/전략적 딜레마가 명확한 설정\n" +
				       "- **예시 아이디어** (이것을 그대로 사용하지 말고 영감만 받을 것):\n" +
				       "  * 화성 이주선 선장으로서 자원과 생존자 관리\n" +
				       "  * 비밀 정보기관 책임자로서 국가안보와 인권 사이 선택\n" +
				       "  * 시간 감시관으로서 역사 개입 여부 결정\n" +
				       "  * 지하 레지스탕스 리더로서 테러와 해방 사이 고민\n" +
				       "  * 로봇 윤리위원회 위원장으로서 AI 권리 문제 해결\n" +
				       "\n**중요**: 이 시나리오 설정은 게임 첫 턴부터 일관되게 유지하세요. 매 턴 상황을 생성할 때 이 설정을 참고하세요.";

			default:
				// 알 수 없는 테마인 경우 AwakeningAI를 기본값으로
				return GetThemeGuideline("AwakeningAI");
		}
	}

	/// <summary>
	/// 게임 오버 시 상황 설명을 위한 프롬프트 생성
	/// </summary>
	public static string BuildGameOverPrompt(GameState state)
	{
		string gameStateJson = JsonUtility.ToJson(state, prettyPrint: true);
		string themeGuideline = GetThemeGuideline(state.selectedTheme);

		return
			"---\n" +
			"Role:\n" +
			"당신은 '딜레마 기반' 시뮬레이션 게임의 AI 게임 마스터(GM)입니다.\n" +
			"플레이어의 안정성이 0이 되어 게임이 종료되었습니다.\n" +
			"당신의 임무는 게임이 끝나게 된 직접적인 원인과 상황을 극적으로 묘사하는 것입니다.\n\n" +
			
			"선택된 시나리오 테마:\n" +
			themeGuideline + "\n\n" +
			
			"Input (Final GameState JSON):\n" +
			gameStateJson + "\n\n" +
			
			"Task:\n" +
			"위 GameState와 시나리오 배경을 바탕으로, 안정성이 0이 되어 모든 것이 무너진 순간을 3문장으로 묘사하세요.\n" +
			"- **1문장**: 플레이어의 마지막 선택('lastPlayerAction')이 가져온 결정적인 사건\n" +
			"- **2문장**: 그로 인해 발생한 연쇄적인 붕괴 과정 (구체적 인물, 장소, 숫자 포함)\n" +
			"- **3문장**: 최종 상태 - 모든 것이 무너진 현재 상황\n\n" +
			
			"톤: 냉정하고 비극적이며 극적인 뉴스 속보 스타일. 총 200자 이내.\n\n" +
			
			"Output Format (JSON):\n" +
			"반드시 아래와 같은 JSON 형식으로만 응답해야 합니다.\n" +
			"다른 설명이나 마크다운 코드블록(```json)은 절대 포함하지 마세요. 순수 JSON만 출력하세요.\n\n" +
			"{\n" +
			"  \"gameover_text\": \"(3문장으로 구성된 게임 오버 상황 묘사)\"\n" +
			"}\n" +
			"---\n";
	}
}

