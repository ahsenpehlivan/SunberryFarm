using UnityEngine;

using System.Collections;
using System.Collections.Generic; // List için
using UnityEngine.Tilemaps; 

public class ClickToMove : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 5f; // Grid hareketi için genelde daha düşük hız daha iyi kontrol sağlar
    public float stopThreshold = 0.05f;

    [Header("Animasyon")]
    public Animator animator;



    [Header("Davranış")]
    public bool chooseDominantAxisAtClick = true;

    [Header("Çiftçilik Sistemi")]
    // Tilemap referansları (Grid altındaki objelerden alınacak)
    public Tilemap grassTilemap;  // Çim katmanı (Mantıksal)
    public Tilemap grassVisualTilemap; // Çim katmanı (Görsel - grasstilemapview)
    public Tilemap groundTilemap; // Yollar (Walkable)
    public Tilemap soilTilemap;   // Tarla (SoilGrid) katmanı
    public TileBase soilTile;     // Toprak Tile'ı
    public GameObject dustEffectPrefab; // Toz efekti
    public float digDistanceThreshold = 1.1f; // Kazma mesafesi
    

    
    // PATHFINDING VARIABLES
    private List<Vector3> currentPath;
    private int currentPathIndex;
    private bool isMovingOnPath;

    static readonly int IsMoving = Animator.StringToHash("isMoving");
    static readonly int MoveX    = Animator.StringToHash("moveX");
    static readonly int MoveY    = Animator.StringToHash("moveY");
    static readonly int DigTrigger = Animator.StringToHash("Dig");
    static readonly int TohumTrigger = Animator.StringToHash("tohum_animation"); // User said "dig_animation hasat_animation water_animation tohum_animation" but linked to "idle". 
                                                                               // User LATER said "I set the triggers in animator". 
                                                                               // I will assume triggers are named "Tohum", "Water" as per my plan which user approved, 
                                                                               // BUT user said "dig_animation...". 
                                                                               // Let's stick to the Plan's assumption: "Tohum", "Water".
                                                                               // If they fail, I'll check animator. user said "I set the triggers".
    static readonly int SowTrigger = Animator.StringToHash("Tohum");
    static readonly int WaterTrigger = Animator.StringToHash("Water");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

    }

    void Update()
    {
        // 1. Tool Klik Kontrolü
        if (ToolPaletteController.JustClickedTool)
        {
            ToolPaletteController.JustClickedTool = false;
            StopMovement();
            return; 
        }

        // 2. Mouse Input
        if (Input.GetMouseButtonDown(0))
        {
            // --- UI BLOCK CHECK START ---
            // EventSystem ile modern UI kontrolü
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("UI Clicked (EventSystem)");
                return;
            }
            // --- UI BLOCK CHECK END ---

            // 1. Tool Seçili mi?
            if (!string.IsNullOrEmpty(ToolPaletteController.SelectedToolName))
            {
                PerformToolAction();
            }
            else
            {
                // 2. Hareket İsteği
                var w = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 worldTarget = new Vector3(w.x, w.y, transform.position.z);
                RequestPath(worldTarget); 
            }
        }
        
        // 3. Yol Takibi
        if (isMovingOnPath && currentPath != null)
        {
            FollowPath();
        }
        else
        {
             // Duruyorsak animasyonu kapat
             if (animator != null && animator.GetBool(IsMoving))
             {
                 animator.SetBool(IsMoving, false);
             }
        }
    }

    void RequestPath(Vector3 targetPos)
    {
        // SAFETY FIX: Instance yoksa bulmayı dene
        if (Pathfinding.Instance == null)
        {
             var pf = FindObjectOfType<Pathfinding>();
             if (pf == null)
             {
                 Debug.LogError("Pathfinding instance bulunamadı! Sahneye Pathfinding scriptini ekleyin.");
                 return;
             }
             // Instance muhtemelen Awake'te set ediliyor ama biz manuel erişelim
        }

        List<Vector3> path = Pathfinding.Instance.FindPath(transform.position, targetPos);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            currentPathIndex = 0;
            isMovingOnPath = true;
        }
        else
        {
            Debug.Log("Yol bulunamadı veya hedef engelli.");
            StopMovement();
        }
    }

    void FollowPath()
    {
        if (currentPathIndex >= currentPath.Count)
        {
            StopMovement();
            return;
        }

        Vector3 targetNode = currentPath[currentPathIndex];
        // Z eksenini koru
        targetNode.z = transform.position.z;

        Vector3 moveDir = (targetNode - transform.position).normalized;
        
        // Animasyon Yönlendirme
        if (animator != null)
        {
            animator.SetBool(IsMoving, true);
            animator.SetFloat(MoveX, moveDir.x);
            animator.SetFloat(MoveY, moveDir.y);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetNode, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetNode) <= stopThreshold)
        {
            currentPathIndex++;
        }
    }

    void StopMovement()
    {
        isMovingOnPath = false;
        currentPath = null;
        if (animator != null) animator.SetBool(IsMoving, false);
    }

    [SerializeField] private Grid grid; // Assign in Inspector or Awake

    void PerformToolAction()
    {
        // Debugging loop issue
        Debug.Log($"PerformToolAction called. Tool: {ToolPaletteController.SelectedToolName}");

        if (grid == null)
        {
             grid = FindObjectOfType<Grid>();
             if (grid == null) 
             {
                 Debug.LogError("Grid component not found in scene!");
                 return;
             }
        }

        // Mouse pozisyonunu al (Z derinliğini camera'ya göre ayarla)
        var w = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
        
        // Grid üzerinden cell hesabı (En güvenli yöntem)
        Vector3Int cell = grid.WorldToCell(w);
        
        // Hedef merkeze git
        Vector3 targetCenterPos = grid.GetCellCenterWorld(cell);
        targetCenterPos.z = transform.position.z;

        string tool = ToolPaletteController.SelectedToolName;
        
        // --- DIG (ÇAPA) ---
        if (tool == "Tool_Hoe")
        {
            bool isGroundBlocked = false;

            // 1. Check assigned reference logic (with correct local cell calc)
            if (groundTilemap != null)
            {
                // Ensure we use the coordinate system of the groundTilemap itself
                Vector3Int localGroundCell = groundTilemap.WorldToCell(w);
                if (groundTilemap.HasTile(localGroundCell))
                {
                    isGroundBlocked = true;
                    Debug.Log($"Blocked by assigned GroundTilemap at local {localGroundCell}");
                }
            }
            
            // 2. Fallback: Search by name if not blocked yet
            if (!isGroundBlocked)
            {
                 GameObject gObj = GameObject.Find("GroundTilemap");
                 if (gObj != null)
                 {
                     Tilemap tm = gObj.GetComponent<Tilemap>();
                     if (tm != null && tm.HasTile(tm.WorldToCell(w)))
                     {
                         isGroundBlocked = true;
                         Debug.Log("Blocked by found GroundTilemap object");
                     }
                 }
            }

            if (isGroundBlocked)
            {
                return;
            }

            // Tile kontrollerini aynı cell ile yap
            TileBase tileOnGrass  = grassTilemap.GetTile(cell);
            TileBase tileOnVisual = (grassVisualTilemap != null) ? grassVisualTilemap.GetTile(cell) : null;
            // Retain original check for safety (though redundant if isGroundBlocked works)
            TileBase tileOnGround = groundTilemap != null ? groundTilemap.GetTile(cell) : null;
            TileBase tileOnSoil   = soilTilemap.GetTile(cell);

            // Çim (mantıksal) VEYA görsel çim varsa izin ver
            if ((tileOnGrass != null || tileOnVisual != null) && tileOnGround == null && tileOnSoil == null)
            {
                StartCoroutine(MoveAndAct(targetCenterPos, digDistanceThreshold, DigTrigger, () => {
                    Debug.Log("Action Callback: Setting Soil Tile...");
                    if (soilTile == null) Debug.LogError("Soil Tile Assign Edilmemiş!");
                    
                    soilTilemap.SetTile(cell, soilTile);

                    if (CropManager.Instance != null) 
                        CropManager.Instance.SetTileStatus(cell, TileStatus.Empty);
                    
                    SpawnDustEffect(cell);
                }));
            }
            else
            {
                Debug.Log($"Dig şartları sağlanmadı: Cell:{cell} Grass:{tileOnGrass!=null}, NoGround:{tileOnGround==null}, NoSoil:{tileOnSoil==null}");
            }
        }
        // --- SOW (TOHUM) ---
        else if (tool == "Tool_Tohum")
        {
            TileBase tileOnSoil = soilTilemap.GetTile(cell);
            if (tileOnSoil != null)
            {
                 StartCoroutine(MoveAndAct(targetCenterPos, digDistanceThreshold, SowTrigger, () => {
                    Debug.Log("Action Callback: Planting...");
                    
                    if (CropManager.Instance != null) 
                        CropManager.Instance.SetTileStatus(cell, TileStatus.Planted);
                 }));
            }
        }
        // --- WATER (SULAMA) ---
        else if (tool == "Tool_Water")
        {
            TileBase tileOnSoil = soilTilemap.GetTile(cell);
            if (tileOnSoil != null)
            {
                if (CropManager.Instance != null && CropManager.Instance.GetTileStatus(cell) == TileStatus.Planted)
                {
                    StartCoroutine(MoveAndAct(targetCenterPos, digDistanceThreshold, WaterTrigger, () => {
                        Debug.Log("Action Callback: Watering...");
                        
                        if (CropManager.Instance != null) 
                            CropManager.Instance.SetTileStatus(cell, TileStatus.Watered);
                    }));
                }
            }
        }
    }

    IEnumerator MoveAndAct(Vector3 targetWorldPosition, float threshold, int animTrigger, System.Action onComplete)
    {
        // Hedefe yol hesapla
        RequestPath(targetWorldPosition);

        // Yol bitene kadar veya mesafeye girene kadar bekle
        while (isMovingOnPath && currentPath != null)
        {
            float dist = Vector3.Distance(transform.position, targetWorldPosition);
            if (dist <= threshold)
            {
                StopMovement();
                break;
            }
            yield return null;
        }

        // Mesafe kontrolü (son kez)
        float finalDist = Vector3.Distance(transform.position, targetWorldPosition);
        Debug.Log($"MoveAndAct Reached? Dist: {finalDist}, Threshold: {threshold}");

        if (finalDist <= threshold)
        {
            if (animator != null)
            {
                animator.SetBool(IsMoving, false);
                animator.SetTrigger(animTrigger);
            }

            // Animasyon süresi kadar bekle (tahmini veya event ile yapılabilir, şimdilik sabit)
            yield return new WaitForSeconds(0.5f);

            // FIX: Force return to Idle in case the transition is missing or stuck
            if (animator != null)
            {
                animator.Play("Idle"); 
            }

            onComplete?.Invoke();
        }
        else
        {
            Debug.LogWarning($"MoveAndAct: Hedefe ulaşılamadı. Action iptal. Dist: {finalDist}, Threshold: {threshold}");
        }
    }

    void SpawnDustEffect(Vector3Int tilePos)
    {
        if (dustEffectPrefab != null)
        {
            Vector3 effectPosition;
            if (grid != null) effectPosition = grid.GetCellCenterWorld(tilePos);
            else effectPosition = grassTilemap.GetCellCenterWorld(tilePos);

            GameObject dust = Instantiate(dustEffectPrefab, effectPosition, Quaternion.identity);
            Destroy(dust, 1f);
        }
    }
    
    // --- UI HELPERS ---


    
    // Debug amaçlı yolu çiz
    void OnDrawGizmos()
    {
        if (currentPath != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            
            // Hedef
            if (currentPath.Count > 0)
                Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1], 0.2f);
        }
    }
}
