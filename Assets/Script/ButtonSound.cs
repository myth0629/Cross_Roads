using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // 이 스크립트를 넣으면 버튼 컴포넌트가 필수임
public class ButtonSound : MonoBehaviour
{
    public AudioClip clickSound; // 인스펙터에서 소리 파일을 넣을 변수
    private AudioSource audioSource;

    void Start()
    {
        Button btn = GetComponent<Button>();
        
        // 이 오브젝트에 AudioSource가 없으면 자동으로 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // 시작하자마자 소리나면 안 되니까 끔
            audioSource.volume = 0.2f; // 소리 크기
        }

        // 버튼 클릭 시 PlaySound 함수 실행 연결
        btn.onClick.AddListener(PlaySound);
    }

    void PlaySound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound); // 소리 한 번 재생
        }
    }
}