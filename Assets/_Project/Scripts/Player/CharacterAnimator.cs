using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Ponte opcional entre movimento e Animator. Só altera parâmetros que existem no
    /// Animator Controller atribuído, então funciona com ou sem animações importadas.
    /// Parâmetros reconhecidos (todos opcionais): MoveX (float), MoveY (float), Speed (float), IsSitting (bool).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Espelha o sprite horizontalmente quando o personagem anda para a esquerda.")]
        [SerializeField] private bool flipWhenMovingLeft = true;

        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveYParameter = "MoveY";
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string sittingParameter = "IsSitting";

        [Header("Animação por quadros (opcional, sem Animator)")]
        [Tooltip("Quadros parado olhando para baixo. Se vazio, usa o primeiro quadro de Andar Baixo.")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkDownFrames;
        [SerializeField] private Sprite[] walkUpFrames;
        [SerializeField] private Sprite[] walkLeftFrames;
        [SerializeField] private Sprite[] walkRightFrames;
        [SerializeField] private float walkFramesPerSecond = 10f;
        [SerializeField] private float idleFramesPerSecond = 3f;

        private enum Facing { Down, Up, Left, Right }
        private Facing facing = Facing.Down;
        private bool moving;
        private float frameTimer;
        private int frameIndex;

        private bool HasFrames =>
            (walkDownFrames != null && walkDownFrames.Length > 0) || (idleFrames != null && idleFrames.Length > 0);

        private int moveXHash;
        private int moveYHash;
        private int speedHash;
        private int sittingHash;
        private bool hasMoveX;
        private bool hasMoveY;
        private bool hasSpeed;
        private bool hasSitting;

        private void Awake()
        {
            CacheParameters();
        }

        public void CacheParameters()
        {
            hasMoveX = hasMoveY = hasSpeed = hasSitting = false;
            if (animator == null || animator.runtimeAnimatorController == null) return;

            moveXHash = Animator.StringToHash(moveXParameter);
            moveYHash = Animator.StringToHash(moveYParameter);
            speedHash = Animator.StringToHash(speedParameter);
            sittingHash = Animator.StringToHash(sittingParameter);

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == moveXHash && parameter.type == AnimatorControllerParameterType.Float) hasMoveX = true;
                if (parameter.nameHash == moveYHash && parameter.type == AnimatorControllerParameterType.Float) hasMoveY = true;
                if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float) hasSpeed = true;
                if (parameter.nameHash == sittingHash && parameter.type == AnimatorControllerParameterType.Bool) hasSitting = true;
            }
        }

        private void Update()
        {
            if (!HasFrames || spriteRenderer == null) return;

            Sprite[] frames = CurrentFrames(out bool animate);
            if (frames == null || frames.Length == 0) return;

            if (animate)
            {
                float fps = moving ? walkFramesPerSecond : idleFramesPerSecond;
                frameTimer += Time.deltaTime;
                float frameTime = 1f / Mathf.Max(0.1f, fps);
                while (frameTimer >= frameTime)
                {
                    frameTimer -= frameTime;
                    frameIndex++;
                }
            }
            else
            {
                frameIndex = 0;
                frameTimer = 0f;
            }

            Sprite next = frames[frameIndex % frames.Length];
            if (next != null) spriteRenderer.sprite = next;
        }

        private Sprite[] CurrentFrames(out bool animate)
        {
            animate = true;
            Sprite[] walk = facing switch
            {
                Facing.Up => walkUpFrames,
                Facing.Left => walkLeftFrames,
                Facing.Right => walkRightFrames,
                _ => walkDownFrames
            };
            if (walk == null || walk.Length == 0) walk = walkDownFrames;

            if (moving) return walk;

            if (facing == Facing.Down && idleFrames != null && idleFrames.Length > 0) return idleFrames;
            // Parado olhando para os lados/cima: primeiro quadro da caminhada nessa direção.
            animate = false;
            return walk;
        }

        /// <summary>
        /// Define os quadros em tempo de execução (usado pelos clientes, que recebem a arte do herói ao entrar).
        /// Os mesmos quadros de caminhada valem para todas as direções; esquerda espelha o sprite.
        /// </summary>
        public void SetFrames(Sprite[] idle, Sprite[] walk)
        {
            idleFrames = idle;
            walkDownFrames = walk;
            walkUpFrames = null;
            walkLeftFrames = null;
            walkRightFrames = null;
            mirrorLeft = true;
            frameIndex = 0;
            frameTimer = 0f;
        }

        private bool mirrorLeft;

        public void SetMovement(Vector2 direction, float speed)
        {
            if (HasFrames)
            {
                bool wasMoving = moving;
                Facing previous = facing;
                moving = speed > 0.01f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                        facing = direction.x < 0f ? Facing.Left : Facing.Right;
                    else
                        facing = direction.y > 0f ? Facing.Up : Facing.Down;
                }
                if (wasMoving != moving || previous != facing)
                {
                    frameIndex = 0;
                    frameTimer = 0f;
                }
                if (spriteRenderer != null) spriteRenderer.flipX = mirrorLeft && facing == Facing.Left;
            }
            else if (flipWhenMovingLeft && spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }

            if (animator == null || !animator.isActiveAndEnabled) return;
            if (hasSpeed) animator.SetFloat(speedHash, speed);
            if (speed > 0.01f)
            {
                if (hasMoveX) animator.SetFloat(moveXHash, direction.x);
                if (hasMoveY) animator.SetFloat(moveYHash, direction.y);
            }
        }

        public void SetSitting(bool sitting)
        {
            if (animator == null || !animator.isActiveAndEnabled || !hasSitting) return;
            animator.SetBool(sittingHash, sitting);
        }
    }
}
