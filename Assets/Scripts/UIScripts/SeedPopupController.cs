using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class SeedPopupController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement overlay;
    private VisualElement popupRoot;
    private Button closeBtn;
    private VisualElement seedListContainer;
    private Button rowTemplate;

    // State
    private string selectedSeedName; // Keeps selection even if popup closes
    private bool isPopupOpen = false;

    // Temporary Data for testing (until we have real data)
    private List<(string name, string price)> dummySeeds = new()
    {
        ("Mısır Tohumu", "$10"),
        ("Buğday Tohumu", "$15"),
        ("Domates Tohumu", "$20"),
        ("Çilek Tohumu", "$50"),
        ("Kabak Tohumu", "$30")
    };

    void Awake()
    {
        if (!uiDocument) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        
        overlay = root.Q<VisualElement>("PopupOverlay");
        popupRoot = root.Q<VisualElement>("SeedPopupRoot");
        closeBtn = root.Q<Button>("CloseBtn");
        seedListContainer = root.Q<VisualElement>("SeedList");
        rowTemplate = root.Q<Button>("SeedRow_Template");

        if (overlay == null || popupRoot == null || seedListContainer == null || rowTemplate == null)
        {
            Debug.LogError($"[SeedPopupController] Missing UI Elements! Overlay:{overlay!=null}, Popup:{popupRoot!=null}, List:{seedListContainer!=null}, Template:{rowTemplate!=null}");
            return;
        }
        else
        {
            Debug.Log("[SeedPopupController] UI Initialized Successfully.");
        }

        // Setup Events
        if (closeBtn != null) closeBtn.clicked += ClosePopup;
        
        // Overlay Click - Close popup
        overlay.RegisterCallback<ClickEvent>(OnOverlayClick);
        
        // Tool Palette Listener
        ToolPaletteController.OnToolChanged += OnToolChanged;

        // Init List
        if(rowTemplate != null) rowTemplate.style.display = DisplayStyle.None; // Hide template at runtime
        PopulateList();

        // Start Closed
        ClosePopup();
    }

    void OnDisable()
    {
        ToolPaletteController.OnToolChanged -= OnToolChanged;
        
        if (overlay != null) overlay.UnregisterCallback<ClickEvent>(OnOverlayClick);
        if (closeBtn != null) closeBtn.clicked -= ClosePopup;
    }

    private void OnToolChanged(string toolName)
    {
        Debug.Log($"[SeedPopupController] OnToolChanged received: '{toolName}'");

        if (!string.IsNullOrEmpty(toolName) && (toolName.Contains("Seed") || toolName.Contains("Tohum")))
        {
            OpenPopup();
        }
        else
        {
            ClosePopup();
        }
    }

    private void OpenPopup()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.Flex;
        isPopupOpen = true;
        RefreshSelectionVisuals();
    }

    private void ClosePopup()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.None;
        isPopupOpen = false;
    }

    private void OnOverlayClick(ClickEvent evt)
    {
        // Only close if clicked directly on overlay, not children (though pickingMode check helps)
        if (evt.target == overlay)
        {
            ClosePopup();
        }
    }

    private void PopulateList()
    {
        // Clear existing items except template
        for (int i = seedListContainer.childCount - 1; i >= 0; i--)
        {
            var child = seedListContainer[i];
            if (child != rowTemplate)
            {
                seedListContainer.Remove(child);
            }
        }

        // Generate Rows
        foreach (var data in dummySeeds)
        {
            Button newRow = new Button();
            newRow.AddToClassList("seed-row");
            newRow.style.width = 180f; // Force width from script to be safe
            newRow.style.display = DisplayStyle.Flex; 
            
            // 1. Text overlay for selection
            var mark = new Label("✓");
            mark.AddToClassList("selected-mark");
            newRow.Add(mark);

            // 2. Icon (Big)
            var icon = new VisualElement();
            icon.AddToClassList("seed-icon");
            // Setup dummy image or sprite if available
            newRow.Add(icon);

            // 3. Name (Bottom)
            var nameLbl = new Label(data.name);
            nameLbl.AddToClassList("name-label");
            newRow.Add(nameLbl);

            // Data
            newRow.userData = data.name;

            // Click
            newRow.clicked += () => OnSeedSelected(data.name);

            seedListContainer.Add(newRow);
        }
    }

    private void OnSeedSelected(string seedName)
    {
        selectedSeedName = seedName;
        Debug.Log($"[SeedPopup] Selected: {seedName}");
        
        RefreshSelectionVisuals();
        
        // Close popup after selection? User said: "Seed seçince: display: None"
        ClosePopup();
    }

    private void RefreshSelectionVisuals()
    {
        // Loop through rows
        for (int i = 0; i < seedListContainer.childCount; i++)
        {
            var element = seedListContainer[i];
             if (element == rowTemplate) continue;
             
             if (element is Button btn && btn.userData is string sName)
             {
                 if (sName == selectedSeedName)
                 {
                     btn.AddToClassList("selected");
                 }
                 else
                 {
                     btn.RemoveFromClassList("selected");
                 }
             }
        }
    }
}
