using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Caixa de diálogo com nome do personagem e falas em sequência (efeito de máquina de escrever).
    /// Enquanto aberta, o jogo fica pausado (GameState.Dialogue).
    /// E / Espaço / Enter: completa o texto ou avança. Esc: fecha o diálogo inteiro.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text continueHintText;

        [Tooltip("Caracteres por segundo. 0 = mostra o texto inteiro de uma vez.")]
        [Min(0f)] [SerializeField] private float charactersPerSecond = 45f;

        [SerializeField] private string continueHint = "[E] continuar    [Esc] fechar";
        [SerializeField] private string closeHint = "[E] fechar";

        public bool IsOpen { get; private set; }

        private class DialogueRequest
        {
            public string speaker;
            public string[] lines;
            public Action onComplete;
        }

        private readonly Queue<DialogueRequest> queue = new Queue<DialogueRequest>();
        private DialogueRequest current;
        private int lineIndex;
        private float visibleCharacters;
        private int totalCharacters;
        private int openedFrame = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Abre (ou enfileira) um diálogo. onComplete é chamado ao fechar.</summary>
        public void Show(string speaker, IList<string> lines, Action onComplete)
        {
            if (lines == null || lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            string[] copy = new string[lines.Count];
            lines.CopyTo(copy, 0);
            queue.Enqueue(new DialogueRequest { speaker = speaker, lines = copy, onComplete = onComplete });

            if (!IsOpen) OpenNext();
        }

        private void OpenNext()
        {
            if (queue.Count == 0) return;

            current = queue.Dequeue();
            lineIndex = 0;
            IsOpen = true;
            openedFrame = Time.frameCount;

            if (GameManager.Instance != null) GameManager.Instance.EnterDialogue();
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerNameText != null) speakerNameText.text = current.speaker;

            AudioManager.Play(SoundEffect.DialogueOpen);
            DisplayCurrentLine();
        }

        private void Update()
        {
            if (!IsOpen || current == null) return;

            if (bodyText != null && visibleCharacters < totalCharacters)
            {
                if (charactersPerSecond <= 0f) visibleCharacters = totalCharacters;
                else visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(totalCharacters, Mathf.FloorToInt(visibleCharacters));
            }

            if (Time.frameCount == openedFrame) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            bool advance = keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                           keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (!advance) return;

            if (visibleCharacters < totalCharacters)
            {
                visibleCharacters = totalCharacters;
                if (bodyText != null) bodyText.maxVisibleCharacters = totalCharacters;
            }
            else
            {
                Next();
            }
        }

        /// <summary>Avança para a próxima fala (também pode ser ligado a um botão).</summary>
        public void Next()
        {
            if (!IsOpen || current == null) return;

            lineIndex++;
            if (lineIndex >= current.lines.Length) Close();
            else DisplayCurrentLine();
        }

        /// <summary>Fecha o diálogo atual (pode ser ligado a um botão).</summary>
        public void Close()
        {
            if (!IsOpen) return;

            DialogueRequest finished = current;
            current = null;
            IsOpen = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.ExitDialogue();

            finished?.onComplete?.Invoke();

            if (queue.Count > 0) OpenNext();
        }

        private void DisplayCurrentLine()
        {
            string line = current.lines[lineIndex] ?? string.Empty;
            if (bodyText != null)
            {
                bodyText.text = line;
                bodyText.ForceMeshUpdate();
                totalCharacters = bodyText.textInfo.characterCount;
                visibleCharacters = charactersPerSecond <= 0f ? totalCharacters : 0f;
                bodyText.maxVisibleCharacters = Mathf.FloorToInt(visibleCharacters);
            }
            else
            {
                totalCharacters = 0;
                visibleCharacters = 0f;
            }

            if (continueHintText != null)
            {
                bool last = lineIndex >= current.lines.Length - 1 && queue.Count == 0;
                continueHintText.text = last ? closeHint : continueHint;
            }
        }
    }
}
