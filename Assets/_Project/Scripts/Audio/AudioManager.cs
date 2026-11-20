using UnityEngine;

namespace GuardiaoDoFarol
{
    public enum SoundEffect
    {
        Footstep,
        Pickup,
        Deliver,
        Coin,
        DialogueOpen,
        CustomerEnter,
        Victory,
        Defeat,
        ButtonClick,
        WrongItem,
        CustomerLeave,
        ShiftBell,
        DayEnd
    }

    public enum MusicTrack
    {
        None,
        Menu,
        Tavern
    }

    /// <summary>
    /// Música e efeitos sonoros. Persiste entre cenas (DontDestroyOnLoad).
    /// Todos os clipes são opcionais: clipes vazios são simplesmente ignorados.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Fontes de áudio (criadas automaticamente se vazias)")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.4f;

        [Header("Músicas (loop)")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip tavernMusic;

        [Header("Efeitos")]
        [SerializeField] private AudioClip footstep;
        [SerializeField] private AudioClip pickup;
        [SerializeField] private AudioClip deliver;
        [SerializeField] private AudioClip coin;
        [SerializeField] private AudioClip dialogueOpen;
        [SerializeField] private AudioClip customerEnter;
        [SerializeField] private AudioClip victory;
        [SerializeField] private AudioClip defeat;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip wrongItem;
        [SerializeField] private AudioClip customerLeave;
        [SerializeField] private AudioClip shiftBell;
        [SerializeField] private AudioClip dayEnd;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.ignoreListenerPause = true;
            musicSource.volume = musicVolume;

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.ignoreListenerPause = true;
            sfxSource.volume = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- API estática (segura mesmo sem AudioManager na cena) ----------

        public static void Play(SoundEffect effect)
        {
            if (Instance != null) Instance.PlayEffect(effect);
        }

        public static void PlayMusic(MusicTrack track)
        {
            if (Instance != null) Instance.PlayTrack(track);
        }

        // ---------- API de instância ----------

        public void PlayEffect(SoundEffect effect)
        {
            switch (effect)
            {
                case SoundEffect.Footstep: PlayClip(footstep, footstepVolume); break;
                case SoundEffect.Pickup: PlayClip(pickup, 1f); break;
                case SoundEffect.Deliver: PlayClip(deliver, 1f); break;
                case SoundEffect.Coin: PlayClip(coin, 1f); break;
                case SoundEffect.DialogueOpen: PlayClip(dialogueOpen, 1f); break;
                case SoundEffect.CustomerEnter: PlayClip(customerEnter, 1f); break;
                case SoundEffect.Victory: PlayClip(victory, 1f); break;
                case SoundEffect.Defeat: PlayClip(defeat, 1f); break;
                case SoundEffect.ButtonClick: PlayClip(buttonClick, 1f); break;
                case SoundEffect.WrongItem: PlayClip(wrongItem, 1f); break;
                case SoundEffect.CustomerLeave: PlayClip(customerLeave, 1f); break;
                case SoundEffect.ShiftBell: PlayClip(shiftBell, 1f); break;
                case SoundEffect.DayEnd: PlayClip(dayEnd, 1f); break;
            }
        }

        public void PlayTrack(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Menu: PlayMusicClip(menuMusic); break;
                case MusicTrack.Tavern: PlayMusicClip(tavernMusic); break;
                default: StopMusic(); break;
            }
        }

        public void StopMusic()
        {
            if (musicSource == null) return;
            musicSource.Stop();
            musicSource.clip = null;
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicVolume;
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        private void PlayMusicClip(AudioClip clip)
        {
            if (musicSource == null) return;
            if (clip == null)
            {
                StopMusic();
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        private void PlayClip(AudioClip clip, float volumeScale)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        private void OnValidate()
        {
            if (musicSource != null) musicSource.volume = musicVolume;
        }
    }
}
