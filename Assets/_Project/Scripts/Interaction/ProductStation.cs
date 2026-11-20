using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Ponto de retirada de um produto (Barril = Cerveja, Fogão = Carne ou Sopa).
    /// - Mãos vazias: pega o produto.
    /// - Carregando outro produto: troca pelo produto desta estação.
    /// - Carregando o mesmo produto: devolve (fica de mãos vazias).
    /// </summary>
    public class ProductStation : Interactable
    {
        [SerializeField] private ProductType product = ProductType.Cerveja;
        [SerializeField] private string stationName = "Barril";

        [Tooltip("SpriteRenderer opcional que mostra o ícone do produto sobre a estação.")]
        [SerializeField] private SpriteRenderer productIconRenderer;

        public ProductType Product => product;

        private void Start()
        {
            OrderManager orders = OrderManager.Instance;
            if (productIconRenderer != null && orders != null)
            {
                Sprite icon = orders.GetIcon(product);
                if (icon != null)
                {
                    productIconRenderer.sprite = icon;
                    productIconRenderer.color = orders.GetIconColor(product);
                }
            }
        }

        public override bool CanInteract(PlayerInteraction player)
        {
            return player != null && product != ProductType.None;
        }

        public override string GetPrompt(PlayerInteraction player)
        {
            string productName = ProductName();
            if (player == null) return stationName;
            if (player.CarriedProduct == product) return "[E] Devolver " + productName;
            if (player.CarriedProduct == ProductType.None) return "[E] Pegar " + productName + " (" + stationName + ")";
            return "[E] Trocar por " + productName;
        }

        public override void Interact(PlayerInteraction player)
        {
            if (player == null) return;

            if (player.CarriedProduct == product)
            {
                player.ClearCarried();
                UIManager.Toast(ProductName() + " devolvida.");
                AudioManager.Play(SoundEffect.Pickup);
            }
            else
            {
                player.SetCarried(product);
            }

            AudioManager.Play(SoundEffect.Pickup);
        }

        private string ProductName()
        {
            return OrderManager.Instance != null ? OrderManager.Instance.GetName(product) : product.ToString();
        }
    }
}
