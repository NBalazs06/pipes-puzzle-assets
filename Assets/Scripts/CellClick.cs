using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellClick : MonoBehaviour
{
    [SerializeField] private int rowIdx;
    [SerializeField] private int colIdx;
    [SerializeField] private Grid grid;

    void OnMouseDown()
    {
        if (!grid.currentLevelComplete)
            grid.OnCellClicked(rowIdx, colIdx);
    }
}
