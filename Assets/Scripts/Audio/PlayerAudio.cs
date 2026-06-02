using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Plays sound effects in response to the player controller's events.
    /// Attach this to the SAME GameObject as your UltimatePlayerController,
    /// or assign the controller reference manually.
    /// 
    /// Hooks into the events the controller already fires:
    ///   - Jumped       → jump SFX
    ///   - DashChanged  → dash SFX (when dash starts)
    ///   - GroundedChanged → land SFX (when landing)
    ///   - WallSlideChanged → wall slide SFX (optional)
    /// 
    /// Setup:
    /// 1. Add this script to the Player GameObject
    /// 2. It auto-adds an AudioSource if missing
    /// 3. Assign the AudioClips in the Inspector
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Controller Reference")]
        [Tooltip("The player controller to listen to. Auto-found if left empty.")]
        [SerializeField] private UltimatePlayerController _controller;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip _jumpClip;
        [SerializeField] private AudioClip _dashClip;
        [SerializeField] private AudioClip _landClip;
        [SerializeField] private AudioClip _wallSlideClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _jumpVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _dashVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float _landVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _wallSlideVolume = 0.4f;

        [Header("Land SFX Settings")]
        [Tooltip("Only play land sound if falling faster than this (avoids tiny bumps)")]
        [SerializeField] private float _minLandSpeed = 2f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;

            if (_controller == null)
            {
                _controller = GetComponent<UltimatePlayerController>();
            }
        }

        private void OnEnable()
        {
            if (_controller == null) return;

            _controller.Jumped += OnJumped;
            _controller.DashChanged += OnDashChanged;
            _controller.GroundedChanged += OnGroundedChanged;
            _controller.WallSlideChanged += OnWallSlideChanged;
        }

        private void OnDisable()
        {
            if (_controller == null) return;

            _controller.Jumped -= OnJumped;
            _controller.DashChanged -= OnDashChanged;
            _controller.GroundedChanged -= OnGroundedChanged;
            _controller.WallSlideChanged -= OnWallSlideChanged;
        }

        private void OnJumped()
        {
            PlayClip(_jumpClip, _jumpVolume);
        }

        private void OnDashChanged(bool isDashing)
        {
            // Only play when the dash STARTS (isDashing = true), not when it ends
            if (isDashing)
            {
                PlayClip(_dashClip, _dashVolume);
            }
        }

        private void OnGroundedChanged(bool grounded, float impactSpeed)
        {
            // Play land sound only when becoming grounded with enough speed
            if (grounded && impactSpeed >= _minLandSpeed)
            {
                PlayClip(_landClip, _landVolume);
            }
        }

        private void OnWallSlideChanged(bool isWallSliding)
        {
            if (isWallSliding)
            {
                PlayClip(_wallSlideClip, _wallSlideVolume);
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null) return;
            _source.PlayOneShot(clip, volume);
        }
    }
}