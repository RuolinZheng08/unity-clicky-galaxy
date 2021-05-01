using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    int minSpritesToAdd = 1;
    int maxSpritesToAdd = 3; // note that int Range is exclusive
    int gridDimension = 8;
    float pixelScale = 1;
    float randomValueThreshold = 0.8f; // fill only 20% of the cells on startup

    public List<Sprite> sprites;
    public GameObject cellPrefab;

    // logical representation of cells, [0, 0] is at bottom-left
    // grows upward and to the right
    GameObject[,] grid;
    List<Vector2Int> emptyIndices; // indices at which sprite is null

    // GUI

    // singleton
    public static GameManager Instance { get; private set; }

    void Awake() {
        Instance = this;
    }

    void Start() {
        emptyIndices = new List<Vector2Int>();
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
                    cell.GetComponent<SpriteRenderer>().sprite = GetRandomSpriteForIndices(row, col);
                }  else { // record this index since its sprite is null
                    emptyIndices.Add(new Vector2Int(row, col));
                }
                // set logical position, i.e., indices in script
                cell.GetComponent<CellController>().indices = new Vector2Int(row, col);

                grid[row, col] = cell;
            }
        }
    }

    public bool TryMoveCell(Vector2Int srcIndices, Vector2Int dstIndices) {
        SpriteRenderer dstRenderer = GetSpriteRendererAtIndices(dstIndices.x, dstIndices.y);
        // false if destination is already occupied
        if (dstRenderer != null && dstRenderer.sprite != null) {
            return false;
        }
        // false also if there isn't a path from src to dest

        // actually make the move
        // TODO: use a coroutine for animation
        SpriteRenderer srcRenderer = GetSpriteRendererAtIndices(srcIndices.x, srcIndices.y);
        dstRenderer.sprite = srcRenderer.sprite;
        srcRenderer.sprite = null;
        emptyIndices.Remove(dstIndices); // dst now occupied
        emptyIndices.Add(srcIndices); // src now empty

        // detect and score matches
        ScoreMatches(dstIndices.x, dstIndices.y, dstRenderer);

        // add new sprites
        AddSprites();

        // detect if grid is full
        if (IsGridFull()) {
            GameOver();
        }

        return true;
    }

    void GameOver() {
        Debug.Log("Game over!");
    }

    bool IsGridFull() {
        return emptyIndices.Count == 0;
    }

    void ScoreMatches(int row, int col, SpriteRenderer currRenderer) {
        HashSet<Vector2Int> matchedIndices = new HashSet<Vector2Int>();
        // only horizontal and vertical matches are possible
        List<Vector2Int> horizontalMatches = new List<Vector2Int>();
        // left
        for (int rr = row - 1; rr >= 0; rr--) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(rr, col);
            if (renderer == null || renderer.sprite != currRenderer.sprite) {
                break;
            }
            horizontalMatches.Add(new Vector2Int(rr, col));
        }
        // right
        for (int rr = row + 1; rr < gridDimension; rr++) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(rr, col);
            if (renderer == null || renderer.sprite != currRenderer.sprite) {
                break;
            }
            horizontalMatches.Add(new Vector2Int(rr, col));
        }
        if (horizontalMatches.Count >= 2) {
            matchedIndices.UnionWith(horizontalMatches);
            matchedIndices.Add(new Vector2Int(row, col)); // add myself
        }

        List<Vector2Int> verticalMatches = new List<Vector2Int>();
        // down
        for (int cc = col - 1; cc >= 0; cc--) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(row, cc);
            if (renderer == null || renderer.sprite != currRenderer.sprite) {
                break;
            }
            verticalMatches.Add(new Vector2Int(row, cc));
        }
        // up
        for (int cc = col + 1; cc < gridDimension; cc++) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(row, cc);
            if (renderer == null || renderer.sprite != currRenderer.sprite) {
                break;
            }
            verticalMatches.Add(new Vector2Int(row, cc));
        }
        if (verticalMatches.Count >= 2) {
            matchedIndices.UnionWith(verticalMatches);
            matchedIndices.Add(new Vector2Int(row, col)); // add myself
        }

        // remove
        foreach (Vector2Int indices in matchedIndices) {
            GetSpriteRendererAtIndices(indices.x, indices.y).sprite = null;
            // mark cell as empty
            emptyIndices.Remove(indices);
        }

        // TODO: score
    }

    void AddSprites() {
        int numSpritesToAdd = Random.Range(minSpritesToAdd, maxSpritesToAdd + 1);
        for (int unused = 0; unused < numSpritesToAdd; unused++) {
            int idx = Random.Range(0, emptyIndices.Count);
            Vector2Int cell = emptyIndices[idx];
            int row = cell.x;
            int col = cell.y;
            // fill in a random sprite in this cell
            GetSpriteRendererAtIndices(row, col).sprite = GetRandomSpriteForIndices(row, col);
            emptyIndices.RemoveAt(idx); // no longer empty
        }
    }

    Sprite GetRandomSprite(List<Sprite> sprites) {
        int idx = Random.Range(0, sprites.Count);
        return sprites[idx];
    }

    Sprite GetRandomSpriteForIndices(int row, int col) {
        // make it impossible to get three in a row upon starting up
        // since the grid grows upward and to the right
        // need to check the left and the bottom
        List<Sprite> possibleSprites = new List<Sprite>(sprites);
        Sprite left1 = GetSpriteAtIndices(row, col - 1);
        Sprite left2 = GetSpriteAtIndices(row, col - 2);
        if (left2 != null && left1 == left2) { // cannot use this sprite
            possibleSprites.Remove(left1);
        }

        Sprite down1 = GetSpriteAtIndices(row - 1, col);
        Sprite down2 = GetSpriteAtIndices(row - 2, col);
        if (down2 != null && down1 == down2) { // cannot use this sprite
            possibleSprites.Remove(down1);
        }

        return GetRandomSprite(possibleSprites);
    }

    SpriteRenderer GetSpriteRendererAtIndices(int row, int col) {
        if (col < 0 || col >= gridDimension || row < 0 || row >= gridDimension) {
            return null;
        }
        GameObject cell = grid[row, col];
        return cell.GetComponent<SpriteRenderer>();
    }

    Sprite GetSpriteAtIndices(int row, int col) {
        SpriteRenderer renderer = GetSpriteRendererAtIndices(row, col);
        if (renderer == null) {
            return null;
        }
        return renderer.sprite;
    }

}
