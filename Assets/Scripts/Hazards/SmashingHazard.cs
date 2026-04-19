using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Two hazards that smash together on a timer, then return to their positions.
    /// Features anticipation shake and post-collision stun for great game feel.
    /// 
    /// Setup:
    /// 1. Create a parent GameObject (SmashingHazard)
    /// 2. Add this script to the parent
    /// 3. Assign the two spike GameObjects (left and right)
    /// 4. Each spike should have a Collider2D (trigger) and Hazard script
    /// </summary>
    public class SmashingHazard : MonoBehaviour
    {
        [Header("Spike References")]
        [Tooltip("The left/top spike")]
        [SerializeField] private Transform _spikeA;
        
        [Tooltip("The right/bottom spike")]
        [SerializeField] private Transform _spikeB;

        [Header("Timing")]
        [Tooltip("Time between smash cycles")]
        public float cycleTime = 3f;
        
        [Tooltip("Delay before this hazard starts its first cycle (for sequencing multiple hazards)")]
        public float cycleOffset = 0f;
        
        [Tooltip("How long the anticipation shake lasts")]
        public float shakeTime = 0.4f;
        
        [Tooltip("How long spikes stay stunned after collision")]
        public float stunTime = 0.3f;

        [Header("Movement Speeds")]
        [Tooltip("Speed when smashing together")]
        public float smashSpeed = 15f;
        
        [Tooltip("Speed when returning to start position")]
        public float returnSpeed = 3f;

        [Header("Shake Settings")]
        [Tooltip("Intensity of the anticipation shake")]
        public float shakeIntensity = 0.1f;
        
        [Tooltip("How fast the shake vibrates")]
        public float shakeFrequency = 50f;

        [Header("Collision Point")]
        [Tooltip("Where the spikes meet (leave empty to auto-calculate centre)")]
        [SerializeField] private Transform _collisionPoint;
        
        [Tooltip("How far from centre each spike stops (half the spike width)")]
        public float collisionOffset = 0.5f;

        // State
        private enum State { Waiting, Shaking, Smashing, Stunned, Returning }
        private State _currentState = State.Waiting;
        
        // Positions
        private Vector3 _spikeAStart;
        private Vector3 _spikeBStart;
        private Vector3 _collisionCentre;
        private Vector3 _spikeATarget;
        private Vector3 _spikeBTarget;
        
        // Timers
        private float _stateTimer;
        private float _shakeTimer;

        private void Start()
        {
            if (_spikeA == null || _spikeB == null)
            {
                Debug.LogError("SmashingHazard: Please assign both spikes!", this);
                enabled = false;
                return;
            }

            // Store starting positions
            _spikeAStart = _spikeA.position;
            _spikeBStart = _spikeB.position;

            // Calculate collision centre (midpoint between spikes)
            if (_collisionPoint != null)
            {
                _collisionCentre = _collisionPoint.position;
            }
            else
            {
                _collisionCentre = (_spikeAStart + _spikeBStart) / 2f;
            }

            // Calculate actual target positions (offset from centre so edges meet)
            Vector3 dirAToB = (_spikeBStart - _spikeAStart).normalized;
            _spikeATarget = _collisionCentre - (dirAToB * collisionOffset);
            _spikeBTarget = _collisionCentre + (dirAToB * collisionOffset);

            // Apply cycle offset for sequencing multiple hazards
            _stateTimer = cycleTime + cycleOffset;
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
                case State.Smashing:
                    UpdateSmashing();
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

            // Apply shake to both spikes
            Vector3 shakeOffset = GetShakeOffset();
            _spikeA.position = _spikeAStart + shakeOffset;
            _spikeB.position = _spikeBStart + shakeOffset;

            if (_stateTimer <= 0f)
            {
                // Reset to exact start positions before smashing
                _spikeA.position = _spikeAStart;
                _spikeB.position = _spikeBStart;
                
                // Start smashing
                _currentState = State.Smashing;
            }
        }

        private void UpdateSmashing()
        {
            // Move spikes towards their individual target points
            _spikeA.position = Vector3.MoveTowards(_spikeA.position, _spikeATarget, smashSpeed * Time.deltaTime);
            _spikeB.position = Vector3.MoveTowards(_spikeB.position, _spikeBTarget, smashSpeed * Time.deltaTime);

            // Check if both reached their targets
            float distA = Vector3.Distance(_spikeA.position, _spikeATarget);
            float distB = Vector3.Distance(_spikeB.position, _spikeBTarget);

            if (distA < 0.05f && distB < 0.05f)
            {
                // Collision! Enter stun state
                _currentState = State.Stunned;
                _stateTimer = stunTime;
                
                // Optional: Trigger camera shake or sound here
                OnCollision();
            }
        }

        private void UpdateStunned()
        {
            _stateTimer -= Time.deltaTime;

            // Subtle vibration while stunned
            float stunShake = Mathf.Sin(Time.time * 30f) * 0.02f;
            Vector3 stunOffset = new Vector3(stunShake, stunShake, 0f);
            
            _spikeA.position = _spikeATarget + stunOffset;
            _spikeB.position = _spikeBTarget - stunOffset;

            if (_stateTimer <= 0f)
            {
                // Start returning
                _currentState = State.Returning;
            }
        }

        private void UpdateReturning()
        {
            // Move spikes back to start positions
            _spikeA.position = Vector3.MoveTowards(_spikeA.position, _spikeAStart, returnSpeed * Time.deltaTime);
            _spikeB.position = Vector3.MoveTowards(_spikeB.position, _spikeBStart, returnSpeed * Time.deltaTime);

            // Check if both returned
            float distA = Vector3.Distance(_spikeA.position, _spikeAStart);
            float distB = Vector3.Distance(_spikeB.position, _spikeBStart);

            if (distA < 0.01f && distB < 0.01f)
            {
                // Snap to exact positions
                _spikeA.position = _spikeAStart;
                _spikeB.position = _spikeBStart;
                
                // Reset cycle
                _currentState = State.Waiting;
                _stateTimer = cycleTime;
            }
        }

        private Vector3 GetShakeOffset()
        {
            // Shake that increases in intensity as we approach smash
            float progress = 1f - (_stateTimer / shakeTime);
            float intensity = shakeIntensity * progress;
            
            // High frequency shake
            float shakeX = Mathf.Sin(_shakeTimer * shakeFrequency) * intensity;
            float shakeY = Mathf.Cos(_shakeTimer * shakeFrequency * 1.3f) * intensity * 0.5f;
            
            return new Vector3(shakeX, shakeY, 0f);
        }

        /// <summary>
        /// Called when spikes collide - override for effects
        /// </summary>
        protected virtual void OnCollision()
        {
            // Add camera shake, particles, or sound here
            // Example: CameraShake.Shake(0.1f, 0.2f);
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            if (_spikeA != null && _spikeB != null)
            {
                // Draw collision centre
                Vector3 centre = _collisionPoint != null 
                    ? _collisionPoint.position 
                    : (_spikeA.position + _spikeB.position) / 2f;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(centre, 0.1f);
                
                // Draw target points (where spikes will stop)
                Vector3 dir = (_spikeB.position - _spikeA.position).normalized;
                Vector3 targetA = centre - (dir * collisionOffset);
                Vector3 targetB = centre + (dir * collisionOffset);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(targetA, Vector3.one * 0.2f);
                Gizmos.DrawWireCube(targetB, Vector3.one * 0.2f);
                
                // Draw lines from spikes to targets
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_spikeA.position, targetA);
                Gizmos.DrawLine(_spikeB.position, targetB);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_spikeA != null && _spikeB != null)
            {
                // Show spike start positions
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_spikeA.position, Vector3.one * 0.3f);
                Gizmos.DrawWireCube(_spikeB.position, Vector3.one * 0.3f);
            }
        }
    }
}