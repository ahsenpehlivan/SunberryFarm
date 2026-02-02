using UnityEngine;
using UnityEngine.UIElements;

public class UIPageController : MonoBehaviour
{
    [Header("Pages (UXML)")]
    [SerializeField] private VisualTreeAsset profileUxml;
    [SerializeField] private VisualTreeAsset leaderboardUxml;
    [Header("Settings Pages (UXML)")]
[SerializeField] private VisualTreeAsset settingsUxml;

[SerializeField] private VisualTreeAsset gameSettingsUxml;
[SerializeField] private VisualTreeAsset languageUxml;
[SerializeField] private VisualTreeAsset supportUxml;
[SerializeField] private VisualTreeAsset termsUxml;
[SerializeField] private VisualTreeAsset linkAccountUxml;
[SerializeField] private VisualTreeAsset giftCodeUxml;
[SerializeField] private VisualTreeAsset privacyUxml;
[SerializeField] private VisualTreeAsset otherUxml;


    private UIDocument uiDocument;
    private VisualElement root;

    private VisualElement currentPage;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
    }

    private void Start()
    {
        ShowProfile();
    }

    // -------------------- PAGE LOADERS --------------------

    private void ShowProfile()
    {
        LoadPage(profileUxml);

        // Profile sayfasındaki butonları bağla
        var navBtn1 = root.Q<Button>("NavBtn1"); // Liderlik
        if (navBtn1 != null)
            navBtn1.clicked += ShowLeaderboard;

        var navBtn2 = root.Q<Button>("NavBtn2"); // Ayarlar
        if (navBtn2 != null)
            navBtn2.clicked += ShowSettings;

         // var navBtn3 = root.Q<Button>("NavBtn3"); // Kooperatif
    }

    private void ShowLeaderboard()
    {
        LoadPage(leaderboardUxml);

        // Leaderboard sayfasındaki geri butonunu bağla
        var backBtn = root.Q<Button>("BackBtn");
        if (backBtn != null)
            backBtn.clicked += ShowProfile;
    }

    private void ShowSettings()
    {
        LoadPage(settingsUxml);

        // Profil'e geri
        var backToProfile = root.Q<Button>("BackToProfileBtn");
        if (backToProfile != null)
            backToProfile.clicked += ShowProfile;

        // Liste butonları -> alt sayfalar
        BindButton("BtnGameSettings", () => LoadSubPage(gameSettingsUxml, "Oyun Ayarları"));
        BindButton("BtnLanguage", () => LoadSubPage(languageUxml, "Dil"));
        BindButton("BtnSupport", () => LoadSubPage(supportUxml, "Destek"));
        BindButton("BtnTerms", () => LoadSubPage(termsUxml, "Hizmet Şartları"));
        BindButton("BtnLinkAccount", () => LoadSubPage(linkAccountUxml, "Hesap Bağlama"));
        BindButton("BtnGiftCode", () => LoadSubPage(giftCodeUxml, "Hediye Kodu"));
        BindButton("BtnPrivacy", () => LoadSubPage(privacyUxml, "Gizlilik Politikası"));
        BindButton("BtnOther", () => LoadSubPage(otherUxml, "Diğer"));
    }

    private void LoadPage(VisualTreeAsset pageUxml)
    {
        root.Clear(); // ekrandaki her şeyi sil
        if (pageUxml == null)
        {
            Debug.LogError("UIPageController: Page UXML is not assigned!");
            return;
        }

        currentPage = pageUxml.Instantiate();
        root.Add(currentPage);
    }

    private void BindButton(string name, System.Action onClick)
    {
        var btn = root.Q<Button>(name);
        if (btn != null)
            btn.clicked += () => onClick?.Invoke();
        else
            Debug.LogWarning($"UIPageController: Button not found: {name}");
    }

    private void LoadSubPage(VisualTreeAsset subPageUxml, string title)
    {
        LoadPage(subPageUxml);

        // Alt sayfada geri -> Settings
        var backBtn = root.Q<Button>("BackBtn");
        if (backBtn != null)
            backBtn.clicked += ShowSettings;

        // İstersen Title'ı runtime'da da set edebilirsin (UXML'de zaten yazacak)
        // var titleLabel = root.Q<Label>("Title");
        // if (titleLabel != null) titleLabel.text = title;
    }
}
