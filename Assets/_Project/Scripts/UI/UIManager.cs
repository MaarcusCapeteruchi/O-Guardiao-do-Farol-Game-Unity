using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// HUD, indicador de interação, avisos rápidos, resumo do dia e telas de vitória/derrota.
    /// Os botões são ligados por código no Awake (basta arrastar as referências).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text servedText;

        [Header("Item carregado")]
        [SerializeField] private Image carriedIcon;
        [SerializeField] private TMP_Text carriedLabel;

        [Header("Indicador de interação")]
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private TMP_Text interactionPromptText;

        [Header("Aviso rápido (toast)")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TMP_Text toastText;
        [Min(0.5f)] [SerializeField] private float toastDuration = 2.5f;

        [Header("Resumo do dia")]
        [SerializeField] private GameObject daySummaryPanel;
        [SerializeField] private TMP_Text summaryTitleText;
        [SerializeField] private TMP_Text summaryBodyText;
        [SerializeField] private Button nextDayButton;
        [SerializeField] private TMP_Text nextDayButtonLabel;

        [Header("Vitória")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TMP_Text victoryTitleText;
        [SerializeField] private TMP_Text victoryBodyText;
        [SerializeField] private Button victoryPlayAgainButton;
        [SerializeField] private Button victoryMenuButton;

        [Header("Derrota")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private TMP_Text defeatTitleText;
        [SerializeField] private TMP_Text defeatBodyText;
        [SerializeField] private Button defeatTryAgainButton;
        [SerializeField] private Button defeatMenuButton;

        private float toastTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            SetActive(daySummaryPanel, false);
            SetActive(victoryPanel, false);
            SetActive(defeatPanel, false);
            SetActive(toastRoot, false);
            SetActive(interactionPromptRoot, false);
            SetActive(hudRoot, true);

            if (nextDayButton != null) nextDayButton.onClick.AddListener(OnNextDayClicked);
            if (victoryPlayAgainButton != null) victoryPlayAgainButton.onClick.AddListener(OnRestartClicked);
            if (defeatTryAgainButton != null) defeatTryAgainButton.onClick.AddListener(OnRestartClicked);
            if (victoryMenuButton != null) victoryMenuButton.onClick.AddListener(OnMenuClicked);
            if (defeatMenuButton != null) defeatMenuButton.onClick.AddListener(OnMenuClicked);

            SetCarriedItem(ProductType.None);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (EconomyManager.Instance != null) EconomyManager.Instance.CoinsChanged -= UpdateCoins;
        }

        private void Start()
        {
            EconomyManager economy = EconomyManager.Instance;
            if (economy != null)
            {
                economy.CoinsChanged += UpdateCoins;
                UpdateCoins(economy.TotalCoins, economy.GoalCoins);
            }
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) SetActive(toastRoot, false);
            }
        }

        // ---------- Aviso rápido estático (seguro sem UIManager) ----------

        public static void Toast(string message)
        {
            if (Instance != null) Instance.ShowToast(message);
        }

        public void ShowToast(string message)
        {
            if (toastText == null || string.IsNullOrEmpty(message)) return;
            toastText.text = message;
            SetActive(toastRoot, true);
            toastTimer = toastDuration;
        }

        // ---------- HUD ----------

        public void UpdateDay(int day, int totalDays)
        {
            if (dayText != null) dayText.text = "Dia " + day + "/" + totalDays;
        }

        public void UpdateCoins(int total, int goal)
        {
            if (coinsText != null) coinsText.text = "Moedas: " + total + "/" + goal;
        }

        public void UpdateTimer(float secondsRemaining)
        {
            if (timerText == null) return;
            int seconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            timerText.text = "Expediente: " + (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        public void UpdateServed(int served, int totalToday)
        {
            if (servedText != null) servedText.text = "Atendidos: " + served + "/" + totalToday;
        }

        public void SetCarriedItem(ProductType product)
        {
            OrderManager orders = OrderManager.Instance;

            if (carriedIcon != null)
            {
                Sprite icon = orders != null && product != ProductType.None ? orders.GetIcon(product) : null;
                carriedIcon.sprite = icon;
                carriedIcon.color = orders != null ? orders.GetIconColor(product) : Color.white;
                carriedIcon.enabled = icon != null;
            }

            if (carriedLabel != null)
            {
                carriedLabel.text = product == ProductType.None
                    ? "Mãos vazias"
                    : "Carregando: " + (orders != null ? orders.GetName(product) : product.ToString());
            }
        }

        public void SetInteractionPrompt(string prompt)
        {
            bool visible = !string.IsNullOrEmpty(prompt);
            SetActive(interactionPromptRoot, visible);
            if (visible && interactionPromptText != null) interactionPromptText.text = prompt;
        }

        // ---------- Resumo do dia ----------

        public void ShowDaySummary(int day, int totalDays, int served, int lost, int coinsToday,
            int totalCoins, int goal, bool isLastDay)
        {
            SetActive(interactionPromptRoot, false);
            SetActive(toastRoot, false);

            if (summaryTitleText != null) summaryTitleText.text = "Dia " + day + "/" + totalDays + " concluído";
            if (summaryBodyText != null)
            {
                int missing = Mathf.Max(0, goal - totalCoins);
                string status = missing > 0
                    ? "Faltam <b>" + missing + "</b> moedas para pagar a dívida."
                    : "<color=#7CFC7C>A meta de " + goal + " moedas foi atingida!</color>";

                summaryBodyText.text =
                    "Clientes atendidos: <b>" + served + "</b>\n" +
                    "Clientes perdidos: <b>" + lost + "</b>\n" +
                    "Moedas recebidas hoje: <b>" + coinsToday + "</b>\n" +
                    "Total acumulado: <b>" + totalCoins + "/" + goal + "</b>\n\n" +
                    status;
            }

            if (nextDayButtonLabel != null) nextDayButtonLabel.text = isLastDay ? "Ver Resultado" : "Próximo Dia";
            SetActive(daySummaryPanel, true);
            if (nextDayButton != null) nextDayButton.Select();
        }

        public void HideDaySummary()
        {
            SetActive(daySummaryPanel, false);
        }

        // ---------- Finais ----------

        public void ShowEnding(bool victory, string title, string body, int coins, int goal)
        {
            SetActive(daySummaryPanel, false);
            SetActive(interactionPromptRoot, false);
            SetActive(toastRoot, false);
            SetActive(hudRoot, false);

            string full = body + "\n\n<b>Moedas conquistadas: " + coins + "/" + goal + "</b>";

            if (victory)
            {
                if (victoryTitleText != null) victoryTitleText.text = title;
                if (victoryBodyText != null) victoryBodyText.text = full;
                SetActive(victoryPanel, true);
                if (victoryPlayAgainButton != null) victoryPlayAgainButton.Select();
            }
            else
            {
                if (defeatTitleText != null) defeatTitleText.text = title;
                if (defeatBodyText != null) defeatBodyText.text = full;
                SetActive(defeatPanel, true);
                if (defeatTryAgainButton != null) defeatTryAgainButton.Select();
            }
        }

        // ---------- Botões ----------

        private void OnNextDayClicked()
        {
            if (DayManager.Instance != null) DayManager.Instance.OnNextDayPressed();
        }

        private void OnRestartClicked()
        {
            AudioManager.Play(SoundEffect.ButtonClick);
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
        }

        private void OnMenuClicked()
        {
            AudioManager.Play(SoundEffect.ButtonClick);
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value) target.SetActive(value);
        }
    }
}
