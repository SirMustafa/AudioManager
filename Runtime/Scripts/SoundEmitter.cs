using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Play(AudioClip clip, SfxDataSO data)
    {
        CancelInvoke();
        audioSource.clip = clip;
        audioSource.volume = data.volume;
        audioSource.pitch = data.GetRandomPitch();
        audioSource.spatialBlend = data.spatialBlend;
        audioSource.minDistance = data.minDistance;
        audioSource.maxDistance = data.maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.loop = data.loop;
        audioSource.Play();

        if (!data.loop)
        {
            float duration = clip.length / Mathf.Abs(audioSource.pitch);
            Invoke(nameof(ReturnToPool), duration + 0.05f);
        }
    }

    public void StopLoop()
    {
        CancelInvoke();
        audioSource.Stop();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        audioSource.Stop();
        audioSource.clip = null;
        AudioManager.Instance.ReturnEmitterToPool(gameObject);
    }

    private void OnDisable()
    {
        CancelInvoke();
        audioSource.Stop();
        audioSource.clip = null;
    }
}