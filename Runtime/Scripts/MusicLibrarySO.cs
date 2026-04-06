using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicLibrary")]
public class MusicLibrarySO : ScriptableObject
{
    [Serializable]
    public class MusicCategory
    {
        public MusicTypes type;
        public AudioClip[] clips;
    }

    [SerializeField] private List<MusicCategory> categories = new();
    [SerializeField] private bool spatialize = false;

    private Dictionary<MusicTypes, AudioClip[]> categoryMap;

    private void OnEnable() => BuildMap();
    private void OnValidate() => BuildMap();

    private void BuildMap()
    {
        categoryMap = new Dictionary<MusicTypes, AudioClip[]>(categories.Count);

        foreach (var cat in categories)
        {
            if (!categoryMap.ContainsKey(cat.type))
            {
                categoryMap.Add(cat.type, cat.clips);
            }
        }
    }

    public AudioClip GetRandom(MusicTypes type)
    {
        return categoryMap[type][UnityEngine.Random.Range(0, categoryMap[type].Length)];
    }

    public bool Spatialize => spatialize;
}