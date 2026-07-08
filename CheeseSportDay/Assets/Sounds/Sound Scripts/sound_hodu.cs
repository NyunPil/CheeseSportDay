using UnityEngine;

public class PlayAudioOnClick : MonoBehaviour
{
    public AudioClip audioClip; // 재생할 오디오 파일
    private AudioSource audioSource;

    public float startTime = 17f; // 원하는 시작 구간 (초)
    public float duration = 20f;  // 재생할 길이 (초)

    void Start()
    {
        // 1단계에서 추가한 AudioSource 가져오기
        audioSource = GetComponent<AudioSource>();
        if (audioSource.clip == null)
        {
            audioSource.clip = audioClip;
        }
    }

    // 마우스로 에셋을 클릭했을 때 실행되는 함수
    void OnMouseDown()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.time = startTime; // 시작 구간으로 이동
            audioSource.Play(); // 재생 시작

            // duration 시간만큼 재생 후 멈추기 (Invoke 이용)
            Invoke("StopAudio", duration);
        }
    }

    void StopAudio()
    {
        CancelInvoke("StopAudio");
        audioSource.Stop();
    }
}