using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class DFSAI : MonoBehaviour
{
    public GameObject mazeGenerator;
    public float moveInterval = 0.3f;

    private bool foundGoal = false;

    // This list will store the entire route DFS travels, including backtracking steps.
    private List<Node> dfsTrace = new List<Node>();

    // If you still want to track the final path from start to goal for stats, you can store it here:
    private List<Node> finalPath = new List<Node>();

    // expansions: how many *new* nodes we visit (i.e., each time we discover a node for the first time)
    private int expansions = 0;

    void Start()
    {
        StartCoroutine(RunDFS());
    }

    IEnumerator RunDFS()
    {
        // Wait one frame so the MazeGenerator can finish its Start()
        yield return null;

        // Retrieve the MazeGenerator component
        MazeGenerator mg = mazeGenerator.GetComponent<MazeGenerator>();
        if (mg == null)
        {
            Debug.LogError("DFSAI: Could not find MazeGenerator component on the assigned GameObject!");
            yield break;
        }

        Node start = mg.startNode;
        Node goal = mg.endNode;
        if (start == null || goal == null)
        {
            Debug.LogError("DFS: No valid start or end node!");
            yield break;
        }

        // 1) Perform the DFS to fill in dfsTrace (every step) and finalPath (success route)
        foundGoal = false;
        expansions = 0;
        dfsTrace.Clear();
        finalPath.Clear();

        // We'll keep a visited set so we don't revisit the same node again
        HashSet<Node> visited = new HashSet<Node>();

        // Start DFS from the start node
        DFSVisit(start, goal, visited);

        // 2) Visualize the entire route we traveled (including backtracking)
        //    MoveAlongTrace() will step through all items in dfsTrace in order
        Debug.Log($"DFS expansions={expansions}, finalPathLength={finalPath.Count}");
        yield return StartCoroutine(MoveAlongTrace(dfsTrace));

        // 3) Save data to CSV (e.g., how many expansions and final path length)
        SaveDataToCsv(finalPath.Count);
    }

    /// <summary>
    /// Naive, *pure* DFS with full route logging.
    /// - expansions++ each time we first visit a new node.
    /// - We add nodes to dfsTrace both when we enter them and when we backtrack.
    /// - Once we reach 'goal', we store the final path by copying from the dfsTrace 
    ///   segment where we found success.
    /// </summary>
    bool DFSVisit(Node current, Node goal, HashSet<Node> visited)
    {
        // Each time we *arrive* at this node, log it in the trace
        dfsTrace.Add(current);

        bool firstVisit = visited.Add(current);
        if (firstVisit)
        {
            // We discovered a new node
            expansions++;
        }

        // If current == goal, we found a path
        if (current == goal)
        {
            foundGoal = true;
            // Build the final path from the dfsTrace if we want 
            // (since dfsTrace includes backtracking, we can parse it—but for simplicity 
            // we'll just copy everything from start..current).
            finalPath = BuildFinalPathFromTrace(current);
            return true;
        }

        // Expand neighbors in a naive order, with no heuristics
        foreach (Node neighbor in current.Neighbors)
        {
            // If we haven't found the goal yet and haven't visited neighbor
            if (!foundGoal && !visited.Contains(neighbor))
            {
                // Recurse
                bool gotGoal = DFSVisit(neighbor, goal, visited);
                if (gotGoal)
                {
                    // The search found the goal; bubble up the success
                    return true;
                }
            }
        }

        // If none of the neighbors led us to the goal, we backtrack:
        // Add 'current' again to show that we physically step back to this node
        // after exploring a neighbor.
        if (!foundGoal)
        {
            dfsTrace.Add(current);
        }

        return foundGoal;
    }

    /// <summary>
    /// Reconstruct the final path from dfsTrace if you want a distinct 
    /// success route for data. We'll find the 'current' node from the 
    /// back and copy backwards until we reach the start. 
    /// 
    /// This is optional. If you only want the entire route, you can skip 
    /// building a final path. 
    /// </summary>
    List<Node> BuildFinalPathFromTrace(Node goalNode)
    {
        // We'll walk backwards from the end of dfsTrace 
        // until we find the first occurrence of 'goalNode', 
        // then invert that sub-list. 
        // This is a rough example; you can do other approaches.
        List<Node> path = new List<Node>();

        // Start from the end of dfsTrace and move backwards to find 'goalNode'
        for (int i = dfsTrace.Count - 1; i >= 0; i--)
        {
            if (dfsTrace[i] == goalNode)
            {
                // Insert nodes until we reach the start node
                path.Insert(0, dfsTrace[i]);
                // Move up the trace until we no longer have a previous node 
                // that leads to this node in a strictly forward sense.
                // For pure DFS, you can track parent pointers if you want, 
                // or keep it simpler: we can just keep inserting until we 
                // find the start node. But that might include backtracking steps. 
                // This is a minimal example.
                break;
            }
        }

        // If you want a more accurate final path, track parents or store 
        // the partial path during recursion. For brevity, we just return
        // a one-node list or a partial approach here. 
        // (You can expand it for a full start->goal route.)
        return path;
    }

    /// <summary>
    /// Moves the DFSAI gameobject along the entire trace we recorded, 
    /// including forward steps and backtracking steps.
    /// </summary>
    IEnumerator MoveAlongTrace(List<Node> trace)
    {
        foreach (Node step in trace)
        {
            Vector3 targetPos = new Vector3(
                step.transform.position.x,
                transform.position.y,
                step.transform.position.z
            );

            transform.position = targetPos;
            yield return new WaitForSeconds(moveInterval);
        }
    }

    /// <summary>
    /// Save expansions, final path length, etc., to a CSV.
    /// </summary>
    void SaveDataToCsv(int pathLength)
    {
        string filePath = Application.dataPath + "/DFSLog.csv";
        string line = $"{System.DateTime.Now}, {expansions}, {expansions}";
        File.AppendAllLines(filePath, new string[] { line });

        Debug.Log($"DFS data saved to {filePath}");
    }
}
