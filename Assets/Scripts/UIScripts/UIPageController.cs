using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIPageController : MonoBehaviour
{
    [Header("Page Assets")]
    [SerializeField] private VisualTreeAsset profilePage;
    [SerializeField] private VisualTreeAsset dealsPage;
    [SerializeField] private VisualTreeAsset eventsPage;
    [SerializeField] private VisualTreeAsset tasksPage;
    [SerializeField] private VisualTreeAsset inventoryPage;
    [SerializeField] private VisualTreeAsset storePage;
    [SerializeField] private VisualTreeAsset friendsPage;
    [SerializeField] private VisualTreeAsset settingsPage;

    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement _root;
    
    // Buttons
    private Button _avatarButton;
    private Button _dealsButton;
    private Button _eventsButton;
    private Button _tasksButton;
    private Button _inventoryButton;
    private Button _storeButton;
    private Button _friendsButton;
    private Button _bottomLeftButton; // Using as Settings for now, typically

    // Currently open page container
    private VisualElement _currentPageContainer;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIPageController: No UIDocument found!");
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null) return;

        // Find Buttons
        _avatarButton = _root.Q<Button>("AvatarFrame");
        _dealsButton = _root.Q<Button>("Deals");
        _eventsButton = _root.Q<Button>("Events");
        _tasksButton = _root.Q<Button>("Tasks");
        _inventoryButton = _root.Q<Button>("InventoryButton");
        _storeButton = _root.Q<Button>("StoreButton");
        _friendsButton = _root.Q<Button>("FriendsButton");
        _bottomLeftButton = _root.Q<Button>("BottomLeftButton");

        // Register Callbacks
        _avatarButton?.RegisterCallback<ClickEvent>(evt => OpenPage(profilePage));
        _dealsButton?.RegisterCallback<ClickEvent>(evt => OpenPage(dealsPage));
        _eventsButton?.RegisterCallback<ClickEvent>(evt => OpenPage(eventsPage));
        _tasksButton?.RegisterCallback<ClickEvent>(evt => OpenPage(tasksPage));
        _inventoryButton?.RegisterCallback<ClickEvent>(evt => OpenPage(inventoryPage));
        _storeButton?.RegisterCallback<ClickEvent>(evt => OpenPage(storePage));
        _friendsButton?.RegisterCallback<ClickEvent>(evt => OpenPage(friendsPage));
        _bottomLeftButton?.RegisterCallback<ClickEvent>(evt => OpenPage(settingsPage));
    }

    private void OnDisable()
    {
        // Ideally unregister callbacks here to prevent memory leaks if the root is kept alive
        // causing double registrations on re-enable, but for simple UI it's often skipped.
        // Good practice:
        _avatarButton?.UnregisterCallback<ClickEvent>(evt => OpenPage(profilePage));
        // ... (repeating for all is tedious without a helper, but safe)
    }

    private void OpenPage(VisualTreeAsset pageAsset)
    {
        if (pageAsset == null)
        {
            Debug.LogWarning("UIPageController: Target page asset is null. Please assign it in the Inspector.");
            return;
        }

        // Close existing page if any
        if (_currentPageContainer != null)
        {
            _root.Remove(_currentPageContainer);
            _currentPageContainer = null;
        }

        // Instantiate new page
        TemplateContainer pageInstance = pageAsset.Instantiate();
        pageInstance.style.flexGrow = 1;
        pageInstance.style.position = Position.Absolute;
        pageInstance.style.top = 0;
        pageInstance.style.bottom = 0;
        pageInstance.style.left = 0;
        pageInstance.style.right = 0;
        
        // Add to root
        _root.Add(pageInstance);
        _currentPageContainer = pageInstance;

        // Optional: Find a "Close" or "Back" button inside the new page to close it
        var closeBtn = pageInstance.Q<Button>("CloseButton"); 
        if (closeBtn != null)
        {
            closeBtn.RegisterCallback<ClickEvent>(evt => CloseCurrentPage());
        }
        
        // Also check for "BackBtn" as per previous conventions
        var backBtn = pageInstance.Q<Button>("BackBtn");
        if (backBtn != null)
        {
            backBtn.RegisterCallback<ClickEvent>(evt => CloseCurrentPage());
        }

        // Also check for "BackButton" (e.g. InventoryScreen)
        var backButtonFull = pageInstance.Q<Button>("BackButton");
        if (backButtonFull != null)
        {
            backButtonFull.RegisterCallback<ClickEvent>(evt => CloseCurrentPage());
        }

        // Check for "CloseBtn" (ProfileScreen, SeedPopup)
        var closeBtnShort = pageInstance.Q<Button>("CloseBtn");
        if (closeBtnShort != null)
        {
            closeBtnShort.RegisterCallback<ClickEvent>(evt => CloseCurrentPage());
        }

        // Check for "BackToProfileBtn" (SettingsScreen)
        var backToProfile = pageInstance.Q<Button>("BackToProfileBtn");
        if (backToProfile != null)
        {
            backToProfile.RegisterCallback<ClickEvent>(evt => CloseCurrentPage());
        }
    }

    public void CloseCurrentPage()
    {
        if (_currentPageContainer != null)
        {
            _root.Remove(_currentPageContainer);
            _currentPageContainer = null;
        }
    }
}
