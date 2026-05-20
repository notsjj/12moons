using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class LetterService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ConfigManager configManager;
        [SerializeField] private RuntimeDataService runtimeDataService;

        private readonly List<LetterDefinition> letters = new List<LetterDefinition>();
        private readonly Dictionary<string, LetterDefinition> lettersById =
            new Dictionary<string, LetterDefinition>(StringComparer.Ordinal);

        public event Action LettersChanged;

        public IReadOnlyList<LetterDefinition> Letters => letters;

        public RuntimeLetterState SelectedLetterState { get; private set; }

        public LetterDefinition SelectedLetter { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
            LoadLetterConfig();
        }

        private void OnEnable()
        {
            if (runtimeDataService != null)
            {
                runtimeDataService.LetterReceived += OnRuntimeLetterReceived;
                runtimeDataService.LetterRemoved += OnRuntimeLetterRemoved;
            }
        }

        private void OnDisable()
        {
            if (runtimeDataService != null)
            {
                runtimeDataService.LetterReceived -= OnRuntimeLetterReceived;
                runtimeDataService.LetterRemoved -= OnRuntimeLetterRemoved;
            }
        }

        public void Refresh()
        {
            LoadLetterConfig();
            NotifyLettersChanged();
        }

        public IReadOnlyList<RuntimeLetterState> GetReceivedLetters()
        {
            if (runtimeDataService == null)
            {
                return Array.Empty<RuntimeLetterState>();
            }

            return runtimeDataService.Data.Letters
                .OrderByDescending(letter => letter.ReceivedRound)
                .ThenBy(letter => letter.LetterId, StringComparer.Ordinal)
                .ToList();
        }

        public bool TryGetLetter(string letterId, out LetterDefinition letter)
        {
            return lettersById.TryGetValue(letterId, out letter);
        }

        public RuntimeLetterState ReceiveLetter(string letterId)
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("LetterService missing RuntimeDataService.", this);
                return null;
            }

            if (!TryGetLetter(letterId, out _))
            {
                Debug.LogWarning($"LetterId {letterId} is not configured in LetterConfig.", this);
                return null;
            }

            var state = runtimeDataService.ReceiveLetter(letterId);
            return state;
        }

        public bool SelectLetter(string letterId)
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("LetterService missing RuntimeDataService.", this);
                return false;
            }

            var state = runtimeDataService.Data.Letters.FirstOrDefault(letter => letter.LetterId == letterId);
            if (state == null)
            {
                Debug.LogWarning($"LetterId {letterId} has not been received.", this);
                return false;
            }

            if (!TryGetLetter(letterId, out var letter))
            {
                Debug.LogWarning($"LetterId {letterId} is not configured in LetterConfig.", this);
                return false;
            }

            state.MarkRead();
            SelectedLetterState = state;
            SelectedLetter = letter;
            NotifyLettersChanged();
            return true;
        }

        public void ClearSelection()
        {
            SelectedLetterState = null;
            SelectedLetter = null;
            NotifyLettersChanged();
        }

        public bool RemoveSelectedLetter()
        {
            var selectedLetterId = SelectedLetterState?.LetterId;
            if (string.IsNullOrEmpty(selectedLetterId) || runtimeDataService == null)
            {
                ClearSelection();
                return false;
            }

            SelectedLetterState = null;
            SelectedLetter = null;
            var removed = runtimeDataService.RemoveLetter(selectedLetterId);
            if (!removed)
            {
                NotifyLettersChanged();
            }

            return removed;
        }

        private void ResolveDependencies()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }
        }

        private void LoadLetterConfig()
        {
            letters.Clear();
            lettersById.Clear();

            if (configManager == null)
            {
                Debug.LogWarning("LetterService missing ConfigManager.", this);
                return;
            }

            if (!configManager.TryGetTable("LetterConfig", out var table))
            {
                Debug.LogWarning("LetterService cannot load LetterConfig.", this);
                return;
            }

            foreach (var row in table.Rows)
            {
                var letter = new LetterDefinition(row);
                if (string.IsNullOrEmpty(letter.LetterId))
                {
                    continue;
                }

                letters.Add(letter);
                lettersById[letter.LetterId] = letter;
            }
        }

        private void NotifyLettersChanged()
        {
            LettersChanged?.Invoke();
        }

        private void OnRuntimeLetterReceived(RuntimeLetterState letter)
        {
            NotifyLettersChanged();
        }

        private void OnRuntimeLetterRemoved(string letterId)
        {
            if (SelectedLetterState != null && SelectedLetterState.LetterId == letterId)
            {
                SelectedLetterState = null;
                SelectedLetter = null;
            }

            NotifyLettersChanged();
        }
    }
}
