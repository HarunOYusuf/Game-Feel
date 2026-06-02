using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Persistent background music manager.
    /// Survives scene loads so music doesn't restart when you reload a level.
    /// 
    /// Setup:
    /// 1. Create an empty GameObject called "MusicManager" in your FIRST scene
    ///    (main menu or level — wherever the game starts)
    /// 2. Add this script
    /// 3. Assign a music AudioClip
    /// 4. Tick "Play On Awake"
    /// 
    /// To change track per-scene, call MusicManager.Instance.PlayTrack(clip).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Music")]
        [Tooltip("The background music clip to loop")]
        [SerializeField] private AudioClip _musicClip;

        [Tooltip("Play the assigned clip automatically on start")]
        [SerializeField] private bool _playOnAwake = true;

        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.5f;

        [Tooltip("Fade duration when switching tracks (seconds, 0 = instant)")]
        [SerializeField] private float _fadeDuration = 1f;

        private AudioSource _source;
        private float _targetVolume;
        private bool _fading;

        private void Awake()
        {
            // Singleton + persist across scenes
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = _volume;
            _targetVolume = _volume;

            // IMPORTANT: ignore the AudioListener pause that the PauseMenu sets,
            // so music keeps playing while the game is paused. Remove this line
            // if you'd rather music also pauses with the game.
            _source.ignoreListenerPause = true;
        }

        private void Start()
        {
            if (_playOnAwake && _musicClip != null)
            {
                PlayTrack(_musicClip);
            }
        }

        private void Update()
        {
            // Handle volume fading
            if (_fading)
            {
                _source.volume = Mathf.MoveTowards(
                    _source.volume,
                    _targetVolume,
                    (_fadeDuration > 0 ? (1f / _fadeDuration) : 100f) * Time.unscaledDeltaTime
                );

                if (Mathf.Approximately(_source.volume, _targetVolume))
                {
                    _fading = false;
                }
            }
        }

        /// <summary>
        /// Play a music track. If it's already playing, does nothing.
        /// </summary>
        public void PlayTrack(AudioClip clip)
        {
            if (clip == null) return;

            // Don't restart the same clip if it's already playing
            if (_source.clip == clip && _source.isPlaying) return;

            _source.clip = clip;
            _source.volume = _volume;
            _targetVolume = _volume;
            _source.Play();
        }

        /// <summary>
        /// Stop the music.
        /// </summary>
        public void StopMusic()
        {
            _source.Stop();
        }

        /// <summary>
        /// Set the music volume (0–1).
        /// </summary>
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            _targetVolume = _volume;
            _fading = true;
        }

        /// <summary>
        /// Fade the music out to silence over the fade duration.
        /// </summary>
        public void FadeOut()
        {
            _targetVolume = 0f;
            _fading = true;
        }

        /// <summary>
        /// Fade the music back in to its set volume.
        /// </summary>
        public void FadeIn()
        {
            _targetVolume = _volume;
            _fading = true;
        }
    }
}