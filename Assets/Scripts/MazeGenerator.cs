using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Configuration")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f; // Distance between grid cells

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject treasurePrefab;
    public GameObject nodePrefab;

    // Maze wall arrays (1 = wall, 0 = no wall)
    private int[,] verticalWalls;    // size: (gridWidth+1, gridHeight)
    private int[,] horizontalWalls;  // size: (gridWidth, gridHeight+1)

    // 2D array of Nodes
    public Node[,] nodeGrid;

    // Expose these for AI references
    [HideInInspector] public Node startNode;
    [HideInInspector] public Node endNode;   // The node where treasure is placed

    void Start()
    {
        nodeGrid = new Node[gridWidth, gridHeight];
        InitializeGrids();

        // 1) Randomly remove walls (but preserve perimeter)
        GenerateMaze();

        // 2) Create nodes (one per accessible cell)
        CreateNodes();

        // 3) Assign neighbors according to missing walls
        AssignNeighbors();

        // 4) Ensure the entire node graph is connected
        EnsureNodesConnected();

        // 5) Reassign neighbors (in case more walls were removed)
        foreach (Node node in GetAllNodes()) node.ClearNeighbors();
        AssignNeighbors();

        // 6) Now instantiate the final walls
        BuildMaze();

        // 7) Pick a start node (for this demo, we just pick (0,0) if accessible)
        //    If it's not accessible, find the first accessible cell in the grid.
        AssignStartNode();

        // 8) Place the treasure in a valid cell, store that node in endNode
        PlaceTreasure();
    }

    // -----------------------------------------------------------
    // Initialization / Maze Generation
    // -----------------------------------------------------------
    void InitializeGrids()
    {
        verticalWalls = new int[gridWidth + 1, gridHeight];
        horizontalWalls = new int[gridWidth, gridHeight + 1];

        // Mark all walls as present
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                verticalWalls[x, z] = 1;
            }
        }
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z <= gridHeight; z++)
            {
                horizontalWalls[x, z] = 1;
            }
        }
    }

    void GenerateMaze()
    {
        // Random removal of interior walls
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Skip perimeter
                if (x == -1 || z == -1 || x == gridWidth || z == gridHeight)
                    continue;

                if (Random.value < 0.5f)
                    verticalWalls[x, z] = 0;
                else
                    horizontalWalls[x, z] = 0;
            }
        }
    }

    // -----------------------------------------------------------
    // Node Creation & Connectivity
    // -----------------------------------------------------------
    void CreateNodes()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (IsCellAccessible(x, z))
                {
                    float worldX = (x + 0.5f) * cellSize;
                    float worldZ = (z + 0.5f) * cellSize;
                    Vector3 pos = new Vector3(worldX, 0.5f, worldZ);

                    GameObject nodeObj = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
                    Node node = nodeObj.GetComponent<Node>();
                    if (node != null)
                    {
                        node.x = x;
                        node.z = z;
                        nodeGrid[x, z] = node;
                    }
                }
            }
        }
    }

    bool IsCellAccessible(int x, int z)
    {
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight)
            return false;

        bool isAccessible = false;
        // If there's no vertical wall on the left
        if (x > 0 && verticalWalls[x, z] == 0) isAccessible = true;
        // If there's no vertical wall on the right
        if (x < gridWidth - 1 && verticalWalls[x + 1, z] == 0) isAccessible = true;
        // If there's no horizontal wall below
        if (z > 0 && horizontalWalls[x, z] == 0) isAccessible = true;
        // If there's no horizontal wall above
        if (z < gridHeight - 1 && horizontalWalls[x, z + 1] == 0) isAccessible = true;

        return isAccessible;
    }

    void AssignNeighbors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Node current = nodeGrid[x, z];
                if (current == null) continue;

                // Up
                if (z < gridHeight - 1 && horizontalWalls[x, z + 1] == 0 && nodeGrid[x, z + 1] != null)
                {
                    current.AddNeighbor(nodeGrid[x, z + 1]);
                }
                // Down
                if (z > 0 && horizontalWalls[x, z] == 0 && nodeGrid[x, z - 1] != null)
                {
                    current.AddNeighbor(nodeGrid[x, z - 1]);
                }
                // Right
                if (x < gridWidth - 1 && verticalWalls[x + 1, z] == 0 && nodeGrid[x + 1, z] != null)
                {
                    current.AddNeighbor(nodeGrid[x + 1, z]);
                }
                // Left
                if (x > 0 && verticalWalls[x, z] == 0 && nodeGrid[x - 1, z] != null)
                {
                    current.AddNeighbor(nodeGrid[x - 1, z]);
                }
            }
        }
    }

    // -----------------------------------------------------------
    // Ensure the node graph is fully connected
    // -----------------------------------------------------------
    void EnsureNodesConnected()
    {
        List<List<Node>> subgraphs = GetAllSubgraphs();
        if (subgraphs.Count <= 1) return; // already fully connected

        while (subgraphs.Count > 1)
        {
            // Merge subgraphs[0] with subgraphs[1..n]
            for (int i = 1; i < subgraphs.Count; i++)
            {
                ConnectSubgraphs(subgraphs[0], subgraphs[i]);
            }

            // Reassign neighbors so the new walls are recognized
            foreach (Node n in GetAllNodes()) n.ClearNeighbors();
            AssignNeighbors();

            // Re-evaluate subgraphs
            subgraphs = GetAllSubgraphs();
        }
    }

    List<List<Node>> GetAllSubgraphs()
    {
        List<List<Node>> subgraphs = new List<List<Node>>();
        HashSet<Node> visited = new HashSet<Node>();

        foreach (Node n in GetAllNodes())
        {
            if (n == null || visited.Contains(n)) continue;

            // BFS
            List<Node> subgraph = new List<Node>();
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(n);
            visited.Add(n);

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                subgraph.Add(current);

                foreach (Node neighbor in current.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            subgraphs.Add(subgraph);
        }

        return subgraphs;
    }

    void ConnectSubgraphs(List<Node> subgraphA, List<Node> subgraphB)
    {
        foreach (Node nodeB in subgraphB)
        {
            int xB = nodeB.x;
            int zB = nodeB.z;

            // Up
            if (zB < gridHeight - 1 && horizontalWalls[xB, zB + 1] == 1)
            {
                Node maybeUp = nodeGrid[xB, zB + 1];
                if (maybeUp != null && subgraphA.Contains(maybeUp))
                {
                    horizontalWalls[xB, zB + 1] = 0;
                    return;
                }
            }
            // Down
            if (zB > 0 && horizontalWalls[xB, zB] == 1)
            {
                Node maybeDown = nodeGrid[xB, zB - 1];
                if (maybeDown != null && subgraphA.Contains(maybeDown))
                {
                    horizontalWalls[xB, zB] = 0;
                    return;
                }
            }
            // Right
            if (xB < gridWidth - 1 && verticalWalls[xB + 1, zB] == 1)
            {
                Node maybeRight = nodeGrid[xB + 1, zB];
                if (maybeRight != null && subgraphA.Contains(maybeRight))
                {
                    verticalWalls[xB + 1, zB] = 0;
                    return;
                }
            }
            // Left
            if (xB > 0 && verticalWalls[xB, zB] == 1)
            {
                Node maybeLeft = nodeGrid[xB - 1, zB];
                if (maybeLeft != null && subgraphA.Contains(maybeLeft))
                {
                    verticalWalls[xB, zB] = 0;
                    return;
                }
            }
        }
    }

    public IEnumerable<Node> GetAllNodes()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (nodeGrid[x, z] != null)
                {
                    yield return nodeGrid[x, z];
                }
            }
        }
    }

    // -----------------------------------------------------------
    // Final Maze Construction / Start/Treasure
    // -----------------------------------------------------------
    void BuildMaze()
    {
        // Vertical walls
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (verticalWalls[x, z] == 1)
                {
                    float worldX = x * cellSize;
                    float worldZ = (z + 0.5f) * cellSize;
                    Vector3 position = new Vector3(worldX, 0f, worldZ);
                    Instantiate(wallPrefab, position, Quaternion.identity);
                }
            }
        }
        // Horizontal walls
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z <= gridHeight; z++)
            {
                if (horizontalWalls[x, z] == 1)
                {
                    float worldX = (x + 0.5f) * cellSize;
                    float worldZ = z * cellSize;
                    Vector3 position = new Vector3(worldX, 0f, worldZ);
                    Quaternion rotation = Quaternion.Euler(0, 90, 0);
                    Instantiate(wallPrefab, position, rotation);
                }
            }
        }
    }

    // For demo: pick (0,0) if accessible, otherwise pick the first accessible cell
    void AssignStartNode()
    {
        // Try (0,0) first
        if (nodeGrid[0, 0] != null)
        {
            startNode = nodeGrid[0, 0];
            Debug.Log("Assigned start node at (0,0)");
            return;
        }

        // Otherwise find any accessible node
        foreach (Node node in GetAllNodes())
        {
            startNode = node;
            Debug.Log($"Assigned start node at ({node.x},{node.z})");
            return;
        }
    }

    void PlaceTreasure()
    {
        Vector2Int treasurePos;
        while (true)
        {
            treasurePos = new Vector2Int(
                Random.Range(0, gridWidth),
                Random.Range(0, gridHeight)
            );
            if (IsCellAccessible(treasurePos.x, treasurePos.y))
                break;
        }

        float tx = (treasurePos.x + 0.5f) * cellSize;
        float tz = (treasurePos.y + 0.5f) * cellSize;
        Vector3 posT = new Vector3(tx, 0.5f, tz);
        GameObject tObj = Instantiate(treasurePrefab, posT, Quaternion.identity, transform);

        // Mark the node that has the treasure
        endNode = nodeGrid[treasurePos.x, treasurePos.y];
        Debug.Log($"Treasure node at ({treasurePos.x},{treasurePos.y})");
    }
}
