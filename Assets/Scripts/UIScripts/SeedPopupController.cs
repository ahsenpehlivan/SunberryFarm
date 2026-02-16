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
        if (!string.IsNullOrEmpty(toolName) && (toolName.Contains("Seed") || toolName.Contains("Tohum")))
        {
            // Toggle logic: if already open and clicked again?
            // User requested: "Seed tool'a ikinci kez tıkla -> popup tekrar açılmalı"
            // Usually if I click the same tool, ToolPalette might re-fire or deselect.
            // If ToolPalette logic deselects on second click, toolName becomes null.
            // If ToolPalette logic keeps it selected, we might want to toggle?
            // For now, if "Seed" is selected, we Open.
            
            // NOTE: If user clicks Seed while Seed is already active, ToolPalette usually does nothing or re-selects.
            // If we want toggle behavior on re-click, ToolPalette needs to handle that.
            // Assuming "Selected" event means "It is now active".
            OpenPopup();
        }
        else
        {
            // Any other tool or null -> Close
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
        // Note: Template is inside seedListContainer.
        // We iterate backwards to remove generated items.
        
        // Identify generated items by user data or just remove all except template
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

            // If rowTemplate is a Button in UXML, CloneTree might wrap it? 
            // Actually, Instantiate logic:
            // TemplateContainer tc = rowTemplate.CloneTree(); 
            // OR if rowTemplate is just a VisualElement in the tree:
            
            // "Template" approach in code: usually we have a specific .uxml for rows.
            // But since we are doing inline cloning of an existing element:
            // We can manually copy properties or use a different method.
            // Since we can't easily "Clone" an element that is part of the tree without UXML template logic,
            // A common trick:
            
            // Better approach for inline template:
            // Create a new Button, copy classes.
            
            Button newRow = new Button();
            newRow.AddToClassList("seed-row");
            newRow.style.display = DisplayStyle.Flex; // Make visible
            
            // Structure
            var icon = new VisualElement();
            icon.AddToClassList("seed-icon");
            newRow.Add(icon);

            var textCol = new VisualElement();
            textCol.AddToClassList("text-col");
            newRow.Add(textCol);

            var nameLbl = new Label(data.name);
            nameLbl.AddToClassList("name-label");
            textCol.Add(nameLbl);

            var priceLbl = new Label(data.price);
            priceLbl.AddToClassList("price-label");
            textCol.Add(priceLbl);

            var mark = new Label("✓");
            mark.AddToClassList("selected-mark");
            newRow.Add(mark);

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
