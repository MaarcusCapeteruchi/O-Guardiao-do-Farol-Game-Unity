using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Base para tudo que o jogador pode usar com E/Espaço (barril, fogão, mesas).
    /// O objeto precisa de pelo menos um Collider2D (pode ser Trigger) para ser detectado.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Objeto ligado enquanto este item é o alvo da interação (contorno, seta etc.). Opcional.")]
        [SerializeField] private GameObject highlight;

        protected virtual void Awake()
        {
            SetHighlighted(false);
        }

        public void SetHighlighted(bool value)
        {
            if (highlight != null && highlight.activeSelf != value) highlight.SetActive(value);
        }

        /// <summary>Se o jogador pode interagir agora.</summary>
        public abstract bool CanInteract(PlayerInteraction player);

        /// <summary>Texto exibido no indicador de interação do HUD.</summary>
        public abstract string GetPrompt(PlayerInteraction player);

        public abstract void Interact(PlayerInteraction player);
    }
}
