using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToolPaletteController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Layout & Anim")]
    [SerializeField] private int itemSizePx = 256;   
    [SerializeField] private int itemGapPx  = 12;    
    [SerializeField] private int paddingPx  = 12;    
    [SerializeField] private float animationDuration = 0.18f;
    [SerializeField] private bool openOnStart = false;

    [Header("Selection Highlight")]
    [SerializeField] private int   highlightBorderPx = 4;                             
    [SerializeField] private Color highlightColor    = new(1f, 0.93f, 0.63f, 1f);    
    [SerializeField] private Color highlightGlow     = new(1f, 1f, 0.8f, 0.25f);      

    private VisualElement bottomRightButton;
    private VisualElement toolPalette;
    private bool isOpen;
    private IVisualElementScheduledItem anim;
    private float t0, h0, h1, o0, o1;
    private void StopAll(EventBase e)
    {
        UIFrameGuard.ConsumedPointerDownThisFrame = true; // << EKLENDİ
        e.StopImmediatePropagation();
    }

    private VisualElement selectedTool;
    public static string SelectedToolName { get; private set; }
    public static bool JustClickedTool;

    void Reset() { uiDocument = GetComponent<UIDocument>(); }
    void Awake() { if (!uiDocument) uiDocument = GetComponent<UIDocument>(); }

    void OnEnable()
    {
        if (!uiDocument) { Debug.LogError("UIDocument yok."); return; }

        var ps = uiDocument.panelSettings;
        if (ps != null)
        {
            ps.scaleMode    = PanelScaleMode.ConstantPixelSize;
            ps.scale        = 1f;
            ps.referenceDpi = 96;
        }

        var root = uiDocument.rootVisualElement;
        bottomRightButton = root.Q<VisualElement>("BottomRightButton");
        toolPalette       = root.Q<VisualElement>("ToolPalette");

        if (bottomRightButton == null || toolPalette == null)
        {
            Debug.LogError("BottomRightButton veya ToolPalette bulunamadı.");
            return;
        }

        // Add blocking class so ClickToMove detects it
        // toolPalette.AddToClassList("blocks-move"); // DEPRECATED: Using UIFrameGuard now

        // ToolPalette içindeki pointer olaylarını tüket
        toolPalette.RegisterCallback<PointerDownEvent>(StopAll, TrickleDown.TrickleDown);
        toolPalette.RegisterCallback<PointerUpEvent>(StopAll,   TrickleDown.TrickleDown);
        toolPalette.RegisterCallback<WheelEvent>(StopAll,       TrickleDown.TrickleDown);

        bottomRightButton.pickingMode = PickingMode.Position;
        toolPalette.pickingMode       = PickingMode.Position;

        // HOIST: paleti köke taşı
        toolPalette.RemoveFromHierarchy();
        root.Add(toolPalette);

        // Mutlak konum; diğer UI'ların üstünde görünmesi için en sona ekledik ve BringToFront çağıracağız
        toolPalette.style.position = Position.Absolute;
        toolPalette.style.overflow = Overflow.Visible;

        // Bu satır eski koddandı — artık KULLANMIYORUZ:
        // toolPalette.style.bottom = new Length(100, LengthUnit.Percent);
        // toolPalette.style.right  = 0;

        toolPalette.style.flexDirection = FlexDirection.Column;
        toolPalette.style.paddingLeft   = paddingPx;
        toolPalette.style.paddingRight  = paddingPx;
        toolPalette.style.paddingTop    = paddingPx;
        toolPalette.style.paddingBottom = paddingPx;

        ApplyItemSizing();
        RegisterToolClickHandlers();

        isOpen = openOnStart;
        if (isOpen)
        {
            toolPalette.style.display = DisplayStyle.Flex;
            toolPalette.style.opacity = 1f;
            toolPalette.style.height  = ComputeContentHeight();
        }
        else
        {
            toolPalette.style.display = DisplayStyle.None;
            toolPalette.style.opacity = 0f;
            toolPalette.style.height  = 0f;
        }

        // İlk hizalama
        RepositionToButton();

        // Her ihtimale karşı en üste al (zIndex yerine)
        toolPalette.BringToFront();

        // Yerleşim değişince yeniden hizala
        root.RegisterCallback<GeometryChangedEvent>(_ => { RepositionToButton(); toolPalette.BringToFront(); });
        bottomRightButton.RegisterCallback<GeometryChangedEvent>(_ => { RepositionToButton(); toolPalette.BringToFront(); });

        // Layout tamamlandığında bir kere daha
        root.schedule.Execute(() => { RepositionToButton(); toolPalette.BringToFront(); });

        bottomRightButton.RegisterCallback<ClickEvent>(OnBottomRightClick);
        // UIFrameGuard için consume
        bottomRightButton.RegisterCallback<PointerDownEvent>(StopAll, TrickleDown.TrickleDown);
        bottomRightButton.RegisterCallback<PointerUpEvent>(StopAll, TrickleDown.TrickleDown);
    }

    // Paleti butonun hemen üstüne hizalar
    void RepositionToButton()
    {
        if (uiDocument == null || bottomRightButton == null || toolPalette == null) return;

        var root = uiDocument.rootVisualElement;
        var rb = root.worldBound;
        var bb = bottomRightButton.worldBound;

        // Sağdan ve alttan mesafeler
        toolPalette.style.right  = rb.xMax - bb.xMax;   // root sağ - buton sağ
        toolPalette.style.bottom = rb.yMax - bb.yMin;   // root alt - buton üst
    }



    void OnDisable()
    {
        if (bottomRightButton != null)
            bottomRightButton.UnregisterCallback<ClickEvent>(OnBottomRightClick);

        anim?.Pause();
        anim = null;
    }

    void OnBottomRightClick(ClickEvent evt)
    {
        if (evt.target != bottomRightButton) return;
        Toggle();
    }

    void Toggle()
    {
        ApplyItemSizing(); 

        float content = ComputeContentHeight();
        isOpen = !isOpen;

        if (isOpen && toolPalette.style.display == DisplayStyle.None)
            toolPalette.style.display = DisplayStyle.Flex;

        t0 = Time.realtimeSinceStartup;
        h0 = toolPalette.resolvedStyle.height;
        h1 = isOpen ? content : 0f;
        o0 = toolPalette.resolvedStyle.opacity;
        o1 = isOpen ? 1f : 0f;

        anim?.Pause();
        anim = toolPalette.schedule.Execute(Animate).Every(16); 
    }

    void Animate()
    {
        float t = Mathf.InverseLerp(t0, t0 + animationDuration, Time.realtimeSinceStartup);
        t = Mathf.Clamp01(t);
        float eased = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic

        toolPalette.style.height  = Mathf.Lerp(h0, h1, eased);
        toolPalette.style.opacity = Mathf.Lerp(o0, o1, eased);

        if (t >= 1f)
        {
            anim?.Pause();
            anim = null;
            if (!isOpen) toolPalette.style.display = DisplayStyle.None;
        }
    }

    void ApplyItemSizing()
    {
        int n = toolPalette.childCount;
        for (int i = 0; i < n; i++)
        {
            if (toolPalette[i] is VisualElement ve)
            {
                ve.style.width  = itemSizePx;
                ve.style.height = itemSizePx;
                ve.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;

                // dikey aralık
                ve.style.marginBottom = (i < n - 1) ? itemGapPx : 0;

                // varsayılan: çerçeve kapalı
                ClearHighlight(ve);
                // köşeler yuvarlak kalsın
                ve.style.borderTopLeftRadius     = 12;
                ve.style.borderTopRightRadius    = 12;
                ve.style.borderBottomLeftRadius  = 12;
                ve.style.borderBottomRightRadius = 12;
            }
        }
    }

    void RegisterToolClickHandlers()
    {
        int n = toolPalette.childCount;
        for (int i = 0; i < n; i++)
        {
            if (toolPalette[i] is VisualElement ve)
            {
                ve.UnregisterCallback<ClickEvent>(OnToolClick);
                ve.RegisterCallback<ClickEvent>(OnToolClick);

                ve.UnregisterCallback<MouseOverEvent>(OnToolHover);
                ve.UnregisterCallback<MouseOutEvent>(OnToolOut);
                ve.RegisterCallback<MouseOverEvent>(OnToolHover);
                ve.RegisterCallback<MouseOutEvent>(OnToolOut);
            }
        }
    }

    void OnToolClick(ClickEvent e)
    {
        UIFrameGuard.ConsumedPointerDownThisFrame = true;

        e.StopImmediatePropagation();

        if (e.currentTarget is not VisualElement ve) return;

        // EĞER tıklanan zaten seçiliyse -> seçim kaldır
        if (selectedTool == ve)
        {
            ClearHighlight(selectedTool);
            selectedTool = null;
            SelectedToolName = null;
            // JustClickedTool = true; // İstersen bunu da true yapabilirsin ki "click" algılansın
            // Ama genelde deselection için ayrıca bir şey yapmak gerekirse buraya eklenir.
            Debug.Log($"[ToolPalette] Deselected: {ve.name}");
            return;
        }

        // FARKLI bir toola tıklandıysa -> eskini temizle, yeniyi seç
        if (selectedTool != null)
            ClearHighlight(selectedTool);

        selectedTool = ve;
        ApplyHighlight(selectedTool);

        SelectedToolName = selectedTool.name;
        JustClickedTool  = true;

        Debug.Log($"[ToolPalette] Selected: {selectedTool.name}");
    }

    void OnToolHover(MouseOverEvent e)
    {
        if (e.currentTarget is not VisualElement ve) return;
        if (ve != selectedTool)
            ve.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.06f));
    }

    void OnToolOut(MouseOutEvent e)
    {
        if (e.currentTarget is not VisualElement ve) return;
        if (ve != selectedTool)
            ve.style.backgroundColor = StyleKeyword.Null; 
    }

    void ApplyHighlight(VisualElement ve)
    {
        // Kenarlık
        ve.style.borderTopWidth    = highlightBorderPx;
        ve.style.borderRightWidth  = highlightBorderPx;
        ve.style.borderBottomWidth = highlightBorderPx;
        ve.style.borderLeftWidth   = highlightBorderPx;

        ve.style.borderTopColor    = highlightColor;
        ve.style.borderRightColor  = highlightColor;
        ve.style.borderBottomColor = highlightColor;
        ve.style.borderLeftColor   = highlightColor;

        // Hafif parıltı (dolgu)
        ve.style.backgroundColor = new StyleColor(highlightGlow);
    }

    void ClearHighlight(VisualElement ve)
    {
        ve.style.borderTopWidth    = 0;
        ve.style.borderRightWidth  = 0;
        ve.style.borderBottomWidth = 0;
        ve.style.borderLeftWidth   = 0;

        ve.style.borderTopColor    = StyleKeyword.Null;
        ve.style.borderRightColor  = StyleKeyword.Null;
        ve.style.borderBottomColor = StyleKeyword.Null;
        ve.style.borderLeftColor   = StyleKeyword.Null;

        ve.style.backgroundColor   = StyleKeyword.Null;
    }

    float ComputeContentHeight()
    {
        int n = toolPalette.childCount;
        return n <= 0
            ? paddingPx * 2
            : (n * itemSizePx) + ((n - 1) * itemGapPx) + (paddingPx * 2);
    }

    public string GetSelectedToolName() => selectedTool?.name;
}
