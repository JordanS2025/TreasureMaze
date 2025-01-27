using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(CharacterController))]
public class AStarAI : MonoBehaviour
{
    public GameObject mazeGenerator; // Now a GameObject reference
    public float moveSpeed = 2f;
    public float moveInterval = 0.3f; // seconds between node moves

    // We'll store some stats for logging
    private int expansions = 0;
    private List<Node> finalPath;

    void Start()
    {
        // Start a coroutine so we can wait for the MazeGenerator to finish
        StartCoroutine(RunAStar());
    }

    IEnumerator RunAStar()
    {
        // Wait one frame
        yield return null;

        // 1) Get the MazeGenerator component from the assigned GameObject
        MazeGenerator mg = mazeGenerator.GetComponent<MazeGenerator>();
        if (mg == null)
        {
            Debug.LogError("AStarAI: No MazeGenerator component found on the assigned GameObject!");
            yield break;
        }

        Node start = mg.startNode;
        Node goal = mg.endNode;
        if (start == null || goal == null)
        {
            Debug.LogError("AStarAI: No valid start or end node found!");
            yield break;
        }

        // 2) Find path using A*
        finalPath = FindPathAStar(start, goal, mg);

        // 3) Move along the path, node by node, pausing
        if (finalPath != null)
        {
            Debug.Log($"AStar found path of length {finalPath.Count}, expansions={expansions}");
            yield return StartCoroutine(MoveAlongPath(finalPath));
        }
        else
        {
            Debug.Log("AStar: No path found!");
        }

        // 4) Save data to CSV
        SaveDataToCsv(finalPath != null ? finalPath.Count : 0);
    }

    /// <summary>
    /// Basic A* from start to goal. 
    /// gCost = cost from start, hCost = Manhattan distance, fCost = g+h.
    /// We pass in MazeGenerator mg so we can fetch mg.GetAllNodes().
    /// </summary>
    List<Node> FindPathAStar(Node start, Node goal, MazeGenerator mg)
    {
        expansions = 0;
        var openSet = new List<Node>();
        var cameFrom = new Dictionary<Node, Node>();
        var gScore = new Dictionary<Node, float>();
        var fScore = new Dictionary<Node, float>();

        // Initialize
        foreach (Node n in mg.GetAllNodes())
        {
            gScore[n] = float.PositiveInfinity;
            fScore[n] = float.PositiveInfinity;
        }

        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            // Get node with lowest fScore
            Node current = GetLowestFScore(openSet, fScore);
            if (current == goal)
            {
                // Reconstruct path
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            expansions++;

            foreach (Node neighbor in current.Neighbors)
            {
                float tentative_g = gScore[current] + Dist(current, neighbor);
                if (tentative_g < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_g;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // No path
        return null;
    }

    Node GetLowestFScore(List<Node> set, Dictionary<Node, float> fScore)
    {
        Node best = set[0];
        float bestF = fScore[best];
        foreach (Node n in set)
        {
            float score = fScore[n];
            if (score < bestF)
            {
                bestF = score;
                best = n;
            }
        }
        return best;
    }

    float Heuristic(Node a, Node b)
    {
        // Manhattan distance on a grid
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    float Dist(Node a, Node b)
    {
        // Treat each step as cost=1 for simplicity
        return 1f;
    }

    List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> path = new List<Node>() { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }

    IEnumerator MoveAlongPath(List<Node> path)
    {
        foreach (Node step in path)
        {
            Vector3 targetPos = new Vector3(step.transform.position.x, transform.position.y, step.transform.position.z);
            // Move instantly or you could do a smooth move
            transform.position = targetPos;

            yield return new WaitForSeconds(moveInterval);
        }
    }

    void SaveDataToCsv(int pathLength)
    {
        string filePath = Application.dataPath + "/AStarLog.csv";
        // We'll just append a line with expansions and path length
        string line = $"{System.DateTime.Now}, {pathLength}, {expansions}";
        File.AppendAllLines(filePath, new string[] { line });

        Debug.Log($"AStar data saved to {filePath}");
    }
}
