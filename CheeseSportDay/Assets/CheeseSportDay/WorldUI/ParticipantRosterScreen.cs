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
        public ParticipantCard[] participantCards;
        public Text pageText;
        public Color normalCardColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        public Color selectedCardColor = new Color(1f, 0.92f, 0.35f, 1f);

        [Header("Detail")]
        public DetailScreen detailRoot;

        [Header("Team Assignment")]
        public ParticipantTeamBoardScreen teamBoardScreen;

        [Header("State")]
        public bool selectFirstParticipantOnStart = false;
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

        public void AssignSelectedParticipantToTeam(int teamIndex)
        {
            if (teamBoardScreen == null || selectedIndex < 0)
            {
                return;
            }

            teamBoardScreen.AssignParticipant(selectedIndex, teamIndex);
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
                if (participantCards == null || i >= participantCards.Length)
                {
                    continue;
                }

                ParticipantCard card = participantCards[i];
                if (card == null)
                {
                    continue;
                }

                int participantIndex = startIndex + i;
                bool hasParticipant = participantIndex >= 0 && participantIndex < GetParticipantCount();
                card.gameObject.SetActive(hasParticipant);

                if (!hasParticipant)
                {
                    continue;
                }

                card.Bind(this, participantIndex);
                card.SetContent(
                    GetString(participantNames, participantIndex, ""),
                    GetSprite(participantPortraits, participantIndex),
                    participantIndex == selectedIndex,
                    normalCardColor,
                    selectedCardColor);
            }
        }

        private void RefreshDetail()
        {
            bool hasSelection = selectedIndex >= 0 && selectedIndex < GetParticipantCount();

            if (!hasSelection)
            {
                return;
            }

            detailRoot.RefreshDetail(GetString(participantNames, selectedIndex, ""),
                GetString(participantTitles, selectedIndex, ""),
                GetString(participantDetails, selectedIndex, ""),
                GetSprite(participantPortraits, selectedIndex),
                GetInt(gameSkillValues, selectedIndex),
                GetInt(gameSenseValues, selectedIndex),
                GetInt(teamworkValues, selectedIndex),
                GetInt(physicalValues, selectedIndex),
                GetInt(luckValues, selectedIndex)
                );

            detailRoot.gameObject.SetActive(true);
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

        public int GetParticipantCount()
        {
            return participantNames == null ? 0 : participantNames.Length;
        }

        public string GetParticipantName(int participantIndex)
        {
            return GetString(participantNames, participantIndex, "");
        }

        public Sprite GetParticipantPortrait(int participantIndex)
        {
            return GetSprite(participantPortraits, participantIndex);
        }

        private int GetCardsPerPage()
        {
            if (participantCards != null && participantCards.Length > 0)
            {
                return participantCards.Length;
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
    }
}
