using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellController : MonoBehaviour
{
    // shared across the entire controller class
    static CellController selectedCell;
    SpriteRenderer spriteRenderer;
    // logial indices into GameManager.grid[row, col]
    public Vector2Int indices;
    public Sprite highlightSprite;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Select() {
        spriteRenderer.color = Color.grey;
    }

    public void Clear() {
        spriteRenderer.color = Color.white;
    }

    public void Highlight() {
        spriteRenderer.color = Color.red;
    }

    bool IsHighlighted() {
        return spriteRenderer.color == Color.red;
    }

    void OnMouseDown() {
        // start the game once a cell is clicked
        if (selectedCell == null) { // nothing selected yet
            // if nothing on this cell, ignore
            if (spriteRenderer.sprite != null && !IsHighlighted()
            && spriteRenderer.sprite != highlightSprite) {
                selectedCell = this;
                Select();
                SoundManager.Instance.PlaySound(SoundType.TypeSelect);
            }
            return;
        }
        if (selectedCell == this) { // deselect
            selectedCell = null;
            Clear();
        } else { // try moving from selectedCell to this
            bool hasMoved = GameManager.Instance.TryMoveCell(selectedCell.indices, indices);
            if (hasMoved) { // deselect
                selectedCell = null;
            }
            // else retains selection
        }
    }

}
