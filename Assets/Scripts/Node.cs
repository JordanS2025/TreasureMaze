using UnityEngine;
using System.Collections.Generic;

public class Node : MonoBehaviour
{
    [HideInInspector]
    public int x; // Grid X position
    [HideInInspector]
    public int z; // Grid Z position

    public List<Node> Neighbors { get; private set; } = new List<Node>();

    /// <summary>
    /// Adds a neighbor to this node if it's not already present.
    /// </summary>
    /// <param name="neighbor">The neighboring node to add.</param>
    public void AddNeighbor(Node neighbor)
    {
        if (neighbor != null && !Neighbors.Contains(neighbor))
        {
            Neighbors.Add(neighbor);
        }
    }

    /// <summary>
    /// Clears all neighbors from this node.
    /// </summary>
    public void ClearNeighbors()
    {
        Neighbors.Clear();
    }

    // Optional: For debugging purposes, visualize neighbors in the Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var neighbor in Neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}
