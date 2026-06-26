using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Participant Roster Screen")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ParticipantRosterScreen : UdonSharpBehaviour
    {
        [Header("Data")]
        public string[] participantNames;
        public string[] participantTitles;
        public string[] participantDetails;
        public Sprite[] participantPortraits;
        public int[] gameSkillValues;
        public int[] gameSenseValues;
        public int[] teamworkValues;
        public int[] physicalValues;
        public int[] luckValues;

        [Header("Grid")]
        public ParticipantCardButton[] cardButtons;
        public GameObject[] cardRoots;
        public Image[] cardBackgroundImages;
        public Image[] cardPortraitImages;
        public Text[] cardNameTexts;
        public Text pageText;
        public Color normalCardColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        public Color selectedCardColor = new Color(1f, 0.92f, 0.35f, 1f);

        [Header("Detail")]
        public GameObject detailRoot;
        public Image detailPortraitImage;
        public Text detailNameText;
        public Text detailTitleText;
        public Text detailBodyText;
        public Text gameSkillText;
        public Text gameSenseText;
        public Text teamworkText;
        public Text physicalText;
        public Text luckText;

        [Header("Labels")]
        public string gameSkillLabel = "Skill";
        public string gameSenseLabel = "Sense";
        public string teamworkLabel = "Teamwork";
        public string physicalLabel = "Physical";
        public string luckLabel = "Luck";

        [Header("State")]
        public bool selectFirstParticipantOnStart = true;
        public bool syncSelectionForEveryone = true;

        [UdonSynced]
        private int syncedPage;

        [UdonSynced]
        private int syncedSelectedIndex = -1;

        private int currentPage;
        private int selectedIndex = -1;

        private void Start()
        {
            currentPage = 0;
            selectedIndex = selectFirstParticipantOnStart && GetParticipantCount() > 0 ? 0 : -1;

            if (syncSelectionForEveryone && Networking.IsOwner(gameObject))
            {
                syncedPage = currentPage;
                syncedSelectedIndex = selectedIndex;
                RequestSerialization();
            }

            RefreshAll();
        }

        public override void OnDeserialization()
        {
            if (!syncSelectionForEveryone)
            {
                return;
            }

            currentPage = ClampPage(syncedPage);
            selectedIndex = ClampParticipantIndex(syncedSelectedIndex);
            RefreshAll();
        }

        public void SelectParticipant(int participantIndex)
        {
            int clampedIndex = ClampParticipantIndex(participantIndex);
            if (clampedIndex < 0)
            {
                return;
            }

            selectedIndex = clampedIndex;
            currentPage = ClampPage(selectedIndex / GetCardsPerPage());
            PublishState();
            RefreshAll();
        }

        public void NextPage()
        {
            ShowPage(currentPage + 1);
        }

        public void PreviousPage()
        {
            ShowPage(currentPage - 1);
        }

        public void ShowPage(int page)
        {
            currentPage = ClampPage(page);
            PublishState();
            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshGrid();
            RefreshDetail();
            RefreshPageText();
        }

        private void RefreshGrid()
        {
            int cardCount = GetCardsPerPage();
            int startIndex = currentPage * cardCount;

            for (int i = 0; i < cardCount; i++)
            {
                int participantIndex = startIndex + i;
                bool hasParticipant = participantIndex >= 0 && participantIndex < GetParticipantCount();

                if (cardRoots != null && i < cardRoots.Length && cardRoots[i] != null)
                {
                    cardRoots[i].SetActive(hasParticipant);
                }

                if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null)
                {
                    cardButtons[i].rosterScreen = this;
                    cardButtons[i].participantIndex = hasParticipant ? participantIndex : -1;
                }

                if (!hasParticipant)
                {
                    continue;
                }

                if (cardNameTexts != null && i < cardNameTexts.Length && cardNameTexts[i] != null)
                {
                    cardNameTexts[i].text = GetString(participantNames, participantIndex, "");
                }

                if (cardPortraitImages != null && i < cardPortraitImages.Length && cardPortraitImages[i] != null)
                {
                    Sprite portrait = GetSprite(participantPortraits, participantIndex);
                    cardPortraitImages[i].sprite = portrait;
                    cardPortraitImages[i].enabled = portrait != null;
                }

                if (cardBackgroundImages != null && i < cardBackgroundImages.Length && cardBackgroundImages[i] != null)
                {
                    cardBackgroundImages[i].color = participantIndex == selectedIndex ? selectedCardColor : normalCardColor;
                }
            }
        }

        private void RefreshDetail()
        {
            bool hasSelection = selectedIndex >= 0 && selectedIndex < GetParticipantCount();

            if (detailRoot != null)
            {
                detailRoot.SetActive(hasSelection);
            }

            if (!hasSelection)
            {
                return;
            }

            if (detailNameText != null)
            {
                detailNameText.text = GetString(participantNames, selectedIndex, "");
            }

            if (detailTitleText != null)
            {
                detailTitleText.text = GetString(participantTitles, selectedIndex, "");
            }

            if (detailBodyText != null)
            {
                detailBodyText.text = GetString(participantDetails, selectedIndex, "");
            }

            if (detailPortraitImage != null)
            {
                Sprite portrait = GetSprite(participantPortraits, selectedIndex);
                detailPortraitImage.sprite = portrait;
                detailPortraitImage.enabled = portrait != null;
            }

            SetStatText(gameSkillText, gameSkillLabel, GetInt(gameSkillValues, selectedIndex));
            SetStatText(gameSenseText, gameSenseLabel, GetInt(gameSenseValues, selectedIndex));
            SetStatText(teamworkText, teamworkLabel, GetInt(teamworkValues, selectedIndex));
            SetStatText(physicalText, physicalLabel, GetInt(physicalValues, selectedIndex));
            SetStatText(luckText, luckLabel, GetInt(luckValues, selectedIndex));
        }

        private void RefreshPageText()
        {
            if (pageText == null)
            {
                return;
            }

            int pageCount = GetPageCount();
            pageText.text = (currentPage + 1).ToString() + " / " + pageCount.ToString();
        }

        private void PublishState()
        {
            if (!syncSelectionForEveryone)
            {
                return;
            }

            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(localPlayer) && !Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            syncedPage = currentPage;
            syncedSelectedIndex = selectedIndex;
            RequestSerialization();
        }

        private int GetParticipantCount()
        {
            return participantNames == null ? 0 : participantNames.Length;
        }

        private int GetCardsPerPage()
        {
            if (cardButtons != null && cardButtons.Length > 0)
            {
                return cardButtons.Length;
            }

            if (cardRoots != null && cardRoots.Length > 0)
            {
                return cardRoots.Length;
            }

            return 1;
        }

        private int GetPageCount()
        {
            int participantCount = GetParticipantCount();
            int cardsPerPage = GetCardsPerPage();
            if (participantCount <= 0)
            {
                return 1;
            }

            return (participantCount + cardsPerPage - 1) / cardsPerPage;
        }

        private int ClampPage(int page)
        {
            int pageCount = GetPageCount();
            if (page < 0)
            {
                return 0;
            }

            if (page >= pageCount)
            {
                return pageCount - 1;
            }

            return page;
        }

        private int ClampParticipantIndex(int participantIndex)
        {
            if (participantIndex < 0 || participantIndex >= GetParticipantCount())
            {
                return -1;
            }

            return participantIndex;
        }

        private string GetString(string[] values, int index, string fallback)
        {
            if (values == null || index < 0 || index >= values.Length || values[index] == null)
            {
                return fallback;
            }

            return values[index];
        }

        private int GetInt(int[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
            {
                return 0;
            }

            return values[index];
        }

        private Sprite GetSprite(Sprite[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
            {
                return null;
            }

            return values[index];
        }

        private void SetStatText(Text target, string label, int value)
        {
            if (target != null)
            {
                target.text = label + " " + value.ToString();
            }
        }
    }
}
