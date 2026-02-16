using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIPageController : MonoBehaviour
{
    [Header("Main HUD (Sample.uxml)")]
    [SerializeField] private VisualTreeAsset hudUxml;

    [Header("Feature Pages")]
    [SerializeField] private VisualTreeAsset profileUxml;
    [SerializeField] private VisualTreeAsset inventoryUxml;
    [SerializeField] private VisualTreeAsset marketUxml;
    [SerializeField] private VisualTreeAsset socialUxml;
    [SerializeField] private VisualTreeAsset tasksUxml;
    [SerializeField] private VisualTreeAsset eventsUxml;
    [SerializeField] private VisualTreeAsset shopUxml; // Deals/Shop
    [SerializeField] private VisualTreeAsset leaderboardUxml;

    [Header("Settings Pages")]
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

    // Keep track of the currently active page element if needed, 
    // though usually root.Clear() is enough for a simple stack.
    private VisualElement currentPageInstance;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) 
        {
            Debug.LogError("UIPageController: UIDocument not found!");
            return;
        }
        root = uiDocument.rootVisualElement;
        
        // IMPORTANT: Ensure root doesn't block raycasts when empty or has transparent gaps
        root.pickingMode = PickingMode.Ignore; 
    }

    private void Start()
    {
        // Start with the Main HUD
        ShowHUD();
    }

    // -------------------- NAVIGATION METHODS --------------------

    public void ShowHUD()
    {
        LoadPage(hudUxml);

        // Bind HUD Buttons (VisualElements acting as buttons)
        // Note: Sample.uxml uses VisualElements with background images for buttons, not Button controls.
        // We will use RegisterCallback<ClickEvent> on them.

        // AvatarFrame -> Profile
        BindClick("AvatarFrame", ShowProfile);

        // InventoryButton -> Inventory
        BindClick("InventoryButton", ShowInventory);

        // StoreButton -> Market
        BindClick("StoreButton", ShowMarket);

        // FriendsButton -> SocialView
        BindClick("FriendsButton", ShowSocial);

        // Tasks -> TasksPage
        BindClick("Tasks", ShowTasks);

        // Events -> EventsPage
        BindClick("Events", ShowEvents);

        // Deals -> ShopUI
        BindClick("Deals", ShowShop);
    }

    public void ShowProfile()
    {
        LoadPage(profileUxml);

        // Profile Navigation
        BindClick("NavBtn1", ShowLeaderboard); // Liderlik
        BindClick("NavBtn2", ShowSettings);    // Ayarlar
            
        // Close / Back Logic
        BindClick("CloseBtn", ShowHUD); 
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowInventory()
    {
        LoadPage(inventoryUxml);
        BindClick("CloseBtn", ShowHUD);
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowMarket()
    {
        LoadPage(marketUxml);
        BindClick("CloseBtn", ShowHUD);
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowSocial()
    {
        LoadPage(socialUxml);
        BindClick("CloseBtn", ShowHUD);
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowTasks()
    {
        LoadPage(tasksUxml);
        BindClick("CloseBtn", ShowHUD);
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowEvents()
    {
        LoadPage(eventsUxml);
        BindClick("CloseBtn", ShowHUD);
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowShop()
    {
        LoadPage(shopUxml);
        BindClick("CloseBtn", ShowHUD);
        BindClick("BackButton", ShowHUD);
        BindClick("BackBtn", ShowHUD);
    }

    public void ShowLeaderboard()
    {
        LoadPage(leaderboardUxml);
        BindClick("BackButton", ShowProfile);
        BindClick("BackBtn", ShowProfile);
    }

    public void ShowSettings()
    {
        LoadPage(settingsUxml);

        BindClick("BackToProfileBtn", ShowProfile);

        // Settings Sub-pages
        BindClick("BtnGameSettings", () => LoadSubPage(gameSettingsUxml, "Oyun Ayarları"));
        BindClick("BtnLanguage",     () => LoadSubPage(languageUxml, "Dil"));
        BindClick("BtnSupport",      () => LoadSubPage(supportUxml, "Destek"));
        BindClick("BtnTerms",        () => LoadSubPage(termsUxml, "Hizmet Şartları"));
        BindClick("BtnLinkAccount",  () => LoadSubPage(linkAccountUxml, "Hesap Bağlama"));
        BindClick("BtnGiftCode",     () => LoadSubPage(giftCodeUxml, "Hediye Kodu"));
        BindClick("BtnPrivacy",      () => LoadSubPage(privacyUxml, "Gizlilik Politikası"));
        BindClick("BtnOther",        () => LoadSubPage(otherUxml, "Diğer"));
    }

    // -------------------- HELPER METHODS --------------------

    private void LoadPage(VisualTreeAsset pageUxml)
    {
        root.Clear();
        if (pageUxml == null)
        {
            // If hudUxml is null, we might just end up with empty screen (desirable if purely testing blocking)
            // But warn just in case actions were expected.
            Debug.LogWarning("UIPageController: Requested page UXML is null.");
            return;
        }

        currentPageInstance = pageUxml.Instantiate();
        
        // Ensure the loaded page container expands to fill screen if it's a full page
        // But for Sample.uxml (HUD), it might be separate bars. 
        // We generally want the instantiated content to fill root.
        currentPageInstance.style.flexGrow = 1;
        currentPageInstance.pickingMode = PickingMode.Ignore; // Let children decide blocking

        root.Add(currentPageInstance);
    }

    private void LoadSubPage(VisualTreeAsset subPageUxml, string title)
    {
        LoadPage(subPageUxml);

        // Back to Settings
        BindClick("BackButton", ShowSettings);
        BindClick("BackBtn", ShowSettings);
        BindClick("CloseBtn", ShowHUD); // Optional: global close

        // Check if there is a title label to update
        var titleLabel = root.Q<Label>("Title"); // Assuming standard naming
        if (titleLabel != null) titleLabel.text = title;
    }

    /// <summary>
    /// Binds a ClickEvent to a VisualElement (Button or any VE).
    /// </summary>
    private void BindClick(string elemName, System.Action onClick)
    {
        var elem = root.Q<VisualElement>(elemName);
        if (elem != null)
        {
            // Remove existing callbacks to avoid double-binding if we reload pages weirdly,
            // though root.Clear() usually handles cleanup of the visual hierarchy.
            // But C# lambdas might stick if we re-query? No, new instance = new elements.
            
            // Allow clicking on VisualElements (like Images/containers in Sample.uxml)
            elem.RegisterCallback<ClickEvent>(evt => 
            {
                onClick?.Invoke();
                evt.StopImmediatePropagation(); // Consuming the click is usually good for UI buttons
            });
        }
        else
        {
            // Optional: Log only if we expect it to be there. 
            // For generic "CloseBtn", it might not exist on all pages, so maybe suppress warning or keep it for debug.
            // Debug.LogWarning($"UIPageController: Element not found: {elemName}");
        }
    }
}

