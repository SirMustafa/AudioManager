using UnityEngine;

[CreateAssetMenu(fileName = "New SfxData", menuName = "Audio/Sfx Data")]
public class SfxDataSO : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Volume & Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float pitchVariation = 0f;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 3f;
    public float maxDistance = 25f;
    public bool loop = false;

    public AudioClip GetRandomClip()
    {
        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomPitch()
    {
        return 1f + Random.Range(-pitchVariation, pitchVariation);
    }
}