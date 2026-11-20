using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardiaoDoFarol
{
    [Serializable]
    public class ProductInfo
    {
        public ProductType type = ProductType.Cerveja;
        public string displayName = "Cerveja";

        [Tooltip("Ícone do produto (balão do pedido, item carregado e HUD).")]
        public Sprite icon;

        [Tooltip("Cor aplicada ao ícone. Use branco quando estiver usando ícones próprios.")]
        public Color iconColor = Color.white;

        [Tooltip("Moedas pagas pelo cliente quando este produto é entregue corretamente.")]
        [Min(0)] public int price = 5;
    }

    /// <summary>
    /// Catálogo de produtos (nomes, ícones, preços), sorteio de pedidos e validação de entregas.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [SerializeField] private List<ProductInfo> products = new List<ProductInfo>
        {
            new ProductInfo { type = ProductType.Cerveja, displayName = "Cerveja", price = 5 },
            new ProductInfo { type = ProductType.Carne, displayName = "Carne", price = 8 },
            new ProductInfo { type = ProductType.Sopa, displayName = "Sopa", price = 6 }
        };

        public IReadOnlyList<ProductInfo> Products => products;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public ProductInfo GetInfo(ProductType type)
        {
            if (type == ProductType.None || products == null) return null;
            foreach (ProductInfo info in products)
            {
                if (info != null && info.type == type) return info;
            }
            return null;
        }

        public string GetName(ProductType type)
        {
            ProductInfo info = GetInfo(type);
            if (info != null && !string.IsNullOrEmpty(info.displayName)) return info.displayName;
            return type == ProductType.None ? "Nada" : type.ToString();
        }

        public int GetPrice(ProductType type)
        {
            ProductInfo info = GetInfo(type);
            return info != null ? info.price : 0;
        }

        public Sprite GetIcon(ProductType type)
        {
            ProductInfo info = GetInfo(type);
            return info != null ? info.icon : null;
        }

        public Color GetIconColor(ProductType type)
        {
            ProductInfo info = GetInfo(type);
            return info != null ? info.iconColor : Color.white;
        }

        /// <summary>Sorteia um dos produtos cadastrados.</summary>
        public ProductType GetRandomOrder()
        {
            if (products == null || products.Count == 0) return ProductType.Cerveja;

            List<ProductType> valid = new List<ProductType>(products.Count);
            foreach (ProductInfo info in products)
            {
                if (info != null && info.type != ProductType.None) valid.Add(info.type);
            }

            if (valid.Count == 0) return ProductType.Cerveja;
            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        /// <summary>
        /// Tenta entregar o item carregado pelo jogador ao cliente.
        /// Retorna true se o pedido estava correto e foi pago.
        /// </summary>
        public bool TryDeliver(CustomerController customer, PlayerInteraction player)
        {
            if (customer == null || player == null || !customer.IsWaitingForOrder) return false;

            ProductType carried = player.CarriedProduct;
            if (carried == ProductType.None)
            {
                UIManager.Toast(customer.HeroName + " quer " + GetName(customer.Order) + ".");
                return false;
            }

            if (carried != customer.Order)
            {
                UIManager.Toast("Pedido errado! " + customer.HeroName + " pediu " + GetName(customer.Order) + ".");
                AudioManager.Play(SoundEffect.WrongItem);
                return false;
            }

            int price = GetPrice(carried);
            player.ClearCarried();

            if (EconomyManager.Instance != null) EconomyManager.Instance.AddCoins(price);

            AudioManager.Play(SoundEffect.Deliver);
            AudioManager.Play(SoundEffect.Coin);
            UIManager.Toast("+" + price + " moedas!");

            customer.ReceiveOrder();
            return true;
        }
    }
}
