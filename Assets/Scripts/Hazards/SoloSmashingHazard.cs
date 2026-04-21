using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A single hazard that shoots out to a target point, hits, stuns, then returns.
    /// Same game feel as SmashingHazard but for solo spikes.
    /// 
    /// Setup:
    /// 1. Create a parent GameObject (SoloSmashingHazard)
    /// 2. Add this script to the parent
    /// 3. Assign the spike GameObject
    /// 4. Set the target point (where it travels to)
    /// 5. Spike should have a Collider2D (trigger) and Hazard script
    /// </summary>
    public class SoloSmashingHazard : MonoBehaviour
    {
        [Header("Spike Reference")]
        [Tooltip("The spike that will shoot out")]
        [SerializeField] private Transform _spike;
        
        [Tooltip("Where the spike travels to (the wall/endpoint)")]
        [SerializeField] private Transform _targetPoint;

        [Header("Timing")]
        [Tooltip("Time between shoot cycles")]
        public float cycleTime = 3f;
        
        [Tooltip("Delay before this hazard starts its first cycle (for sequencing multiple hazards)")]
        public float cycleOffset = 0f;
        
        [Tooltip("How long the anticipation shake lasts")]
        public float shakeTime = 0.4f;
        
        [Tooltip("How long spike stays stunned after hitting")]
        public float stunTime = 0.3f;

        [Header("Movement Speeds")]
        [Tooltip("Speed when shooting out")]
        public float shootSpeed = 15f;
        
        [Tooltip("Speed when returning to start position")]
        public float returnSpeed = 3f;

        [Header("Shake Settings")]
        [Tooltip("Intensity of the anticipation shake")]
        public float shakeIntensity = 0.1f;
        
        [Tooltip("How fast the shake vibrates")]
        public float shakeFrequency = 50f;

        [Header("Collision Offset")]
        [Tooltip("How far from target point the spike stops (half the spike width)")]
        public float collisionOffset = 0.5f;

        [Header("Platform (Optional)")]
        [Tooltip("Enable if player can stand on this spike while it moves")]
        [SerializeField] private bool _actAsPlatform = true;

        // State
        private enum State { Waiting, Shaking, Shooting, Stunned, Returning }
        private State _currentState = State.Waiting;
        
        // Positions
        private Vector3 _spikeStart;
        private Vector3 _spikeTarget;
        private Vector3 _targetPosition;
        
        // Timers
        private float _stateTimer;
        private float _shakeTimer;

        // Passenger (player standing on platform)
        private Transform _passenger;

        private void Start()
        {
            if (_spike == null)
            {
                Debug.LogError("SoloSmashingHazard: Please assign the spike!", this);
                enabled = false;
                return;
            }

            if (_targetPoint == null)
            {
                Debug.LogError("SoloSmashingHazard: Please assign the target point!", this);
                enabled = false;
                return;
            }

            // Setup platform collision detection
            if (_actAsPlatform)
            {
                // Add collision listener component to the spike
                var listener = _spike.gameObject.GetComponent<PlatformCollisionListener>();
                if (listener == null)
                {
                    listener = _spike.gameObject.AddComponent<PlatformCollisionListener>();
                }
                listener.Initialize(this);
            }

            // Store starting position
            _spikeStart = _spike.position;
            _targetPosition = _targetPoint.position;

            // Calculate actual target (offset so spike edge hits the wall)
            Vector3 dirToTarget = (_targetPosition - _spikeStart).normalized;
            _spikeTarget = _targetPosition - (dirToTarget * collisionOffset);

            // Apply cycle offset for sequencing
            _stateTimer = cycleTime + cycleOffset;
        }

        /// <summary>
        /// Called by PlatformCollisionListener when player lands on spike
        /// </summary>
        public void OnPassengerEnter(Transform passenger)
        {
            _passenger = passenger;
            _passenger.SetParent(_spike);
        }

        /// <summary>
        /// Called by PlatformCollisionListener when player leaves spike
        /// </summary>
        public void OnPassengerExit(Transform passenger)
        {
            if (_passenger == passenger)
            {
                _passenger.SetParent(null);
                _passenger = null;
            }
        }

        private void Update()
        {
            switch (_currentState)
            {
                case State.Waiting:
                    UpdateWaiting();
                    break;
                case State.Shaking:
                    UpdateShaking();
                    break;
                case State.Shooting:
                    UpdateShooting();
                    break;
                case State.Stunned:
                    UpdateStunned();
                    break;
                case State.Returning:
                    UpdateReturning();
                    break;
            }
        }

        private void UpdateWaiting()
        {
            _stateTimer -= Time.deltaTime;
            
            if (_stateTimer <= 0f)
            {
                // Start shaking (anticipation)
                _currentState = State.Shaking;
                _stateTimer = shakeTime;
                _shakeTimer = 0f;
            }
        }

        private void UpdateShaking()
        {
            _stateTimer -= Time.deltaTime;
            _shakeTimer += Time.deltaTime;

            // Apply shake to spike
            Vector3 shakeOffset = GetShakeOffset();
            _spike.position = _spikeStart + shakeOffset;

            if (_stateTimer <= 0f)
            {
                // Reset to exact start position before shooting
                _spike.position = _spikeStart;
                
                // Start shooting
                _currentState = State.Shooting;
            }
        }

        private void UpdateShooting()
        {
            // Move spike towards target
            _spike.position = Vector3.MoveTowards(_spike.position, _spikeTarget, shootSpeed * Time.deltaTime);

            // Check if reached target
            float dist = Vector3.Distance(_spike.position, _spikeTarget);

            if (dist < 0.05f)
            {
                // Hit! Enter stun state
                _spike.position = _spikeTarget;
                _currentState = State.Stunned;
                _stateTimer = stunTime;
                
                // Trigger effects
                OnHit();
            }
        }

        private void UpdateStunned()
        {
            _stateTimer -= Time.deltaTime;

            // Subtle vibration while stunned
            Vector3 dirToTarget = (_targetPosition - _spikeStart).normalized;
            float stunShake = Mathf.Sin(Time.time * 30f) * 0.02f;
            Vector3 stunOffset = dirToTarget * stunShake;
            
            _spike.position = _spikeTarget + stunOffset;

            if (_stateTimer <= 0f)
            {
                // Start returning
                _currentState = State.Returning;
            }
        }

        private void UpdateReturning()
        {
            // Move spike back to start position
            _spike.position = Vector3.MoveTowards(_spike.position, _spikeStart, returnSpeed * Time.deltaTime);

            // Check if returned
            float dist = Vector3.Distance(_spike.position, _spikeStart);

            if (dist < 0.01f)
            {
                // Snap to exact position
                _spike.position = _spikeStart;
                
                // Reset cycle
                _currentState = State.Waiting;
                _stateTimer = cycleTime;
            }
        }

        private Vector3 GetShakeOffset()
        {
            // Shake that increases in intensity as we approach shoot
            float progress = 1f - (_stateTimer / shakeTime);
            float intensity = shakeIntensity * progress;
            
            // High frequency shake
            float shakeX = Mathf.Sin(_shakeTimer * shakeFrequency) * intensity;
            float shakeY = Mathf.Cos(_shakeTimer * shakeFrequency * 1.3f) * intensity * 0.5f;
            
            return new Vector3(shakeX, shakeY, 0f);
        }

        /// <summary>
        /// Called when spike hits the target - override for effects
        /// </summary>
        protected virtual void OnHit()
        {
            // Add camera shake, particles, or sound here
            // Example: CameraShake.Shake(0.1f, 0.2f);
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            if (_spike != null && _targetPoint != null)
            {
                Vector3 start = Application.isPlaying ? _spikeStart : _spike.position;
                Vector3 target = _targetPoint.position;
                
                // Draw target point (wall)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target, 0.15f);
                
                // Draw where spike will stop
                Vector3 dir = (target - start).normalized;
                Vector3 stopPoint = target - (dir * collisionOffset);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(stopPoint, Vector3.one * 0.2f);
                
                // Draw path
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, stopPoint);
                
                // Draw spike start
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(start, Vector3.one * 0.2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_spike != null && _targetPoint != null)
            {
                // Draw arrow showing direction
                Vector3 start = _spike.position;
                Vector3 target = _targetPoint.position;
                Vector3 dir = (target - start).normalized;
                
                Gizmos.color = Color.red;
                Vector3 arrowHead = start + dir * 0.5f;
                Gizmos.DrawLine(start, arrowHead);
            }
        }
    }

    /// <summary>
    /// Helper component that listens for collisions on the spike and reports to SoloSmashingHazard.
    /// Automatically added by SoloSmashingHazard - do not add manually.
    /// </summary>
    public class PlatformCollisionListener : MonoBehaviour
    {
        private SoloSmashingHazard _hazard;
        private Transform _currentPassenger;

        public void Initialize(SoloSmashingHazard hazard)
        {
            _hazard = hazard;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_hazard == null) return;
            
            // Only detect player
            if (collision.collider.GetComponent<UltimatePlayerController>() == null) return;

            // Check if passenger is on top
            if (IsOnTop(collision))
            {
                _currentPassenger = collision.transform;
                _hazard.OnPassengerEnter(collision.transform);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_hazard == null) return;
            if (collision.collider.GetComponent<UltimatePlayerController>() == null) return;
            
            // Keep checking if player is on top (in case they land while platform is moving)
            if (_currentPassenger == null && IsOnTop(collision))
            {
                _currentPassenger = collision.transform;
                _hazard.OnPassengerEnter(collision.transform);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (_hazard == null) return;
            
            // Only detect player
            if (collision.collider.GetComponent<UltimatePlayerController>() == null) return;

            if (_currentPassenger == collision.transform)
            {
                _currentPassenger = null;
                _hazard.OnPassengerExit(collision.transform);
            }
        }

        private bool IsOnTop(Collision2D collision)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // Normal pointing up means passenger is on top
                if (contact.normal.y < -0.5f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}