using System.Collections.Generic;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class LetterAreaView : MonoBehaviour
    {
        [Header("依赖服务：读取已收到信件并维护当前阅读状态")]
        [SerializeField] private LetterService letterService;

        [Header("信件列表：只让信件按钮接收点击，背景不遮挡背包卡牌")]
        [SerializeField] private Transform listRoot;
        [SerializeField] private LetterRowView rowPrefab;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private int maxVisibleLetters = 9;

        [Header("阅读面板：选中信件后显示正文")]
        [SerializeField] private GameObject readerPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text senderText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text feedbackText;

        private readonly List<LetterRowView> rows = new List<LetterRowView>();

        private void Awake()
        {
            if (letterService == null)
            {
                letterService = FindFirstObjectByType<LetterService>();
            }

            DisableDecorativeRaycasts();
        }

        private void OnEnable()
        {
            if (letterService != null)
            {
                letterService.LettersChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (letterService != null)
            {
                letterService.LettersChanged -= Refresh;
            }
        }

        public void SelectLetter(string letterId)
        {
            letterService?.SelectLetter(letterId);
        }

        public void ClearSelection()
        {
            letterService?.ClearSelection();
        }

        public void CloseSelectedLetter()
        {
            letterService?.RemoveSelectedLetter();
        }

        public void Refresh()
        {
            ClearRows();

            if (letterService == null)
            {
                SetText(emptyText, "LetterService is missing.");
                RefreshReader(null, null);
                return;
            }

            var receivedLetters = letterService.GetReceivedLetters();
            SetEmptyText(receivedLetters.Count == 0 ? "No letters received." : string.Empty);

            var visibleCount = Mathf.Min(receivedLetters.Count, Mathf.Max(0, maxVisibleLetters));
            for (var index = 0; index < visibleCount; index++)
            {
                var state = receivedLetters[index];
                letterService.TryGetLetter(state.LetterId, out var definition);
                CreateRow(definition, state);
            }

            RefreshReader(letterService.SelectedLetter, letterService.SelectedLetterState);
        }

        private void CreateRow(LetterDefinition definition, RuntimeLetterState state)
        {
            if (rowPrefab == null || listRoot == null)
            {
                return;
            }

            var row = Instantiate(rowPrefab, listRoot);
            row.gameObject.SetActive(true);
            row.Bind(this, definition, state);
            rows.Add(row);
        }

        private void ClearRows()
        {
            foreach (var row in rows)
            {
                if (row != null)
                {
                    row.gameObject.SetActive(false);
                    Destroy(row.gameObject);
                }
            }

            rows.Clear();
        }

        private void RefreshReader(LetterDefinition definition, RuntimeLetterState state)
        {
            if (definition == null || state == null)
            {
                SetReaderVisible(false);
                SetText(titleText, "");
                SetText(senderText, "");
                SetText(roundText, "");
                SetText(bodyText, "");
                SetText(feedbackText, "");
                return;
            }

            SetReaderVisible(true);
            SetText(titleText, string.IsNullOrEmpty(definition.Title) ? definition.LetterId : definition.Title);
            SetText(senderText, string.IsNullOrEmpty(definition.SenderName) ? "Unknown sender" : definition.SenderName);
            SetText(roundText, $"Received round: {state.ReceivedRound}");
            SetText(bodyText, definition.BodyText);
            SetText(feedbackText, "");
        }

        private void SetReaderVisible(bool visible)
        {
            if (readerPanel != null)
            {
                readerPanel.SetActive(visible);
            }
        }

        private void SetEmptyText(string value)
        {
            SetText(emptyText, value);
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private void DisableDecorativeRaycasts()
        {
            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.GetComponent<Button>() != null)
                {
                    continue;
                }

                image.raycastTarget = false;
            }
        }
    }
}
