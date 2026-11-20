using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Tela de início: botões Jogar, Créditos e Sair, e painel de créditos.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Nome da cena do jogo (precisa estar no Build Profile / Build Settings).")]
        [SerializeField] private string gameSceneName = "Game";

        [Header("Botões do menu")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Créditos")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button closeCreditsButton;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (closeCreditsButton != null) closeCreditsButton.onClick.AddListener(CloseCredits);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        private void Start()
        {
            AudioManager.PlayMusic(MusicTrack.Menu);
            if (playButton != null) playButton.Select();
        }

        private void Update()
        {
            if (creditsPanel == null || !creditsPanel.activeSelf) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CloseCredits();
        }

        public void Play()
        {
            AudioManager.Play(SoundEffect.ButtonClick);
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenCredits()
        {
            AudioManager.Play(SoundEffect.ButtonClick);
            if (creditsPanel != null) creditsPanel.SetActive(true);
            if (closeCreditsButton != null) closeCreditsButton.Select();
        }

        public void CloseCredits()
        {
            AudioManager.Play(SoundEffect.ButtonClick);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (creditsButton != null) creditsButton.Select();
        }

        public void Quit()
        {
            AudioManager.Play(SoundEffect.ButtonClick);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
