using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class DocumentPopupPanelView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private DocumentService documentService;
        [SerializeField] private SharedActorSlotView sharedActorSlot;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text optionAText;
        [SerializeField] private TMP_Text optionBText;
        [SerializeField] private TMP_Text proposerFeedbackText;
        [SerializeField] private Image stampImage;

        [Header("Buttons")]
        [SerializeField] private Button optionAButton;
        [SerializeField] private Button optionBButton;

        private RuntimeDocumentQueueEntry currentEntry;
        private DocumentDefinition currentDocument;

        private void Awake()
        {
            ResolveDependencies();
            Hide();
        }

        private void OnEnable()
        {
            if (documentService != null)
            {
                documentService.DocumentsChanged += RefreshCurrentDocument;
            }
        }

        private void OnDisable()
        {
            if (documentService != null)
            {
                documentService.DocumentsChanged -= RefreshCurrentDocument;
            }
        }

        [ContextMenu("Show Preview")]
        public void ShowPreview()
        {
            Show(
                "Document Preview",
                "This popup is the desk document frame. Document queue and result logic are added by the document system stage.",
                "Option A",
                "Option B");
        }

        [ContextMenu("Show Next Pending Document")]
        public void ShowNextPendingDocument()
        {
            if (documentService == null ||
                !documentService.TryGetNextPendingDocument(out var entry, out var document))
            {
                SetText(proposerFeedbackText, "No pending document.");
                return;
            }

            ShowDocument(entry, document);
        }

        public void Show(string title, string body, string optionA, string optionB)
        {
            currentEntry = null;
            currentDocument = null;
            SetText(titleText, title);
            SetText(bodyText, body);
            SetText(optionAText, optionA);
            SetText(optionBText, optionB);
            SetText(proposerFeedbackText, string.Empty);
            SetButtonsInteractable(true);

            if (stampImage != null)
            {
                stampImage.enabled = false;
            }

            gameObject.SetActive(true);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            currentEntry = null;
            currentDocument = null;
            gameObject.SetActive(false);
        }

        public void OnOptionAClicked()
        {
            ResolveCurrentDocument(DocumentOptionType.A);
        }

        public void OnOptionBClicked()
        {
            ResolveCurrentDocument(DocumentOptionType.B);
        }

        private void ShowDocument(RuntimeDocumentQueueEntry entry, DocumentDefinition document)
        {
            currentEntry = entry;
            currentDocument = document;
            SetText(titleText, document.Title);
            SetText(bodyText, document.BodyText);
            SetText(optionAText, document.OptionA.Text);
            SetText(optionBText, document.OptionB.Text);
            SetText(proposerFeedbackText, string.Empty);
            SetButtonsInteractable(true);

            if (stampImage != null)
            {
                stampImage.enabled = false;
            }

            ShowProposer(document);
            gameObject.SetActive(true);
        }

        private void ResolveCurrentDocument(DocumentOptionType optionType)
        {
            if (documentService == null || currentEntry == null || currentDocument == null)
            {
                SetText(proposerFeedbackText, "No pending document is open.");
                return;
            }

            var result = documentService.ResolveDocument(currentEntry, optionType);
            SetText(proposerFeedbackText, FormatResultFeedback(result));
            if (stampImage != null)
            {
                stampImage.enabled = result.Success;
            }

            if (result.Success)
            {
                SetButtonsInteractable(false);
                currentEntry = null;
                currentDocument = null;
            }
        }

        private void RefreshCurrentDocument()
        {
            if (currentEntry == null || currentDocument == null)
            {
                return;
            }

            if (!documentService.TryGetDefinition(currentDocument.DocumentId, out var refreshedDocument))
            {
                Hide();
                return;
            }

            currentDocument = refreshedDocument;
        }

        private void ShowProposer(DocumentDefinition document)
        {
            if (sharedActorSlot == null)
            {
                return;
            }

            if (documentService != null &&
                documentService.TryGetCharacter(document.ProposerCharacterId, out var character))
            {
                sharedActorSlot.ShowActor(character.CharacterName, "Document proposer", null);
                return;
            }

            if (!string.IsNullOrEmpty(document.ProposerCharacterId))
            {
                sharedActorSlot.ShowActor(document.ProposerCharacterId, "Document proposer", null);
            }
        }

        private void ResolveDependencies()
        {
            if (documentService == null)
            {
                documentService = FindFirstObjectByType<DocumentService>();
            }

            if (sharedActorSlot == null)
            {
                sharedActorSlot = FindFirstObjectByType<SharedActorSlotView>(FindObjectsInactive.Include);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (optionAButton != null)
            {
                optionAButton.interactable = interactable;
            }

            if (optionBButton != null)
            {
                optionBButton.interactable = interactable;
            }
        }

        private static string FormatResultFeedback(DocumentResolutionResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(result.FactionFeedbackText))
            {
                return result.Message;
            }

            if (string.IsNullOrEmpty(result.Message))
            {
                return result.FactionFeedbackText;
            }

            return $"{result.Message}\n{result.FactionFeedbackText}";
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
