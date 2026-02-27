using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FarmSunberry.Social
{
    public class LeaderboardController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _rowTemplate;

        private VisualElement _root;
        private ScrollView _listContainer;
        private Button _backButton;

        private struct LeaderboardEntry
        {
            public int Rank;
            public string Nickname;
            public int Level;
            public int Experience;
            public Sprite Avatar; // Placeholder for now
        }

        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument != null)
                _root = _uiDocument.rootVisualElement;

            if (_root != null)
            {
                InitializeUI();
                LoadMockData();
            }
        }

        private void InitializeUI()
        {
            _listContainer = _root.Q<ScrollView>("LeaderboardList");
            _backButton = _root.Q<Button>("BackButton");

            if (_backButton != null)
            {
                _backButton.clicked += OnBackButtonClicked;
            }
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("Back Button Clicked - Returning to previous screen (Implementation required by Scene Manager)");
            // Add logic here to close this view or load the previous scene/UI
            gameObject.SetActive(false); // Simple close for now
        }

        private void LoadMockData()
        {
            if (_listContainer == null || _rowTemplate == null)
            {
                Debug.LogError("List Container or Row Template is missing!");
                return;
            }

            _listContainer.Clear();

            List<LeaderboardEntry> mockData = GenerateMockData(100);

            foreach (var entry in mockData)
            {
                VisualElement row = _rowTemplate.Instantiate();
                VisualElement rowRoot = row.Q<VisualElement>("RowRoot"); // Access the root element with class

                // Populate Data
                Label rankLabel = row.Q<Label>("RankLabel");
                Label nameLabel = row.Q<Label>("NameLabel");
                Label levelLabel = row.Q<Label>("LevelLabel");
                Label expLabel = row.Q<Label>("ExpLabel");
                VisualElement avatarIcon = row.Q<VisualElement>("AvatarIcon");

                if (rankLabel != null) rankLabel.text = entry.Rank.ToString();
                if (nameLabel != null) nameLabel.text = entry.Nickname;
                if (levelLabel != null) levelLabel.text = $"Lv. {entry.Level}";
                if (expLabel != null) expLabel.text = $"{entry.Experience} XP";

                // Apply Special Styles for Top 3
                if (entry.Rank == 1)
                {
                    rowRoot.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f)); // Gold
                    if (rankLabel != null) rankLabel.text = "👑 " + entry.Rank;
                }
                else if (entry.Rank == 2)
                {
                    rowRoot.style.backgroundColor = new StyleColor(new Color(0.75f, 0.75f, 0.75f)); // Silver
                }
                else if (entry.Rank == 3)
                {
                    rowRoot.style.backgroundColor = new StyleColor(new Color(0.8f, 0.5f, 0.2f)); // Bronze
                }

                _listContainer.Add(row);
            }
        }

        private List<LeaderboardEntry> GenerateMockData(int count)
        {
            List<LeaderboardEntry> data = new List<LeaderboardEntry>();
            for (int i = 1; i <= count; i++)
            {
                data.Add(new LeaderboardEntry
                {
                    Rank = i,
                    Nickname = $"Player_{i}",
                    Level = Random.Range(10, 100),
                    Experience = Random.Range(1000, 999999)
                });
            }
            return data;
        }
    }
}
