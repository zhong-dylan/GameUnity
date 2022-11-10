using System;
using UnityEngine;
using UnityEngine.UI;
using Framework;

public class GameBehavior : MonoBehaviour
{
    const int GRID_WIDTH = 8;
    const int GRID_HEIGHT = 8;

    public GameObject grid;
    public Cell cell;

    private ObjectPool _cellsPool;

    public void Start()
    {
        _cellsPool = new ObjectPool(cell.gameObject, 1, grid.transform);

        for(int i = 0; i < GRID_WIDTH; i++)
        {
            for (int j = 0; j < GRID_HEIGHT; j++)
            {
                _cellsPool.GetObject();
            }
        }
    }
}
