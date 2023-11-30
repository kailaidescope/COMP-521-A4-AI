using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;

public class NavMesh : MonoBehaviour
{
    public UnityEngine.GameObject partitionPrefab;
    public Vector2[] referencePlaneCorners = new Vector2[2];
    [SerializeField] float height;

    private List<List<Partition>> partitions;
    private List<Partition> corners;
    float xStorageOffset;
    float zStorageOffset;

    // Start is called before the first frame update
    void Awake()
    {
        GenerateNavMesh();

        foreach (List<Partition> ps in partitions)
        {
            foreach (Partition p in ps)
            {
                //p.Draw(Color.yellow, Color.red);
            }
        }

        Partition start = partitions[0][0]; 
        Partition end = partitions[partitions.Count-1][partitions[partitions.Count-1].Count-1]; 

        var path = AStar.FindPath(start, end);
        
        for(int i = 0; i < path.Count-1; i++)
        {
            Debug.DrawLine(new Vector3(path[i].GetPosition().x, 0, path[i].GetPosition().y), 
                        new Vector3(path[i+1].GetPosition().x, 0, path[i+1].GetPosition().y), Color.red, 1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //CaclulateAndDrawDummyPath();
        DrawOccupiedPartitions();
    }

    void GenerateNavMesh()
    {
        partitions = new List<List<Partition>>();
        corners = new List<Partition>();

        float minx = Mathf.Min(referencePlaneCorners[0].x, referencePlaneCorners[1].x);
        float maxx = Mathf.Max(referencePlaneCorners[0].x, referencePlaneCorners[1].x) - 1;
        float minz = Mathf.Min(referencePlaneCorners[0].y, referencePlaneCorners[1].y);
        float maxz = Mathf.Max(referencePlaneCorners[0].y, referencePlaneCorners[1].y) - 1;

        xStorageOffset = -minx - 0.5f;
        zStorageOffset = -minz - 0.5f;

        for (float x = minx; x <= maxx; x++)
        {
            partitions.Add(new List<Partition>());

            for (float z = minz; z <= maxz; z++)
            {
                //Debug.Log("("+x+","+z+")");
                Partition p = Instantiate(partitionPrefab, gameObject.transform).GetComponent<Partition>();
                p.transform.position = new Vector3(x+0.5f, 0f, z+0.5f);

                partitions[(int)(p.GetPosition().x + xStorageOffset)].Add(p);
                //Debug.Log(partitions[(int)(p.GetPosition().x + xStorageOffset)][(int)(p.GetPosition().z + zStorageOffset)]);

/*                 int storagex = (int)(p.GetPosition().x + xStorageOffset);
                int storagey = (int)(p.GetPosition().y + yStorageOffset);

                Debug.Log("[" + storagex + ", " + storagey
                            + "]: " + partitions[storagex][storagey]); */
                
                // Add edges to existing nodes
                if (x > minx)
                {
                    // Connect to left node
                    AddEdge(p, GetPartition(new Vector3(x-0.5f, 0f, z+0.5f)));

                    if (z < maxz)
                    {
                        // Connect to upper-left node
                        AddEdge(p, GetPartition(new Vector3(x-0.5f, 0f, z+1.5f)));
                    }
                    if(z > minz)
                    {
                        // Connect to lower-left node
                        AddEdge(p, GetPartition(new Vector3(x-0.5f, 0f, z-0.5f)));
                    }
                }
                if (z > minz)
                {
                    // Connect to lower node
                    AddEdge(p, GetPartition(new Vector3(x+0.5f, 0f, z-0.5f)));
                }
            }
        }

        corners.Add(partitions[partitions.Count-1][partitions[partitions.Count-1].Count-1]);
        corners.Add(partitions[partitions.Count-1][0]);
        corners.Add(partitions[0][partitions[0].Count-1]);
        corners.Add(partitions[0][0]);
    }

    void DrawOccupiedPartitions()
    {
        foreach (List<Partition> parts in partitions)
        {
            foreach (Partition p in parts)
            {
                if (p.GetOccupied() != null)
                {
                    p.Draw(Color.yellow);
                }
            }
        }
    }

    // Create an edge between two nodes in the navmesh
    void AddEdge(Partition a, Partition b)
    {
        Edge edge = new Edge(a,b);
        a.GetEdges().Add(edge);
        b.GetEdges().Add(edge);
    }

    public Partition GetPartition(Vector3 v)
    {
        float x = Mathf.Floor(v.x) + 0.5f;
        float z = Mathf.Floor(v.z) + 0.5f;

        //Debug.Log("("+v.x+","+v.z+"), "+"("+x+","+z+")");
        return partitions[(int)(x+xStorageOffset)][(int)(z+zStorageOffset)];
    }

    public List<List<Partition>> GetPartitions()
    {
        return partitions;
    }

    public List<Partition> GetCorners()
    {
        return corners;
    }

    // Simple breadth-first search to find nearest unoccupied partition
    // Does not count ignoreObjects elements as occupying space
    public Partition GetNearestUnoccupiedPartition(Partition start, List<GameObject> ignoreObjects)
    {
        List<Partition> searchedParts = new List<Partition>();
        Queue<Partition> partitionsToSearch = new Queue<Partition>();
        partitionsToSearch.Enqueue(start);
        Partition currentPart;

        while (partitionsToSearch.Count > 0)
        {
            currentPart = partitionsToSearch.Dequeue();

            if (currentPart.GetOccupied() == null || ignoreObjects.Contains(currentPart.GetOccupied()))
            {
                return currentPart;
            }

            List<Partition> adjacentParts = currentPart.GetConnectedPartitions();

            foreach(Partition p in adjacentParts)
            {
                partitionsToSearch.Enqueue(p);
            }

            searchedParts.Add(currentPart);
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        float[] xs = {referencePlaneCorners[0].x, referencePlaneCorners[1].x};
        float[] ys = {referencePlaneCorners[0].y, referencePlaneCorners[1].y};
        
        Gizmos.DrawLine(new Vector3(xs[0], height, ys[0]), new Vector3(xs[0], height, ys[1]));
        Gizmos.DrawLine(new Vector3(xs[0], height, ys[1]), new Vector3(xs[1], height, ys[1]));
        Gizmos.DrawLine(new Vector3(xs[1], height, ys[1]), new Vector3(xs[1], height, ys[0]));
        Gizmos.DrawLine(new Vector3(xs[1], height, ys[0]), new Vector3(xs[0], height, ys[0]));
    }
}
