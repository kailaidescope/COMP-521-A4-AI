using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Partition : MonoBehaviour
{
    UnityEngine.GameObject occupied = null;
    List<Edge> edges = new List<Edge>();

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public List<Edge> GetEdges()
    {
        return edges;
    }

    public float GetDistanceToConnectedPartition(Partition p)
    {
        if (p == this) { return 0; }

        foreach (Edge e in edges)
        {
            if (e.a == p || e.b == p)
            {
                return e.distance;
            }
        }

        return -1;
    }

    public UnityEngine.GameObject GetOccupied() { return occupied; }
    public Partition SetOccupied(UnityEngine.GameObject g) { occupied = g; return this; }

    public List<Partition> GetConnectedPartitions()
    {
        List<Partition> connectedParts = new List<Partition>();

        foreach(Edge e in edges)
        {
            connectedParts.Add((e.a == this)? e.b : e.a);
        }

        return connectedParts;
    }

    public float GetDistanceToPartition(Partition part)
    {
        foreach(Edge e in edges)
        {
            if (e.a == part || e.b == part)
            {
                return e.distance;
            }
        }

        throw new Exception("Partition not connected");
    }

    public void DrawWithEdges(Color partColor, Color edgeColor)
    {
        float[] xs = {GetPosition().x-0.5f, GetPosition().x+0.5f};
        float[] zs = {GetPosition().z-0.5f, GetPosition().z+0.5f};
        
        Debug.DrawLine(new Vector3(xs[0], 0, zs[0]), new Vector3(xs[0], 0, zs[1]), partColor, 100f);
        Debug.DrawLine(new Vector3(xs[0], 0, zs[1]), new Vector3(xs[1], 0, zs[1]), partColor, 100f);
        Debug.DrawLine(new Vector3(xs[1], 0, zs[0]), new Vector3(xs[0], 0, zs[0]), partColor, 100f);
        Debug.DrawLine(new Vector3(xs[1], 0, zs[1]), new Vector3(xs[1], 0, zs[0]), partColor, 100f);

        foreach (Edge e in edges)
        {
            if(e.distance > 0)
            {
                if (e.a == this)
                {
                    if (e.b.GetPosition().x < GetPosition().x || e.b.GetPosition().y < GetPosition().y)
                    {
                        Debug.DrawLine(e.b.GetPosition(), e.a.GetPosition(), edgeColor, 100f);
                    }
                } else 
                {
                    if (e.a.GetPosition().x < GetPosition().x || e.a.GetPosition().y < GetPosition().y)
                    {
                        Debug.DrawLine(e.b.GetPosition(), e.a.GetPosition(), edgeColor, 100f);
                    }
                }
            }
        }
    }
    
    public void Draw(Color partColor)
    {
        float[] xs = {GetPosition().x-0.5f, GetPosition().x+0.5f};
        float[] zs = {GetPosition().z-0.5f, GetPosition().z+0.5f};
        
        Debug.DrawLine(new Vector3(xs[0], 0, zs[0]), new Vector3(xs[0], 0, zs[1]), partColor);
        Debug.DrawLine(new Vector3(xs[0], 0, zs[1]), new Vector3(xs[1], 0, zs[1]), partColor);
        Debug.DrawLine(new Vector3(xs[1], 0, zs[0]), new Vector3(xs[0], 0, zs[0]), partColor);
        Debug.DrawLine(new Vector3(xs[1], 0, zs[1]), new Vector3(xs[1], 0, zs[0]), partColor);
    }

    public override string ToString()
    {
        return "Partition at "+GetPosition().ToString();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (occupied == null)
        {
            occupied = collider.gameObject;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (occupied != null && collider.gameObject == occupied)
        {
            occupied = null;
        }
    }
    
    void OnTriggerStay(Collider collider)
    {
        if (occupied == null)
        {
            occupied = collider.gameObject;
        }
    }
}
