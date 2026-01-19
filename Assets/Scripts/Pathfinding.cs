using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance { get; private set; }

    [Header("References")]
    public Tilemap grassTilemap; // Defines the bounds of the world
    
    [Header("Settings")]
    public LayerMask obstacleLayer; // Layer for Decor/Fences (e.g., Default or a specific Obstacle layer)
    public bool useDiagonals = false; // Farm games usually strictly usage 4-directions (Manhattan)

    // Node class for A*
    public class Node
    {
        public Vector3Int gridPosition;
        public Vector3 worldPosition;
        public bool isWalkable;
        
        public int gCost;
        public int hCost;
        public Node parent;

        public int fCost { get { return gCost + hCost; } }

        public Node(Vector3Int gridPos, Vector3 worldPos, bool walkable)
        {
            gridPosition = gridPos;
            worldPosition = worldPos;
            isWalkable = walkable;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Finds a path from startWorldPos to targetWorldPos.
    /// Returns a list of world positions to follow, or null if no path found.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
    {
        Vector3Int startCell = grassTilemap.WorldToCell(startWorldPos);
        Vector3Int targetCell = grassTilemap.WorldToCell(targetWorldPos);

        // Standard A* Implementation
        
        Node startNode = new Node(startCell, grassTilemap.GetCellCenterWorld(startCell), true);
        Node targetNode = new Node(targetCell, grassTilemap.GetCellCenterWorld(targetCell), IsWalkable(targetCell));

        if (!targetNode.isWalkable)
        {
            // If the target itself is an obstacle, maybe try to find a nearest walkable neighbor?
            // For now, let's just return null or allow clicking obstacles (and stop adjacent).
            // Usually in RTS/RPGs, if you click a wall, you walk to the wall.
            // Let's stick to strict pathfinding first.
            Debug.Log($"Hedef nokta yürünebilir değil: {targetCell}. Lütfen Pathfinding -> IsWalkable fonksiyonunu ya da Obstacle Layer ayarlarını kontrol edin.");
            return null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

        openSet.Add(startNode);

        // Since we don't cache the whole grid (inifinite/large world?), we create nodes on the fly or just use local search checking.
        // Caching the whole grid might be heavy if map is huge. 
        // Let's use a Dictionary to keep track of generated nodes to avoid duplicates in this search.
        Dictionary<Vector3Int, Node> allNodes = new Dictionary<Vector3Int, Node>();
        allNodes[startCell] = startNode;

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.gridPosition);

            if (currentNode.gridPosition == targetCell)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode, allNodes))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor.gridPosition))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null; // Path not found
    }

    private List<Node> GetNeighbors(Node node, Dictionary<Vector3Int, Node> allNodes)
    {
        List<Node> neighbors = new List<Node>();

        // 4 Directions: Up, Down, Left, Right
        Vector3Int[] directions = {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        foreach (Vector3Int dir in directions)
        {
            Vector3Int neighborPos = node.gridPosition + dir;

            // Retrieve or Create Node
            if (!allNodes.ContainsKey(neighborPos))
            {
                Vector3 worldPos = grassTilemap.GetCellCenterWorld(neighborPos);
                bool walkable = IsWalkable(neighborPos);
                allNodes[neighborPos] = new Node(neighborPos, worldPos, walkable);
            }

            neighbors.Add(allNodes[neighborPos]);
        }

        return neighbors;
    }

    private bool IsWalkable(Vector3Int cellPos)
    {
        // 1. Check if it's within map bounds (Grass check)
        if (!grassTilemap.HasTile(cellPos)) return false;

        // 2. Check for Obstacles (Colliders)
        Vector3 worldPos = grassTilemap.GetCellCenterWorld(cellPos);
        
        // Physics Check
        // Modified to detect specific obstacles and ignore the Player
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.3f, obstacleLayer);
        
        foreach (var col in colliders)
        {
            if (col.isTrigger) continue; // Ignore triggers
            if (col.CompareTag("Player")) continue; // Ignore Player

            // Found a valid obstacle
            // Debug.Log($"Obstacle detected at {cellPos}: {col.gameObject.name}");
            return false;
        }

        return true;
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        
        path.Reverse();
        return path;
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        // Manhattan Distance for 4-way movement
        int dstX = Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
        int dstY = Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);

        if (useDiagonals)
        {
            // Octile distance implementation if needed later
            if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        return 10 * (dstX + dstY);
    }
}
