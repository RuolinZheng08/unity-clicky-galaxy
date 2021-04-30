using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellController : MonoBehaviour
{
    // shared across the entire controller class
    private static CellController selectedCell;
    private SpriteRenderer renderer;
    // logial indices into GameManager.grid[row, col]
    public Vector2Int indices;

    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    public void Select() {
        renderer.color = Color.grey;
    }

    public void Deselect() {
        renderer.color = Color.white;
    }

    void OnMouseDown() {
        if (selectedCell == null) { // nothing selected yet
            selectedCell = this;
            Select();
            return;
        }
        if (selectedCell == this) { // deselect
            selectedCell = null;
            Deselect();
        } else { // try moving from selectedCell to this
            bool hasMoved = GameManager.Instance.TryMoveCell(selectedCell.indices, indices);
            if (hasMoved) { // deselect
                selectedCell = null;
                Deselect();
            }
            // else retains selection
        }
    }

}
