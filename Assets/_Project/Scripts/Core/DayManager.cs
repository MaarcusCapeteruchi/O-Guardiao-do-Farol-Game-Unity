using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Progressão dos 5 dias: introdução narrativa, expediente com tempo limitado,
    /// encerramento do dia, resumo e avanço manual para o próximo dia.
    ///
    /// Regra de encerramento: quando o tempo acaba, nenhum cliente novo entra (os que ainda
    /// não chegaram contam como perdidos) e o dia termina quando os clientes presentes forem
    /// atendidos ou desistirem. Se todos os clientes do dia forem resolvidos antes do tempo,
    /// o dia termina mais cedo.
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Tooltip("Asset da campanha. Se ficar vazio, a campanha padrão (embutida no código) é usada.")]
        [SerializeField] private CampaignData campaign;

        [SerializeField] private CustomerManager customerManager;
        [SerializeField] private DialogueManager dialogueManager;

        public CampaignData Campaign => campaign;
        public int CurrentDayIndex { get; private set; }
        public int CurrentDayNumber => CurrentDayIndex + 1;
        public int TotalDays => campaign != null ? campaign.DayCount : 0;
        public bool IsLastDay => CurrentDayIndex >= TotalDays - 1;
        public float TimeRemaining { get; private set; }
        public int ServedToday { get; private set; }
        public int LostToday { get; private set; }
        public int ServedTotal { get; private set; }
        public int LostTotal { get; private set; }

        private DayConfig currentDay;
        private bool shiftRunning;
        private bool shiftTimeOver;
        private bool dayFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (campaign == null || campaign.DayCount == 0)
            {
                campaign = CampaignData.CreateDefault();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (customerManager != null)
            {
                customerManager.CustomerServed += HandleCustomerServed;
                customerManager.CustomerLost += HandleCustomerLost;
            }
        }

        private void OnDisable()
        {
            if (customerManager != null)
            {
                customerManager.CustomerServed -= HandleCustomerServed;
                customerManager.CustomerLost -= HandleCustomerLost;
            }
        }

        public void StartCampaign()
        {
            ServedTotal = 0;
            LostTotal = 0;
            StartDay(0);
        }

        private void StartDay(int index)
        {
            CurrentDayIndex = Mathf.Clamp(index, 0, Mathf.Max(0, TotalDays - 1));
            currentDay = campaign.GetDay(CurrentDayIndex);
            if (currentDay == null)
            {
                Debug.LogError("DayManager: configuração do dia " + CurrentDayNumber + " não encontrada.");
                return;
            }

            ServedToday = 0;
            LostToday = 0;
            TimeRemaining = currentDay.shiftDuration;
            shiftRunning = false;
            shiftTimeOver = false;
            dayFinished = false;

            if (EconomyManager.Instance != null) EconomyManager.Instance.BeginNewDay();
            if (customerManager != null) customerManager.PrepareDay(currentDay);
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);

            UIManager ui = UIManager.Instance;
            if (ui != null)
            {
                ui.HideDaySummary();
                ui.UpdateDay(CurrentDayNumber, TotalDays);
                ui.UpdateTimer(TimeRemaining);
                ui.UpdateServed(ServedToday, currentDay.CustomerCount);
            }

            bool hasIntro = currentDay.introLines != null && currentDay.introLines.Length > 0;
            if (dialogueManager != null && hasIntro)
            {
                dialogueManager.Show(currentDay.narratorName, currentDay.introLines, BeginShift);
            }
            else
            {
                BeginShift();
            }
        }

        private void BeginShift()
        {
            if (dayFinished || currentDay == null) return;
            shiftRunning = true;
            if (customerManager != null) customerManager.BeginSpawning();
            UIManager.Toast(currentDay.title + ": a taverna está aberta!");
            AudioManager.Play(SoundEffect.ShiftBell);
        }

        private void Update()
        {
            if (!shiftRunning || dayFinished) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            if (!shiftTimeOver)
            {
                TimeRemaining -= Time.deltaTime;
                if (TimeRemaining <= 0f)
                {
                    TimeRemaining = 0f;
                    EndShiftTime();
                }

                if (UIManager.Instance != null) UIManager.Instance.UpdateTimer(TimeRemaining);
            }

            if (customerManager == null)
            {
                FinishDay();
                return;
            }

            bool noMoreArrivals = shiftTimeOver || customerManager.AllCustomersSpawned;
            if (noMoreArrivals && customerManager.ActiveCustomerCount == 0)
            {
                FinishDay();
            }
        }

        private void EndShiftTime()
        {
            shiftTimeOver = true;
            int notArrived = customerManager != null ? customerManager.StopSpawning() : 0;
            if (notArrived > 0)
            {
                LostToday += notArrived;
                LostTotal += notArrived;
            }

            UIManager.Toast("Fim do expediente! Atenda quem ainda está na taverna.");
            AudioManager.Play(SoundEffect.ShiftBell);
        }

        private void FinishDay()
        {
            if (dayFinished) return;
            dayFinished = true;
            shiftRunning = false;

            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.DaySummary);

            EconomyManager economy = EconomyManager.Instance;
            int coinsToday = economy != null ? economy.CoinsToday : 0;
            int total = economy != null ? economy.TotalCoins : 0;
            int goal = economy != null ? economy.GoalCoins : 100;

            if (UIManager.Instance != null)
            {
                AudioManager.Play(SoundEffect.DayEnd);
                UIManager.Instance.ShowDaySummary(CurrentDayNumber, TotalDays, ServedToday, LostToday,
                    coinsToday, total, goal, IsLastDay);
            }
        }

        /// <summary>Ligado ao botão "Próximo Dia" do resumo.</summary>
        public void OnNextDayPressed()
        {
            if (!dayFinished) return;
            AudioManager.Play(SoundEffect.ButtonClick);

            if (IsLastDay)
            {
                if (UIManager.Instance != null) UIManager.Instance.HideDaySummary();
                if (GameManager.Instance != null) GameManager.Instance.FinishCampaign();
            }
            else
            {
                StartDay(CurrentDayIndex + 1);
            }
        }

        private void HandleCustomerServed(CustomerController customer)
        {
            ServedToday++;
            ServedTotal++;
            if (UIManager.Instance != null && currentDay != null)
            {
                UIManager.Instance.UpdateServed(ServedToday, currentDay.CustomerCount);
            }
        }

        private void HandleCustomerLost(CustomerController customer)
        {
            LostToday++;
            LostTotal++;
        }
    }
}
