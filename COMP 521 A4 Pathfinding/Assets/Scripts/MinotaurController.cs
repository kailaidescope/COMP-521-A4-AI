using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class MinotaurController : MonoBehaviour
{
    public static float speed = 2.5f;
    public static int recalculateBlockedPathDistance = 1;

    public GameObject treasure;

    private NavMesh navMesh;
    private Vector3 target;
    private Vector3 lastTargetPosition;
    private List<Partition> path;

    // Start is called before the first frame update
    void Start()
    {
        navMesh = FindObjectOfType<NavMesh>();

        var parts = navMesh.GetPartitions();

        target = transform.position;
        path = null;

        StartCoroutine(GuardTreasure());
    }

    // Update is called once per frame
    void Update()
    {
    }

    // Make minotaur circle the treasure on a loop
    IEnumerator GuardTreasure()
    {
        Vector3 lastTreasurePos = treasure.transform.position;
        List<Partition> points = GeneratePatrolPoints(2);

        while (true)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Vector3.Distance(lastTreasurePos, treasure.transform.position) > 0.01)
                {
                    StartCoroutine(GuardTreasure());
                    yield break;
                }
                yield return StartCoroutine(FollowPath(points[i], points[(i+1)%points.Count]));
            }
        }
    }

    IEnumerator test()
    {
        for(int i = 0; i < 10; i++)
        {
            Debug.Log("Number: "+i);
            yield return new WaitForSeconds(1);;
        }
    }

    // Move minotaur from point start to end
    IEnumerator FollowPath(Partition start, Partition end)
    {
        //Debug.Log("start follow path");
        List<Partition> path = AStar.FindPath(start, end, gameObject);
        yield return null;

        Vector3 nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

        while (!(path.Count == 1 && Vector3.Distance(transform.position, nextPathPos) < 0.001))
        {
            HandleBlockedPath(path);
            MoveAlongPath(path);
            DrawDebugPath();
            nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;
            //Debug.Log("follow path 2");
            yield return null;
        }
        //Debug.Log("end follow path");
    }

    List<Partition> GeneratePatrolPoints(float distFromTreasure)
    {
        List<Partition> points = new List<Partition>();

        for (int i = -1; i < 2; i += 2)
        {
            for (int j = 1; j > -2; j -= 2)
            {
                Vector3 pos = treasure.transform.position + Vector3.forward * i * distFromTreasure + Vector3.left * j * i * distFromTreasure;
                Partition part = navMesh.GetPartition(pos);
                if (part != null)
                {
                    points.Add(part);
                }
            }
        }

        return points;
    }

    // Recalculates path when position of target changes or path is blocked
    void HandleBlockedPath()
    {
        bool blocked = false;
        if (path != null)
        {
            for (int i = 0; i < Mathf.Min(path.Count, recalculateBlockedPathDistance + 1); i++)
        {
            if (path[i].GetOccupied() != null && path[i].GetOccupied() != gameObject)
            {
                blocked = true;
            }
        }
        }
        

        if (blocked || path == null)
        {
            path = AStar.FindPath(navMesh.GetPartition(transform.position), navMesh.GetPartition(target), gameObject);
        }
    }

    // Recalculates path when position of target changes or path is blocked
    void HandleBlockedPath(List<Partition> curPath)
    {
        bool blocked = false;
        if (curPath != null)
        {
            for (int i = 0; i < Mathf.Min(curPath.Count, recalculateBlockedPathDistance + 1); i++)
        {
            if (curPath[i].GetOccupied() != null && curPath[i].GetOccupied() != gameObject)
            {
                blocked = true;
            }
        }
        }
        

        if (blocked || curPath == null)
        {
            curPath = AStar.FindPath(navMesh.GetPartition(transform.position), navMesh.GetPartition(target), gameObject);
        }
    }

    // Handles movement towards target
    void MoveToTarget()
    {
        if (path != null && path.Count > 0)
        {
            Vector3 nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

            // Check if the position of the human and the next point on the path are approximately equal.
            if (Vector3.Distance(transform.position, nextPathPos) < 0.001f)
            {
                transform.position = nextPathPos;
                path.RemoveAt(0);
            }

            if (path.Count > 0)
            {
                // Move position a step closer to the target.
                var step =  speed * Time.deltaTime; // calculate distance to move
                
                transform.position = Vector3.MoveTowards(transform.position, nextPathPos, step);
            }
        }
    }

    // Handles movement towards target
    void MoveAlongPath(List<Partition> curPath)
    {
        if (curPath != null && curPath.Count > 0)
        {
            Vector3 nextPathPos = curPath[0].GetPosition() - curPath[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

            // Check if the position of the human and the next point on the path are approximately equal.
            if (Vector3.Distance(transform.position, nextPathPos) < 0.001f)
            {
                transform.position = nextPathPos;
                curPath.RemoveAt(0);
            }

            if (curPath.Count > 0)
            {
                // Move position a step closer to the target.
                var step =  speed * Time.deltaTime; // calculate distance to move
                
                transform.position = Vector3.MoveTowards(transform.position, nextPathPos, step);
            }
        }
    }

    // Draws path in debug window
    void DrawDebugPath()
    {
        if (path != null) 
        {
            for(int i = 0; i < path.Count-1; i++)
            {
                Debug.DrawLine(path[i].GetPosition(), path[i+1].GetPosition(), Color.red);
            }
        }
    }

    // Draws path in scene view
    void DrawDebugPath(List<Partition> curPath, Color color)
    {
        if (curPath != null) 
        {
            for(int i = 0; i < curPath.Count-1; i++)
            {
                Debug.DrawLine(curPath[i].GetPosition(), curPath[i+1].GetPosition(), color);
            }
        }
    }

    public List<Partition> GetPath()
    {
        return path;
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
        path = null;
    }
}
