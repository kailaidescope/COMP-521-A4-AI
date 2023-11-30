using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Types;
using Utils;

public class AStar
{
    static readonly float sqrt2 = (float)Math.Sqrt(2);

    // A* algorithm:
    // Searches for shortest path from start to goal
    public static List<Partition> FindPath(Partition start, Partition goal)
    {
        Dictionary<Partition, float> gValues = new Dictionary<Partition, float>();
        Dictionary<Partition, Partition> cameFrom = new Dictionary<Partition, Partition>();

        PriorityQueue<Partition, Partition> openSet = new PriorityQueue<Partition, Partition>(new PathQueueComparer(gValues, goal));
        openSet.Enqueue(start, start);

        gValues.Add(start, 0);

        int i = 0;

        while (openSet.Count > 0)
        {
            i++;
            //Debug.Log(i);
            Partition current = openSet.Dequeue();

            if (current.Equals(goal))
            {
                //Debug.Log("Path found!");
                return ReconstructPath(cameFrom, current);
            }

            //Debug.Log(current.GetConnectedPartitions().Count + ", " + current.GetEdges().Count);
            foreach (Partition neighbor in current.GetConnectedPartitions())
            {
                float tempGValue = gValues.GetValueOrDefault(current, -1) + neighbor.GetDistanceToPartition(current);

                if (tempGValue < gValues.GetValueOrDefault(neighbor, -1) || -1 == gValues.GetValueOrDefault(neighbor, -1))
                {
                    ReplaceOrAddValue(gValues, neighbor, tempGValue);
                    ReplaceOrAddValue(cameFrom, neighbor, current);
                    
                    //Debug.Log(openSet.ContainsKey(neighbor));
                    if (!openSet.ContainsKey(neighbor))
                    {
                        openSet.Enqueue(neighbor, neighbor);
                    }
                }
            }
        }

        throw new Exception("Path unable to be found");
    }

    // A* algorithm:
    // Searches for shortest path from start to goal, 
    // allows traversal through spaces occupied by the seeker
    public static List<Partition> FindPath(Partition start, Partition goal, UnityEngine.GameObject seeker)
    {
        Dictionary<Partition, float> gValues = new Dictionary<Partition, float>();
        Dictionary<Partition, Partition> cameFrom = new Dictionary<Partition, Partition>();

        PriorityQueue<Partition, Partition> openSet = new PriorityQueue<Partition, Partition>(new PathQueueComparer(gValues, goal));
        openSet.Enqueue(start, start);

        gValues.Add(start, 0);

        int i = 0;

        while (openSet.Count > 0)
        {
            i++;
            //Debug.Log(i);
            Partition current = openSet.Dequeue();

            if (current.Equals(goal))
            {
                //Debug.Log("Path found!");
                return ReconstructPath(cameFrom, current);
            }

            //Debug.Log(current.GetConnectedPartitions().Count + ", " + current.GetEdges().Count);
            foreach (Partition neighbor in current.GetConnectedPartitions())
            {
                if (neighbor.GetOccupied() == null || neighbor.GetOccupied() == seeker)
                {
                    float tempGValue = gValues.GetValueOrDefault(current, -1) + neighbor.GetDistanceToPartition(current);

                    if (tempGValue < gValues.GetValueOrDefault(neighbor, -1) || -1 == gValues.GetValueOrDefault(neighbor, -1))
                    {
                        ReplaceOrAddValue(gValues, neighbor, tempGValue);
                        ReplaceOrAddValue(cameFrom, neighbor, current);
                        
                        //Debug.Log(openSet.ContainsKey(neighbor));
                        if (!openSet.ContainsKey(neighbor))
                        {
                            openSet.Enqueue(neighbor, neighbor);
                        }
                    }
                }
            }
        }

        //throw new Exception("Path unable to be found");
        return null;
    }

