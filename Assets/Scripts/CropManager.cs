using UnityEngine;
using System.Collections.Generic;

public enum TileStatus
{
    Empty,
    Planted,
    Watered
}

public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    // Stores the status of each soil tile by its grid position
    private Dictionary<Vector3Int, TileStatus> tileStates = new Dictionary<Vector3Int, TileStatus>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public TileStatus GetTileStatus(Vector3Int position)
    {
        if (tileStates.TryGetValue(position, out TileStatus status))
        {
            return status;
        }
        // Default to Empty if we know it's a soil tile but haven't tracked it yet
        // OR we could assume if it's not in dict, it's just raw soil (Empty).
        return TileStatus.Empty; 
    }

    public void SetTileStatus(Vector3Int position, TileStatus status)
    {
        if (tileStates.ContainsKey(position))
        {
            tileStates[position] = status;
        }
        else
        {
            tileStates.Add(position, status);
        }
        Debug.Log($"Tile at {position} set to {status}");
    }
}
