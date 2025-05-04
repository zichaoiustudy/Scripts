using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility functions for hex grid calculations, based on algorithms from:
/// https://www.redblobgames.com/grids/hexagons/
/// </summary>
public static class HexUtils
{
    private static FieldManager _cachedFieldManager;

    // The 6 neighboring directions in axial hex coordinates
    public static readonly Vector2Int[] HexDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0),   // East
        new Vector2Int(1, -1),  // Southeast
        new Vector2Int(0, -1),  // Southwest
        new Vector2Int(-1, 0),  // West
        new Vector2Int(-1, 1),  // Northwest
        new Vector2Int(0, 1)    // Northeast
    };

    public static List<Vector2Int> GetHexNeighbors(int q, int r)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (Vector2Int dir in HexDirections)
        {
            neighbors.Add(new Vector2Int(q + dir.x, r + dir.y));
        }

        return neighbors;
    }
    
    /// <summary>
    /// Calculate distance between two hex coordinates
    /// </summary>
    public static int HexDistance(Vector2Int a, Vector2Int b)
    {
        // Convert to cube coordinates
        int ax = a.x;
        int az = a.y;
        int ay = -ax - az;
        
        int bx = b.x;
        int bz = b.y;
        int by = -bx - bz;
        
        // Return the maximum of the absolute differences
        return Mathf.Max(
            Mathf.Abs(ax - bx),
            Mathf.Abs(ay - by),
            Mathf.Abs(az - bz)
        );
    }

    /// <summary>
    /// Convert hex coordinates to world position
    /// </summary>
    public static Vector3 HexToWorldPosition(int q, int r)
    {
        // Standard hex grid conversion 
        float x = (float)(3f/2f * q);
        float z = (float)(Mathf.Sqrt(3f)/2f * q + Mathf.Sqrt(3f) * r);
        float y = 0; // Set to the default height of hex tiles
        
        return new Vector3(x, y, z);
    }
    
    /// <summary>
    /// Convert hex coordinates to world position with height offset
    /// </summary>
    public static Vector3 HexToWorldPosition(int q, int r, float heightOffset)
    {
        Vector3 position = HexToWorldPosition(q, r);
        position.y += heightOffset;
        return position;
    }

    // Get the altitude of a hex at specified coordinates
    public static int GetHexAltitude(int q, int r, int defaultValue = 3)
    {
        // Cache the FieldController reference
        if (_cachedFieldManager == null)
        {
            _cachedFieldManager = Object.FindFirstObjectByType<FieldManager>();
        }
        
        // Use the FieldController to get the altitude
        if (_cachedFieldManager != null)
        {
            return _cachedFieldManager.GetHexAltitude(q, r, defaultValue);
        }
        
        return defaultValue;
    }
    
    // Vector2Int overload for convenience
    public static int GetHexAltitude(Vector2Int position, int defaultValue = 3)
    {
        return GetHexAltitude(position.x, position.y, defaultValue);
    }
    
    /// <summary>
    /// Find multiple possible paths considering altitude restrictions
    /// </summary>
    public static List<List<Vector2Int>> FindMultiplePathsWithRestrictions(
        Vector2Int start,
        Vector2Int end,
        ICollection<Vector2Int> obstacles = null,
        System.Func<Vector2Int, bool> isValidHex = null,
        int maxPaths = 7)
    {
        List<List<Vector2Int>> results = new List<List<Vector2Int>>();

        // Validate common parameters
        List<Vector2Int> singlePointPath;
        if (!ValidatePathParameters(start, end, ref obstacles, out singlePointPath))
        {
            if (singlePointPath != null) results.Add(singlePointPath);
            return results;
        }

        // Find optimal path to determine target distance
        List<Vector2Int> optimalPath = FindPathWithRestrictions(start, end, obstacles, isValidHex);
        if (optimalPath.Count == 0) return results;

        int targetDistance = optimalPath.Count - 1;
        results.Add(optimalPath);

        // Track paths we've already seen
        HashSet<string> visitedPaths = new HashSet<string>
        {
            PathToString(optimalPath)
        };

        // Use a priority queue to explore paths more systematically
        List<(List<Vector2Int> path, int priority)> pathQueue = new List<(List<Vector2Int>, int)>
        {
            (new List<Vector2Int> { start }, 0)
        };

        int processedPaths = 0;
        int maxProcessedPaths = 1000; // Safety limit to prevent infinite loops

        while (pathQueue.Count > 0 && results.Count < maxPaths && processedPaths < maxProcessedPaths)
        {
            // Sort paths by priority (higher value = lower priority)
            pathQueue.Sort((a, b) => b.priority.CompareTo(a.priority));

            var current = pathQueue[pathQueue.Count - 1];
            pathQueue.RemoveAt(pathQueue.Count - 1);

            List<Vector2Int> currentPath = current.path;
            Vector2Int currentPos = currentPath[currentPath.Count - 1];
            processedPaths++;

            // Skip if path is too long
            if (currentPath.Count > targetDistance + 1)
                continue;

            // If we reached the destination with the right path length
            if (currentPos == end && currentPath.Count - 1 == targetDistance)
            {
                string pathHash = PathToString(currentPath);
                if (!visitedPaths.Contains(pathHash))
                {
                    visitedPaths.Add(pathHash);

                    // Check if this path is sufficiently different from existing ones
                    bool isUniquePath = true;
                    foreach (var existingPath in results)
                    {
                        if (ArePathsSimilar(currentPath, existingPath, 0.5f))
                        {
                            isUniquePath = false;
                            break;
                        }
                    }

                    if (isUniquePath)
                        results.Add(new List<Vector2Int>(currentPath));
                }
                continue;
            }

            // Skip if we already reached the end but with wrong path length
            if (currentPos == end)
                continue;

            // Check if completing the path is still possible
            int remainingSteps = targetDistance - (currentPath.Count - 1);
            int minStepsNeeded = HexDistance(currentPos, end);

            if (minStepsNeeded > remainingSteps)
                continue;

            // Try each possible move
            foreach (Vector2Int neighbor in GetHexNeighbors(currentPos.x, currentPos.y))
            {
                // Skip if we've already been here
                if (currentPath.Contains(neighbor)) continue;

                // Skip invalid positions
                if (obstacles.Contains(neighbor)) continue;
                if (isValidHex != null && !isValidHex(neighbor)) continue;

                // Check altitude restrictions using the helper
                if (!IsValidAltitudeChange(currentPos, neighbor)) continue;

                // Create new path
                List<Vector2Int> newPath = new List<Vector2Int>(currentPath) { neighbor };

                // Priority favors paths closer to completion and closer to the goal
                int priority = (targetDistance - newPath.Count + 1) + HexDistance(neighbor, end);
                pathQueue.Add((newPath, priority));
            }
        }

        return results;
    }
    
    /// <summary>
    /// Find path considering altitude restrictions (maximum altitude difference of 1 per step)
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="end">Target position</param>
    /// <param name="obstacles">Collection of hex positions that cannot be traversed</param>
    /// <param name="isValidHex">Additional hex validation function</param>
    /// <returns>Valid path that respects altitude restrictions, or empty list if no path exists</returns>
    private static List<Vector2Int> FindPathWithRestrictions(
        Vector2Int start, 
        Vector2Int end, 
        ICollection<Vector2Int> obstacles = null,
        System.Func<Vector2Int, bool> isValidHex = null)
    {
        // Validate common parameters
        List<Vector2Int> result;
        if (!ValidatePathParameters(start, end, ref obstacles, out result))
        {
            return result ?? new List<Vector2Int>();
        }

        // Setup for A* pathfinding
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();
        var frontier = new PriorityQueue<Vector2Int>();

        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Vector2Int currentPos = frontier.Dequeue();

            // If we reached the goal, break out of the loop
            if (currentPos == end) break;

            // For each neighbor of the current hex
            foreach (Vector2Int next in GetHexNeighbors(currentPos.x, currentPos.y))
            {
                // Skip if invalid
                if (obstacles.Contains(next) || 
                    (isValidHex != null && !isValidHex(next)) ||
                    !IsValidAltitudeChange(currentPos, next))
                {
                    continue;
                }

                // Calculate new cost (each step costs 1)
                int newCost = costSoFar[currentPos] + 1;

                // If we've found a new position or a better path to an existing position
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    int priority = newCost + HexDistance(next, end);
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = currentPos;
                }
            }
        }

        // If we didn't find a path, return empty list
        if (!cameFrom.ContainsKey(end)) return new List<Vector2Int>();

        // Reconstruct the path
        return ReconstructPath(cameFrom, start, end);
    }

    private static bool ValidatePathParameters(
        Vector2Int start,
        Vector2Int end,
        ref ICollection<Vector2Int> obstacles,
        out List<Vector2Int> singlePointPath)
    {
        singlePointPath = null;

        // If start and end are the same, return just the start position
        if (start == end) 
        {
            singlePointPath = new List<Vector2Int> { start };
            return false;
        }

        // If obstacles is null, initialize an empty collection
        if (obstacles == null) obstacles = new HashSet<Vector2Int>();

        // If the target position is an obstacle, we can't reach it
        if (obstacles.Contains(end)) return false;

        return true;
    }

    private static bool IsValidAltitudeChange(Vector2Int from, Vector2Int to)
    {
        int fromAltitude = GetHexAltitude(from);
        int toAltitude = GetHexAltitude(to);
        return Mathf.Abs(fromAltitude - toAltitude) <= 1;
    }

    // Helper to reconstruct path from the cameFrom dictionary
    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom, 
        Vector2Int start, 
        Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;

        // Work backwards from the end
        path.Add(end);
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        // Reverse to get path from start to end
        path.Reverse();
        return path;
    }

    // Helper with adjustable threshold
    private static bool ArePathsSimilar(List<Vector2Int> path1, List<Vector2Int> path2, float similarityThreshold = 0.5f)
    {
        if (path1.Count != path2.Count) return false;
        
        int commonPoints = 0;
        // Skip start and end points as they'll always be the same
        for (int i = 1; i < path1.Count - 1; i++)
        {
            if (path2.Contains(path1[i]))
                commonPoints++;
        }
        
        // Calculate similarity as percentage of common intermediate points
        float similarity = path1.Count <= 2 ? 
            1.0f : // If path only has start/end, it's identical
            (float)commonPoints / (path1.Count - 2);
        
        return similarity >= similarityThreshold;
    }
    
    // Helper method to convert a path to a unique string representation
    private static string PathToString(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (Vector2Int p in path)
        {
            sb.Append($"({p.x},{p.y}),");
        }
        return sb.ToString();
    }
    
    private class PriorityQueue<T>
    {
        private List<(T item, int priority)> elements = new List<(T, int)>();
        
        public int Count => elements.Count;
        
        public void Enqueue(T item, int priority)
        {
            elements.Add((item, priority));
        }
        
        public T Dequeue()
        {
            elements.Sort((a, b) => a.priority.CompareTo(b.priority));
            var result = elements[0].item;
            elements.RemoveAt(0);
            return result;
        }
    }
}

