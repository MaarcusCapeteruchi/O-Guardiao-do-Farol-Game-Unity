using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuardiaoDoFarol
{
    public enum GameState
    {
        Playing,
        Dialogue,
        DaySummary,
        Ended
    }

    /// <summary>
    /// Estado geral da partida, pausa (Time.timeScale), finais e troca de cenas.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Cenas (precisam estar no Build Profile / Build Settings)")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameSceneName = "Game";

        [Header("Referências")]
        [SerializeField] private DayManager dayManager;

        public GameState State { get; private set; } = GameState.Playing;

        /// <summary>Frame em que o estado mudou pela última vez (evita que a mesma tecla seja lida duas vezes).</summary>
        public int StateChangedFrame { get; private set; } = -1;

        public event Action<GameState> StateChanged;

        /// <summary>True quando o jogador pode andar e interagir.</summary>
        public static bool CanPlayerAct => Instance == null || Instance.State == GameState.Playing;

        /// <summary>True no frame exato em que o estado mudou.</summary>
        public static bool StateChangedThisFrame => Instance != null && Instance.StateChangedFrame == Time.frameCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        private void Start()
        {
            AudioManager.PlayMusic(MusicTrack.Tavern);

            if (dayManager != null)
            {
                dayManager.StartCampaign();
            }
            else
            {
                Debug.LogError("GameManager: o campo 'Day Manager' não foi atribuído no Inspector.");
            }
        }

        public void SetState(GameState newState)
        {
            State = newState;
            StateChangedFrame = Time.frameCount;
            Time.timeScale = newState == GameState.Playing ? 1f : 0f;
            StateChanged?.Invoke(newState);
        }

        public void EnterDialogue()
        {
            if (State == GameState.Playing) SetState(GameState.Dialogue);
        }

        public void ExitDialogue()
        {
            if (State == GameState.Dialogue) SetState(GameState.Playing);
        }

        /// <summary>Chamado após o resumo do último dia. Decide vitória ou derrota.</summary>
        public void FinishCampaign()
        {
            if (State == GameState.Ended) return;
            SetState(GameState.Ended);

            EconomyManager economy = EconomyManager.Instance;
            int coins = economy != null ? economy.TotalCoins : 0;
            int goal = economy != null ? economy.GoalCoins : 100;
            bool victory = coins >= goal;

            CampaignData campaign = dayManager != null ? dayManager.Campaign : null;
            string title;
            string text;

            if (victory)
            {
                title = campaign != null ? campaign.victoryTitle : "A TAVERNA FOI SALVA!";
                text = campaign != null ? campaign.victoryText : "Você juntou {moedas} moedas e salvou a taverna!";
            }
            else
            {
                title = campaign != null ? campaign.defeatTitle : "A TAVERNA FOI FECHADA";
                text = campaign != null ? campaign.defeatText : "Você juntou apenas {moedas} moedas. A taverna foi fechada.";
            }

            text = (text ?? string.Empty).Replace("{moedas}", coins.ToString());

            AudioManager.PlayMusic(MusicTrack.None);
            AudioManager.Play(victory ? SoundEffect.Victory : SoundEffect.Defeat);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEnding(victory, title, text, coins, goal);
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
