using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;

    public AudioClip overworldMusic;
    public AudioClip shopMusic;
    public AudioClip combatMusic;

    [Header("SFX")]
    public AudioSource sfxSource;

    public AudioClip buySound;
    public AudioClip sellSound;
    public AudioClip pickupSound;
    public AudioClip hitSound;
    public AudioClip transitionSound;

    private void Awake()
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayOverworldMusic();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "overworldScene":
                PlayOverworldMusic();
                break;

            case "CombatScene":
                PlayCombatMusic();
                break;
        }
    }

    // ==========================
    // MUSIC
    // ==========================

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlayOverworldMusic()
    {
        PlayMusic(overworldMusic);
    }

    public void PlayShopMusic()
    {
        PlayMusic(shopMusic);
    }

    public void PlayCombatMusic()
    {
        PlayMusic(combatMusic);
    }

    // ==========================
    // SFX
    // ==========================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayBuy() => PlaySFX(buySound);

    public void PlaySell() => PlaySFX(sellSound);

    public void PlayPickup() => PlaySFX(pickupSound);

    public void PlayHit() => PlaySFX(hitSound);

    public void PlayTransition() => PlaySFX(transitionSound);
}