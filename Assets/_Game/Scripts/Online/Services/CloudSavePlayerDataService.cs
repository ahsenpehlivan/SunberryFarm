using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSavePlayerDataService
{
    // Cloud Save keys
    public const string KEY_PROFILE = "player_profile";
    public const string KEY_PROGRESSION = "player_progression";
    public const string KEY_INVENTORY = "inv_stacks";

    public async Task<(bool hasProfile, PlayerProfile profile, PlayerProgression prog, InventoryData inv)> LoadInitialDataAsync()
    {
        string playerId = AuthenticationService.Instance.PlayerId;

        var keys = new HashSet<string> { KEY_PROFILE, KEY_PROGRESSION, KEY_INVENTORY };
        Dictionary<string, string> loaded = await LoadStringsAsync(keys);

        bool hasProfile = loaded.TryGetValue(KEY_PROFILE, out var profileJson) && !string.IsNullOrEmpty(profileJson);

        PlayerProfile profile = null;
        PlayerProgression prog;
        InventoryData inv;

        if (hasProfile)
        {
            profile = JsonUtility.FromJson<PlayerProfile>(profileJson);
            profile.lastLoginAtUtc = DateTime.UtcNow.ToString("o"); // update login time

            prog = loaded.TryGetValue(KEY_PROGRESSION, out var progJson) && !string.IsNullOrEmpty(progJson)
                ? JsonUtility.FromJson<PlayerProgression>(progJson)
                : new PlayerProgression { level = 1, xp = 0 };

            inv = loaded.TryGetValue(KEY_INVENTORY, out var invJson) && !string.IsNullOrEmpty(invJson)
                ? JsonUtility.FromJson<InventoryData>(invJson)
                : new InventoryData();

            await SaveProfileAsync(profile);
        }
        else
        {
            prog = new PlayerProgression { level = 1, xp = 0 };
            inv = new InventoryData();
        }

        return (hasProfile, profile, prog, inv);
    }

    public async Task SaveNewProfileAsync(string displayName, string avatarId, string realmId)
    {
        string playerId = AuthenticationService.Instance.PlayerId;

        PlayerProfile profile = new PlayerProfile
        {
            playerId = string.IsNullOrEmpty(playerId) ? "unknown" : playerId,
            displayName = displayName,
            avatarId = avatarId,
            realmId = realmId,
            createdAtUtc = DateTime.UtcNow.ToString("o"),
            lastLoginAtUtc = DateTime.UtcNow.ToString("o")
        };

        PlayerProgression prog = new PlayerProgression { level = 1, xp = 0 };
        InventoryData inv = new InventoryData();

        await SaveAllAsync(profile, prog, inv);
    }

    public async Task SaveProfileAsync(PlayerProfile profile)
    {
        var data = new Dictionary<string, object>
        {
            { KEY_PROFILE, JsonUtility.ToJson(profile) }
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public async Task SaveProgressionAsync(PlayerProgression prog)
    {
        var data = new Dictionary<string, object>
        {
            { KEY_PROGRESSION, JsonUtility.ToJson(prog) }
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public async Task SaveInventoryAsync(InventoryData inv)
    {
        var data = new Dictionary<string, object>
        {
            { KEY_INVENTORY, JsonUtility.ToJson(inv) }
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public async Task SaveAllAsync(PlayerProfile profile, PlayerProgression prog, InventoryData inv)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { KEY_PROFILE, JsonUtility.ToJson(profile) },
                { KEY_PROGRESSION, JsonUtility.ToJson(prog) },
                { KEY_INVENTORY, JsonUtility.ToJson(inv) }
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }
        catch (CloudSaveValidationException ex)
        {
            Debug.LogError($"[CloudSave] SaveAllAsync Validation Error: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CloudSave] SaveAllAsync Failed: {ex.Message}");
        }
    }

    private async Task<Dictionary<string, string>> LoadStringsAsync(HashSet<string> keys)
    {
        if (keys == null || keys.Count == 0)
            throw new Exception("Cloud Save LoadAsync called with empty keys set.");

        foreach (var k in keys)
        {
            if (string.IsNullOrWhiteSpace(k))
                throw new Exception("Cloud Save key is null/empty/whitespace.");

            if (k.Length > 256)
                throw new Exception($"Cloud Save key too long: {k.Length} -> {k}");
        }

        Debug.Log("[CloudSave] Loading keys: " + string.Join(", ", keys));

        var result = new Dictionary<string, string>();
        
        try
        {
            var loadResult = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            foreach (var key in keys)
            {
                if (loadResult.TryGetValue(key, out var item))
                {
                    result[key] = item.Value.GetAsString();
                }
            }
        }
        catch (CloudSaveValidationException ex)
        {
            // Expected when keys don't exist yet on a completely fresh account for some Unity SDK versions
            Debug.Log($"[CloudSave] Validation exception (likely new account/missing keys): {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CloudSave] Failed to load keys: {ex.Message}");
        }

        return result;
    }

    public async Task DeleteProfileDataAsync()
    {
        try 
        {
            Debug.Log("[CloudSave] Deleting profile data for fresh start...");
            await CloudSaveService.Instance.Data.Player.DeleteAsync(KEY_PROFILE);
            await CloudSaveService.Instance.Data.Player.DeleteAsync(KEY_PROGRESSION);
            await CloudSaveService.Instance.Data.Player.DeleteAsync(KEY_INVENTORY);
            Debug.Log("[CloudSave] Profile data deleted successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CloudSave] Failed to delete profile data: {e.Message}");
        }
    }
}
