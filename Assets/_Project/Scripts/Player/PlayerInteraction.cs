using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Detecta o Interactable mais próximo dentro do alcance, destaca-o, mostra o indicador
    /// no HUD e executa a interação com E ou Espaço. Também guarda o item carregado (apenas um).
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Alcance")]
        [Tooltip("Centro da área de interação. Se vazio, usa a posição do jogador.")]
        [SerializeField] private Transform interactionOrigin;
        [Min(0.1f)] [SerializeField] private float interactionRadius = 1.1f;
        [Tooltip("Camadas consideradas na busca. Deixe 'Everything' se não usar camadas específicas.")]
        [SerializeField] private LayerMask interactableLayers = ~0;

        [Header("Item carregado")]
        [Tooltip("SpriteRenderer acima da cabeça do jogador que mostra o item carregado.")]
        [SerializeField] private SpriteRenderer carriedItemRenderer;

        public ProductType CarriedProduct { get; private set; } = ProductType.None;
        public Interactable CurrentTarget { get; private set; }

        public event Action<ProductType> CarriedChanged;

        private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);
        private ContactFilter2D contactFilter;
        private string lastPrompt;

        private void Awake()
        {
            contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(interactableLayers);
            contactFilter.useTriggers = true;
            RefreshCarriedVisual();
        }

        private void Start()
        {
            SetCarried(ProductType.None);
        }

        private void Update()
        {
            if (!GameManager.CanPlayerAct)
            {
                SetTarget(null);
                UpdatePrompt(null);
                return;
            }

            SetTarget(FindBestTarget());
            UpdatePrompt(CurrentTarget != null ? CurrentTarget.GetPrompt(this) : null);

            if (CurrentTarget != null && InteractPressed() && !GameManager.StateChangedThisFrame)
            {
                CurrentTarget.Interact(this);
            }
        }

        private void OnDisable()
        {
            SetTarget(null);
            UpdatePrompt(null);
        }

        public void SetCarried(ProductType product)
        {
            CarriedProduct = product;
            RefreshCarriedVisual();
            if (UIManager.Instance != null) UIManager.Instance.SetCarriedItem(product);
            CarriedChanged?.Invoke(product);
        }

        public void ClearCarried()
        {
            SetCarried(ProductType.None);
        }

        private Interactable FindBestTarget()
        {
            Vector2 origin = interactionOrigin != null ? (Vector2)interactionOrigin.position : (Vector2)transform.position;
            overlapResults.Clear();
            Physics2D.OverlapCircle(origin, interactionRadius, contactFilter, overlapResults);

            Interactable best = null;
            float bestDistance = float.MaxValue;

            foreach (Collider2D hit in overlapResults)
            {
                if (hit == null) continue;
                Interactable candidate = hit.GetComponentInParent<Interactable>();
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.CanInteract(this)) continue;

                float distance = Vector2.Distance(origin, hit.ClosestPoint(origin));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private void SetTarget(Interactable target)
        {
            if (CurrentTarget == target) return;
            if (CurrentTarget != null) CurrentTarget.SetHighlighted(false);
            CurrentTarget = target;
            if (CurrentTarget != null) CurrentTarget.SetHighlighted(true);
        }

        private void UpdatePrompt(string prompt)
        {
            if (prompt == lastPrompt) return;
            lastPrompt = prompt;
            if (UIManager.Instance != null) UIManager.Instance.SetInteractionPrompt(prompt);
        }

        private void RefreshCarriedVisual()
        {
            if (carriedItemRenderer == null) return;

            if (CarriedProduct == ProductType.None || OrderManager.Instance == null)
            {
                carriedItemRenderer.enabled = false;
                return;
            }

            carriedItemRenderer.sprite = OrderManager.Instance.GetIcon(CarriedProduct);
            carriedItemRenderer.color = OrderManager.Instance.GetIconColor(CarriedProduct);
            carriedItemRenderer.enabled = carriedItemRenderer.sprite != null;
        }

        private static bool InteractPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            return keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, interactionRadius);
        }
    }
}
