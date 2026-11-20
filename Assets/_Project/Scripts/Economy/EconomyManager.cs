using System;
using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Guarda as moedas acumuladas na campanha e as recebidas no dia atual.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Tooltip("Meta de moedas para salvar a taverna.")]
        [Min(1)] [SerializeField] private int goalCoins = 100;

        [Tooltip("Moedas com que a campanha começa.")]
        [Min(0)] [SerializeField] private int startingCoins = 0;

        public int TotalCoins { get; private set; }
        public int CoinsToday { get; private set; }
        public int GoalCoins => goalCoins;
        public bool GoalReached => TotalCoins >= goalCoins;

        /// <summary>Disparado com (total, meta) sempre que as moedas mudam.</summary>
        public event Action<int, int> CoinsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            TotalCoins = startingCoins;
            CoinsToday = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            CoinsChanged?.Invoke(TotalCoins, goalCoins);
        }

        public void BeginNewDay()
        {
            CoinsToday = 0;
            CoinsChanged?.Invoke(TotalCoins, goalCoins);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            TotalCoins += amount;
            CoinsToday += amount;
            CoinsChanged?.Invoke(TotalCoins, goalCoins);
        }
    }
}
