using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using UnityEngine;

public class UGSBootstrap : MonoBehaviour
{
    public static UGSBootstrap Instance { get; private set; }

    public PlayerProfile Profile { get; private set; }
    public PlayerProgression Progression { get; private set; }
    public InventoryData Inventory { get; private set; }

    CloudSavePlayerDataService _cloud;

    public static event Action OnUGSReady;
    public static event Action<string> OnUGSError;

    public bool HasProfile { get; private set; }

    private async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _cloud = new CloudSavePlayerDataService();

        await InitializeAndLoginAndLoad();
    }

    private async Task InitializeAndLoginAndLoad()
    {
        try
        {
            var options = new InitializationOptions()
                .SetEnvironmentName("development");

            await UnityServices.InitializeAsync(options);

            // Editor / şimdilik: anonymous
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var (hasProfile, profile, prog, inv) = await _cloud.LoadInitialDataAsync();

            HasProfile = hasProfile;
            Profile = profile;
            Progression = prog;
            Inventory = inv;

            Debug.Log($"UGS Ready | PlayerId={AuthenticationService.Instance.PlayerId} | HasProfile={HasProfile}");
            if (HasProfile)
            {
                Debug.Log($"Name={Profile.displayName} | Lv={Progression.level}");
            }
            
            OnUGSReady?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UGSBootstrap] Init Failed: {ex.Message}");
            OnUGSError?.Invoke(ex.Message);
        }
    }

    public async Task RetryInitialization()
    {
        await InitializeAndLoginAndLoad();
    }

    public async Task CreateProfileAsync(string displayName, string avatarId, string realmId)
    {
        await _cloud.SaveNewProfileAsync(displayName, avatarId, realmId);
        
        // Reload data to populate properties
        var (hasProfile, profile, prog, inv) = await _cloud.LoadInitialDataAsync();
        HasProfile = hasProfile;
        Profile = profile;
        Progression = prog;
        Inventory = inv;
    }

    // Test için: Envantere item ekleyip kaydet
    public async Task AddItemAndSave(string itemId, int amount)
    {
        var stack = Inventory.items.Find(x => x.itemId == itemId);
        if (stack == null)
        {
            stack = new InventoryStack { itemId = itemId, qty = 0 };
            Inventory.items.Add(stack);
        }

        stack.qty += amount;
        await _cloud.SaveInventoryAsync(Inventory);
        Debug.Log($"Saved inventory: {itemId} +{amount} => {stack.qty}");
    }
}
