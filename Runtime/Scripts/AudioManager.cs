using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MusicLibrarySO musicLibrary;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private GameObject soundEmitterPrefab;
    [SerializeField] private int initialPoolSize = 15;

    [Header("Mixer Params")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam = "SfxVolume";
    [SerializeField] private string lowpassParam = "LowpassFreq";

    [Header("Music")]
    [SerializeField] private float musicFadeDuration = 1f;

    private const float MinDb = -80f;

    private readonly Queue<GameObject> emitterPool = new();
    private readonly Dictionary<int, SoundEmitter> activeLoops = new();
    private int loopTokenCounter;

    private Coroutine musicCoroutine;
    private MusicTypes currentMusicType;

    public void EnsureInstance()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            emitterPool.Enqueue(CreateEmitter());
        }
    }

    public void PlaySfx(SfxDataSO data, Vector3 position)
    {
        AudioClip clip = data.GetRandomClip();
        SpawnEmitter(position).Play(clip, data);
    }

    public void PlayUiSfx(SfxDataSO data)
    {
        AudioClip clip = data.GetRandomClip();

        uiSfxSource.pitch = data.GetRandomPitch();
        uiSfxSource.volume = data.volume;
        uiSfxSource.PlayOneShot(clip);
        uiSfxSource.pitch = 1f;
    }

    public int PlayLoopSfx(SfxDataSO data, Vector3 position)
    {
        AudioClip clip = data.GetRandomClip();
        SoundEmitter emitter = SpawnEmitter(position);
        emitter.Play(clip, data);
        int token = ++loopTokenCounter;
        activeLoops[token] = emitter;
        return token;
    }

    public void StopLoopSfx(int token)
    {
        if (!activeLoops.TryGetValue(token, out var emitter))
        {
            return;
        }

        emitter.StopLoop();
        activeLoops.Remove(token);
    }

    public void UpdateLoopPosition(int token, Vector3 position)
    {
        if (!activeLoops.TryGetValue(token, out var emitter))
        {
            return;
        }

        emitter.transform.position = position;
    }

    public void StopAllLoops()
    {
        foreach (var kvp in activeLoops)
        {
            kvp.Value.StopLoop();
        }

        activeLoops.Clear();
    }

    public void PlayMusic(MusicTypes type)
    {
        if (type == MusicTypes.None || type == currentMusicType)
        {
            return;
        }

        currentMusicType = type;

        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }

        musicCoroutine = StartCoroutine(MusicLoop(type));
    }

    public void StopMusic()
    {
        currentMusicType = MusicTypes.None;

        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }

        audioMixer.DOSetFloat(musicParam, MinDb, musicFadeDuration)
            .SetUpdate(true)
            .OnComplete(() => musicSource.Stop());
    }

    public void SetLowpass(bool enabled, float duration = 0.5f)
    {
        audioMixer.DOSetFloat(lowpassParam, enabled ? 800f : 22000f, duration).SetUpdate(true);
    }

    public void SetMasterVolume(float v0to10, float duration = 0.2f)
        => audioMixer.DOSetFloat(masterParam, Vol10ToDb(v0to10), duration).SetUpdate(true);

    public void SetMusicVolume(float v0to10, float duration = 0.2f)
        => audioMixer.DOSetFloat(musicParam, Vol10ToDb(v0to10), duration).SetUpdate(true);

    public void SetSfxVolume(float v0to10, float duration = 0.2f)
        => audioMixer.DOSetFloat(sfxParam, Vol10ToDb(v0to10), duration).SetUpdate(true);

    private IEnumerator MusicLoop(MusicTypes type)
    {
        float targetDb = Vol10ToDb(GetCurrentMusicVol10());
        audioMixer.DOSetFloat(musicParam, targetDb, musicFadeDuration).SetUpdate(true);

        while (currentMusicType == type)
        {
            AudioClip clip = musicLibrary.GetRandom(type);

            musicSource.clip = clip;
            musicSource.Play();

            float waitTime = clip.length - musicFadeDuration;

            if (waitTime > 0f)
            {
                yield return new WaitForSecondsRealtime(waitTime);
            }

            audioMixer.DOSetFloat(musicParam, MinDb, musicFadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(musicFadeDuration);

            musicSource.Stop();
            audioMixer.DOSetFloat(musicParam, targetDb, musicFadeDuration).SetUpdate(true);
        }
    }

    private GameObject CreateEmitter()
    {
        var go = Instantiate(soundEmitterPrefab, transform);
        go.SetActive(false);
        return go;
    }

    private SoundEmitter SpawnEmitter(Vector3 position)
    {
        GameObject go = emitterPool.Count > 0 ? emitterPool.Dequeue() : CreateEmitter();
        go.transform.position = position;
        go.SetActive(true);
        return go.GetComponent<SoundEmitter>();
    }

    public void ReturnEmitterToPool(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        emitterPool.Enqueue(go);
    }

    private float Vol10ToDb(float v0to10)
    {
        float t = Mathf.Clamp01(v0to10 / 10f);
        return t <= 0f ? MinDb : Mathf.Max(20f * Mathf.Log10(t), MinDb);
    }

    private float GetCurrentMusicVol10()
    {
        audioMixer.GetFloat(musicParam, out float db);

        if (db <= MinDb)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.Pow(10f, db / 20f)) * 10f;
    }
}