    // A* algorithm:
    // Searches for shortest path from start to goal
    // Allows for traverals through spaces occupied by ignoreObjects
    public static List<Partition> FindPath(Partition start, Partition goal, List<UnityEngine.GameObject> ignoreObjects)
    {
        Dictionary<Partition, float> gValues = new Dictionary<Partition, float>();
        Dictionary<Partition, Partition> cameFrom = new Dictionary<Partition, Partition>();

        PriorityQueue<Partition, Partition> openSet = new PriorityQueue<Partition, Partition>(new PathQueueComparer(gValues, goal));
        openSet.Enqueue(start, start);

        gValues.Add(start, 0);

        int i = 0;

        while (openSet.Count > 0)
        {
            i++;
            //Debug.Log(i);
            Partition current = openSet.Dequeue();

            if (current.Equals(goal))
            {
                //Debug.Log("Path found!");
                return ReconstructPath(cameFrom, current);
            }

            //Debug.Log(current.GetConnectedPartitions().Count + ", " + current.GetEdges().Count);
            foreach (Partition neighbor in current.GetConnectedPartitions())
            {
                if (neighbor.GetOccupied() == null || ignoreObjects.Contains(neighbor.GetOccupied()))
                {
                    float tempGValue = gValues.GetValueOrDefault(current, -1) + neighbor.GetDistanceToPartition(current);

                    if (tempGValue < gValues.GetValueOrDefault(neighbor, -1) || -1 == gValues.GetValueOrDefault(neighbor, -1))
                    {
                        ReplaceOrAddValue(gValues, neighbor, tempGValue);
                        ReplaceOrAddValue(cameFrom, neighbor, current);
                        
                        //Debug.Log(openSet.ContainsKey(neighbor));
                        if (!openSet.ContainsKey(neighbor))
                        {
                            openSet.Enqueue(neighbor, neighbor);
                        }
                    }      
                }
            }
        }

        //throw new Exception("Path unable to be found");
        return null;
    }

    // Turns path created by FindPath into a list of partitions
    private static List<Partition> ReconstructPath(Dictionary<Partition, Partition> cameFrom, Partition current)
    {
        List<Partition> path = new List<Partition>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom.GetValueOrDefault(current, null);
            path.Insert(0, current);
        }

        return path;
    }

    public static float FindPathDistance(Partition start, Partition goal, List<GameObject> ignoreObjects)
    {
        List<Partition> path = AStar.FindPath(start, goal, ignoreObjects);

        if (path == null)
        {
            return -1;
        } else
        {
            float dist = 0;

            for (int i = 0; i < path.Count-1; i++)
            {
                dist += path[i].GetDistanceToConnectedPartition(path[i+1]);
            }

            return dist;
        }
    }

    // Calculates heuristic distance between two points on an octile grid
    public static float OctileHeuristic(Partition current, Partition goal)
    {
        float deltaX = Math.Abs(current.GetPosition().x - goal.GetPosition().x);
        float deltaY = Math.Abs(current.GetPosition().z - goal.GetPosition().z);
        return sqrt2 * Math.Min(deltaY,deltaX) + Math.Abs(deltaY - deltaX);
    }

    public static void ReplaceOrAddValue<T,V>(Dictionary<T,V> dict, T key, V value)
    {
        if(dict.ContainsKey(key))
        {
            dict.Remove(key);
            
        } 
        dict.Add(key,value);
    }

    public class PathQueueComparer : IComparer<Partition>
    {
        Dictionary<Partition, float> gValues;
        Partition goal;

        public PathQueueComparer(Dictionary<Partition, float> dict, Partition goal)
        {
            gValues = dict;
            this.goal = goal;
        }

        public int Compare(Partition x, Partition y)
        {
            float g_x = -1;
            float g_y = -1;

            if (gValues.ContainsKey(x))
            {
                gValues.TryGetValue(x, out float g);
                g_x = g;
            }
            if (gValues.ContainsKey(y))
            {
                gValues.TryGetValue(y, out float g);
                g_y = g;
            }

            float f_x = g_x + AStar.OctileHeuristic(x, goal);
            float f_y = g_y + AStar.OctileHeuristic(y, goal);
            
            if (f_x == f_y)
                return 0;
            else if(f_x < f_y)
                return -1;
            else
                return 1;  
        }
    }
}
