using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 8;
    public int height = 20;

    private Transform[,] grid;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        grid = new Transform[width, height];
    }

    // Call this when a tetromino lands
    public void RegisterBlock(Transform tetromino)
    {
        foreach (Transform block in tetromino)
        {
            Vector2Int pos = Vector2Int.RoundToInt(block.position);
            if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
            {
                grid[pos.x, pos.y] = block;
            }
        }

        CheckAndClearRows();
    }

    private void CheckAndClearRows()
    {
        for (int y = 0; y < height; y++)
        {
            int blockCount = 0;

            // Count all blocks in this row
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null) blockCount++;
            }

            // If 5 or more blocks exist, clear the **entire row**
            if (blockCount >= 5)
            {
                ClearRow(y);
                ShiftRowsDown(y + 1);
                y--; // check same row again after shifting
            }
        }
    }

    private void ClearRow(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] != null)
            {
                Destroy(grid[x, y].gameObject);
                grid[x, y] = null;
            }
        }
    }

    private void ShiftRowsDown(int startY)
    {
        for (int y = startY; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y - 1] = grid[x, y];
                    grid[x, y] = null;
                    grid[x, y - 1].position += Vector3.down;
                }
            }
        }
    }
}
