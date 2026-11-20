using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Movimento top-down do taverneiro com Rigidbody2D (WASD / setas).
    /// Requer "Active Input Handling" = Input System Package (ou Both).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [Min(0.1f)] [SerializeField] private float moveSpeed = 4.5f;
        [Tooltip("Aceleração/desaceleração em unidades por segundo². Valores altos = resposta mais seca.")]
        [Min(1f)] [SerializeField] private float acceleration = 40f;

        [Header("Visual (opcional)")]
        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("Passos")]
        [Min(0.05f)] [SerializeField] private float footstepInterval = 0.32f;

        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private float footstepTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            moveInput = GameManager.CanPlayerAct ? ReadMoveInput() : Vector2.zero;

            if (IsMoving)
            {
                FacingDirection = moveInput.normalized;
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    footstepTimer = footstepInterval;
                    AudioManager.Play(SoundEffect.Footstep);
                }
            }
            else
            {
                footstepTimer = 0f;
            }

            if (characterAnimator != null)
            {
                characterAnimator.SetMovement(FacingDirection, IsMoving ? moveSpeed : 0f);
            }
        }

        private void FixedUpdate()
        {
            Vector2 target = moveInput * moveSpeed;
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            Vector2 input = new Vector2(x, y);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }
    }
}
