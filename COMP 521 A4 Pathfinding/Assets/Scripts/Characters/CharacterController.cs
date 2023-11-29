using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    public static List<CharacterController> ADVENTURERS = new List<CharacterController>();
    public static float SPEED = 2.5f;
    public static int RECALCULATE_BLOCKED_PATH_DISTANCE = 1;
    public static int MAX_HEALTH = 6;

    public Slider healthBar;

    private NavMesh navMesh;
    private Vector3 target;

    // Start is called before the first frame update
    void Start()
    {
        healthBar.value = MAX_HEALTH;
        ADVENTURERS.Add(this);
        navMesh = FindObjectOfType<NavMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage()
    {
        healthBar.value -= 1;
        if (healthBar.value == 0)
        {
            gameObject.SetActive(false);
        }
    }

    // Move minotaur from point start to end
    IEnumerator FollowPath(Partition start, Partition end)
    {
        //Debug.Log("start follow path");
        List<Partition> path = AStar.FindPath(start, end, gameObject);
        
        if (path == null)
        {
            yield break;
        }

        yield return null;

        Vector3 nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

        while (!(path.Count == 1 && Vector3.Distance(transform.position, nextPathPos) < 0.001))
        {
            HandleBlockedPath(path);
            MoveAlongPath(path);
            DrawDebugPath(path, Color.red);
            nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;
            //Debug.Log("follow path 2");
            yield return null;
        }
        //Debug.Log("end follow path");
    }

    // Recalculates path when position of target changes or path is blocked
    void HandleBlockedPath(List<Partition> curPath)
    {
        bool blocked = false;
        if (curPath != null)
        {
            for (int i = 0; i < Mathf.Min(curPath.Count, RECALCULATE_BLOCKED_PATH_DISTANCE + 1); i++)
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
                var step =  SPEED * Time.deltaTime; // calculate distance to move
                
                transform.position = Vector3.MoveTowards(transform.position, nextPathPos, step);
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

    public void SetTarget(Vector3 destination)
    {
        target = destination;
        
        StopAllCoroutines();
        StartCoroutine(FollowPath(navMesh.GetPartition(transform.position), navMesh.GetPartition(target)));
    }
}
