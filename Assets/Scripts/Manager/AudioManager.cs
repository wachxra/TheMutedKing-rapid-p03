using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;
    }

    [Header("SFX Library")]
    public List<Sound> sfxSounds = new List<Sound>();

    [Header("Music Library")]
    public List<Sound> musicSounds = new List<Sound>();

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private Dictionary<string, Sound> sfxDict = new Dictionary<string, Sound>();
    private Dictionary<string, Sound> musicDict = new Dictionary<string, Sound>();

    void Awake()
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

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        foreach (Sound s in sfxSounds)
            sfxDict[s.name] = s;

        foreach (Sound s in musicSounds)
            musicDict[s.name] = s;
    }

    public void PlaySFX(string soundName)
    {
        if (sfxDict.TryGetValue(soundName, out Sound s))
        {
            sfxSource.PlayOneShot(s.clip, s.volume);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX '{soundName}' not found!");
        }
    }

    public void PlayMusic(string soundName)
    {
        if (musicDict.TryGetValue(soundName, out Sound s))
        {
            musicSource.clip = s.clip;
            musicSource.volume = s.volume;
            musicSource.loop = s.loop;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Music '{soundName}' not found!");
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void StopAllSFX()
    {
        sfxSource.Stop();
    }
}