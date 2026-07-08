using UnityEngine;

public class PlayAudioOnClick : MonoBehaviour
{
    public AudioClip audioClip; // 재생할 오디오 파일
    private AudioSource audioSource;

    public float startTime = 5.5f; // 원하는 시작 구간 (초)

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource.clip == null && audioClip != null)
        {
            audioSource.clip = audioClip;
        }
    }

    // 마우스로 에셋을 클릭했을 때 실행되는 함수
    void OnMouseDown()
    {
        // 1. 현재 오디오가 재생 중이 아닐 때만 시작합니다.
        if (!audioSource.isPlaying)
        {
            audioSource.time = startTime; // 시작 구간으로 이동
            audioSource.Play(); // 재생 시작 (끝까지 자동 재생됨)
        }
    }
}