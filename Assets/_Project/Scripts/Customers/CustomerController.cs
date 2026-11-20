using System.Collections.Generic;
using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Comportamento individual de um cliente: entra pela porta, segue waypoints até a mesa,
    /// faz um pedido, espera (barra de paciência), recebe o pedido ou desiste, e vai embora.
    /// </summary>
    public class CustomerController : MonoBehaviour
    {
        public enum CustomerState
        {
            None,
            Entering,
            Waiting,
            Served,
            Leaving
        }

        [Header("Visual")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("Movimento")]
        [Min(0.1f)] [SerializeField] private float walkSpeed = 2.6f;
        [Min(0.001f)] [SerializeField] private float arriveDistance = 0.03f;

        [Header("Balão do pedido")]
        [SerializeField] private GameObject orderBubble;
        [SerializeField] private SpriteRenderer orderIconRenderer;

        [Header("Barra de paciência")]
        [SerializeField] private GameObject patienceBarRoot;
        [Tooltip("Transform escalado no eixo X (pivô na borda esquerda da barra).")]
        [SerializeField] private Transform patienceFillPivot;
        [SerializeField] private SpriteRenderer patienceFillRenderer;
        [SerializeField] private Color patienceFullColor = new Color(0.35f, 0.85f, 0.35f);
        [SerializeField] private Color patienceEmptyColor = new Color(0.9f, 0.2f, 0.2f);

        public CustomerState State { get; private set; } = CustomerState.None;
        public ProductType Order { get; private set; } = ProductType.None;
        public string HeroName { get; private set; } = "Cliente";
        public bool IsWaitingForOrder => State == CustomerState.Waiting;
        public float PatienceRatio => patienceMax > 0f ? Mathf.Clamp01(patienceLeft / patienceMax) : 0f;

        private CustomerProfile profile;
        private Table table;
        private CustomerManager manager;
        private List<Vector3> path = new List<Vector3>();
        private int pathIndex;
        private float patienceMax;
        private float patienceLeft;

        public void Initialize(CustomerProfile customerProfile, Table assignedTable, ProductType order,
            float patienceSeconds, Vector3 doorPosition, CustomerManager owner)
        {
            profile = customerProfile;
            table = assignedTable;
            manager = owner;
            Order = order;
            patienceMax = Mathf.Max(1f, patienceSeconds);
            patienceLeft = patienceMax;

            HeroName = profile != null && !string.IsNullOrEmpty(profile.heroName) ? profile.heroName : "Cliente";
            gameObject.name = "Cliente - " + HeroName;

            if (bodyRenderer != null && profile != null)
            {
                if (profile.sprite != null) bodyRenderer.sprite = profile.sprite;
                bodyRenderer.color = profile.tint;
            }

            if (characterAnimator != null && profile != null && profile.walkFrames != null && profile.walkFrames.Length > 0)
            {
                Sprite[] idle = profile.sprite != null ? new[] { profile.sprite } : null;
                characterAnimator.SetFrames(idle, profile.walkFrames);
            }

            SetOrderIcon();
            SetBubbleVisible(false);

            transform.position = doorPosition;
            path = table != null ? table.BuildPathFrom(doorPosition) : new List<Vector3> { doorPosition };
            pathIndex = 0;
            State = CustomerState.Entering;
            if (characterAnimator != null) characterAnimator.SetSitting(false);
        }

        private void Update()
        {
            switch (State)
            {
                case CustomerState.Entering:
                    if (FollowPath()) SitDown();
                    break;

                case CustomerState.Waiting:
                    patienceLeft -= Time.deltaTime;
                    UpdatePatienceBar();
                    if (patienceLeft <= 0f) GiveUp();
                    break;

                case CustomerState.Leaving:
                    if (FollowPath()) Exit();
                    break;
            }
        }

        /// <summary>Chamado pelo OrderManager quando o pedido correto é entregue.</summary>
        public void ReceiveOrder()
        {
            if (State != CustomerState.Waiting) return;
            State = CustomerState.Served;
            SetBubbleVisible(false);
            if (manager != null) manager.NotifyServed(this);

            DialogueManager dialogue = DialogueManager.Instance;
            string[] lines = profile != null ? profile.servedLines : null;
            if (dialogue != null && lines != null && lines.Length > 0)
            {
                dialogue.Show(HeroName, lines, StartLeaving);
            }
            else
            {
                StartLeaving();
            }
        }

        private void SitDown()
        {
            State = CustomerState.Waiting;
            patienceLeft = patienceMax;
            SetBubbleVisible(true);
            UpdatePatienceBar();
            if (characterAnimator != null)
            {
                characterAnimator.SetMovement(Vector2.down, 0f);
                characterAnimator.SetSitting(true);
            }
        }

        private void GiveUp()
        {
            if (State != CustomerState.Waiting) return;
            UIManager.Toast(HeroName + " cansou de esperar e foi embora sem pagar!");
            AudioManager.Play(SoundEffect.CustomerLeave);
            if (manager != null) manager.NotifyLost(this);
            StartLeaving();
        }

        private void StartLeaving()
        {
            if (State == CustomerState.Leaving || this == null) return;
            SetBubbleVisible(false);
            if (table != null) table.Release(this);

            Vector3 door = path.Count > 0 ? path[0] : transform.position;
            List<Vector3> exitPath = new List<Vector3>(path);
            exitPath.Reverse();
            if (exitPath.Count == 0 || exitPath[exitPath.Count - 1] != door) exitPath.Add(door);
            path = exitPath;
            pathIndex = 0;

            State = CustomerState.Leaving;
            if (characterAnimator != null) characterAnimator.SetSitting(false);
        }

        private void Exit()
        {
            State = CustomerState.None;
            if (manager != null) manager.NotifyExited(this);
            Destroy(gameObject);
        }

        /// <summary>Move ao longo do caminho. Retorna true ao chegar ao último ponto.</summary>
        private bool FollowPath()
        {
            if (pathIndex >= path.Count)
            {
                if (characterAnimator != null) characterAnimator.SetMovement(Vector2.down, 0f);
                return true;
            }

            Vector3 target = path[pathIndex];
            Vector3 current = transform.position;
            Vector3 next = Vector3.MoveTowards(current, target, walkSpeed * Time.deltaTime);
            transform.position = next;

            Vector2 direction = target - current;
            if (characterAnimator != null && direction.sqrMagnitude > 0.0001f)
            {
                characterAnimator.SetMovement(direction.normalized, walkSpeed);
            }

            if ((next - target).sqrMagnitude <= arriveDistance * arriveDistance)
            {
                transform.position = target;
                pathIndex++;
            }

            return pathIndex >= path.Count;
        }

        private void SetOrderIcon()
        {
            if (orderIconRenderer == null) return;
            OrderManager orders = OrderManager.Instance;
            if (orders == null) return;
            orderIconRenderer.sprite = orders.GetIcon(Order);
            orderIconRenderer.color = orders.GetIconColor(Order);
        }

        private void SetBubbleVisible(bool visible)
        {
            if (orderBubble != null) orderBubble.SetActive(visible);
            if (patienceBarRoot != null) patienceBarRoot.SetActive(visible);
        }

        private void UpdatePatienceBar()
        {
            float ratio = PatienceRatio;
            if (patienceFillPivot != null)
            {
                Vector3 scale = patienceFillPivot.localScale;
                scale.x = ratio;
                patienceFillPivot.localScale = scale;
            }

            if (patienceFillRenderer != null)
            {
                patienceFillRenderer.color = Color.Lerp(patienceEmptyColor, patienceFullColor, ratio);
            }
        }

        private void OnDestroy()
        {
            if (table != null) table.Release(this);
        }
    }
}
