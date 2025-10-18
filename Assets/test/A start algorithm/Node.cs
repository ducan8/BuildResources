using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 worldPosition;
    public bool walkable;
    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;

    public Node parent;

    public Node(Vector3 _worldPos, bool _walkable, int _gridX, int _gridY)
    {
        worldPosition = _worldPos;
        walkable = _walkable;
        gridX = _gridX;
        gridY = _gridY;
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
}
