using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    int gridDimension = 8;
    float pixelScale = 1;
    float randomValueThreshold = 0.8f; // fill only 20% of the cells on startup

    public List<Sprite> sprites;
    public GameObject cellPrefab;

    // logical representation of cells, [0, 0] is at bottom-left
    // grows upward and to the right
    GameObject[,] grid;

    // GUI

    // Start is called before the first frame update
    void Start() {
        grid = new GameObject[gridDimension, gridDimension];
        InitGrid();
    }

    void InitGrid() {
        float center = gridDimension * pixelScale / 2;
        Vector3 gridCenter = new Vector3(center, center, 0);
        Vector3 gridOffset = transform.position - gridCenter;

        for (int row = 0; row < gridDimension; row++) {
            for (int col = 0; col < gridDimension; col++) {
                GameObject cell = Instantiate(cellPrefab);
                // set parent to be the grid
                cell.transform.parent = transform;
                // set position to draw
                Vector3 cellPosition = new Vector3(col * pixelScale, row * pixelScale, 0);
                Vector3 cellPositionOffseted = cellPosition + gridOffset;
                cell.transform.position = cellPositionOffseted;
                // set sprite
                if (Random.value > randomValueThreshold) {
                    cell.GetComponent<SpriteRenderer>().sprite = GetRandomSprite(sprites);
                }
                // set logical position, i.e., indices in script
                // cell.GetComponent<CellController>().indices = new Vector2Int(row, col);

                grid[row, col] = cell;
            }
        }
    }

    Sprite GetRandomSprite(List<Sprite> sprites) {
        int idx = Random.Range(0, sprites.Count);
        return sprites[idx];
    }

    Sprite GetRandomSpriteAtIndices(int row, int col) {
        return null;
    }

    // add between 1 to 3 sprites every round
}
