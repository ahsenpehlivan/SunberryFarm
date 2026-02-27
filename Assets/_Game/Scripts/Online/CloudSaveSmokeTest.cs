using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class CloudSaveSmokeTest : MonoBehaviour
{
    private async void Start()
    {
        await Run();
    }

    private async Task Run()
    {
        Debug.Log("[TEST] Wait for UGSBootstrap to initialize...");
        while (!AuthenticationService.Instance.IsSignedIn)
        {
            await Task.Yield();
        }

        Debug.Log("[TEST] Save...");
        try 
        {
            var data = new Dictionary<string, object> 
            { 
                { "MySaveKey", "Hello World" } 
            };
            
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }
        catch (CloudSaveValidationException ex)
        {
            Debug.LogError($"[TEST] Save Validation Error: {ex.Message}");
        }

        Debug.Log("[TEST] Load...");
        var keys = new HashSet<string> { "MySaveKey" };
        var loaded = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        var value = loaded["MySaveKey"].Value.GetAsString();
        Debug.Log("[TEST] Loaded value: " + value);
    }
}
