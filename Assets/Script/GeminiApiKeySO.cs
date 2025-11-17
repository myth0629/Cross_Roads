using UnityEngine;

// Gemini API 키를 보관하는 ScriptableObject
[CreateAssetMenu(fileName = "GeminiApiKeySO", menuName = "Config/Gemini API Key")]
public class GeminiApiKeySO : ScriptableObject
{
	[Tooltip("Google AI Studio에서 발급받은 Gemini API 키")]
	public string apiKey;
}
