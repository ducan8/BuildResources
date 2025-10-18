using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public Transform player;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    Node[,] grid;

    public List<Node> path;    

    float nodeDiameter;

    int gridSizeX, gridSizeY;

    void Start()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // tính toán toạ độ thế giới của node hiện tại dựa trên vị trí góc dưới bên trái của lưới và vị trí của node trong lưới
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                grid[x, y] = new Node(worldPoint, walkable, x, y);
            }
        }
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                int checkX = node.gridX + x;   
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        // vì sao lại cộng gridWorldSize.x / 2:  vì toạ độ worldPosition.x là toạ độ tuyệt đối trong thế giới, còn gridWorldSize.x là kích thước của lưới. Để chuyển đổi toạ độ từ hệ toạ độ thế giới sang hệ toạ độ lưới, ta cần dịch chuyển toạ độ gốc của lưới về phía trái (âm) một nửa kích thước của lưới. Điều này giúp ta xác định vị trí tương đối của điểm trong lưới. -> tọa độ lưới đang (0, 0) sẽ trở thành (-gridWorldSize.x / 2, -gridWorldSize.y / 2) trong hệ toạ độ thế giới.
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x; 
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        // vì sao lại là gridSizeX - 1: vì chỉ số mảng bắt đầu từ 0, nên phần tử cuối cùng sẽ có chỉ số là gridSizeX - 1.
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX); 
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y)); 
        if (grid != null)
        {
            Node playerNode = NodeFromWorldPoint(player.position);
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                if(playerNode == n)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }

            if (path != null)
            {
                foreach (Node n in path)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }

    }
}
