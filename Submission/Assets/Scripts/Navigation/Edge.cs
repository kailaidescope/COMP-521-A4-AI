using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge
{
    public Partition a;
    public Partition b;
    public float distance;

    public Edge(Partition p_a, Partition p_b)
    {
        a = p_a;
        b = p_b;
        distance = (a.GetPosition() - b.GetPosition()).magnitude;
    }

    public override string ToString()
    {
        return "Edge from "+a+" to "+b;
    }
}
