using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 worldPosition;
    public bool walkable;

    public Node(Vector3 _worldPos, bool _walkable)
    {
        worldPosition = _worldPos;
        walkable = _walkable;
    }
}
