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
                    cell.GetComponent<CellController>().logicalSprite = GetRandomSpriteForIndices(row, col, true);
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
        CellController dstController = GetCellControllerAtIndices(dstIndices.x, dstIndices.y);
        // false if destination is already occupied
        if (dstController != null && dstController.logicalSprite != null) {
            return false;
        }
        // false also if there isn't a path from src to dest
        List<Vector2Int> path = BreadthFirstSearch(srcIndices, dstIndices);
        if (path == null) {
            return false;
        }

        // actually make the move
        GameObject srcCell = grid[srcIndices.x, srcIndices.y];
        srcCell.GetComponent<CellController>().Clear();
        // move animation
        StartCoroutine("MoveAlongPath", path);
        // update the empty cells on the grid
        emptyIndices.Remove(dstIndices); // dst now occupied
        emptyIndices.Add(srcIndices); // src now empty

        // detect and score matches
        ScoreMatches(dstIndices.x, dstIndices.y, dstController, false);

        // add new sprites
        AddSprites();

        // detect if grid is full
        if (IsGridFull()) {
            GameOver();
        }

        return true;
    }

    IEnumerator WaitForMoveToFinish(List<Vector2Int> path) {
        yield return StartCoroutine("MoveAlongPath", path);
    }

    IEnumerator MoveAlongPath(List<Vector2Int> path) {
        // path contains the src and dst nodes as well
        Sprite sprite = GetSpriteAtIndices(path[0].x, path[0].y);
        for (int i = 1; i < path.Count; i++) {
            Vector2Int currIndices = path[i];
            Vector2Int prevIndices = path[i - 1];
            // use physical sprite here for animation instead of logical sprite
            GetSpriteRendererAtIndices(currIndices.x, currIndices.y).sprite = sprite;
            GetSpriteRendererAtIndices(prevIndices.x, prevIndices.y).sprite = null;
            yield return new WaitForSeconds(0.1f);
        }
    }

    List<Vector2Int> BreadthFirstSearch(Vector2Int srcIndices, Vector2Int dstIndices) {
        // identify a path from srcIndices to dstIndices, could be null
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> nodeQueue = new Queue<Vector2Int>();
        Queue<List<Vector2Int>> pathQueue = new Queue<List<Vector2Int>>();

        List<Vector2Int> startPath = new List<Vector2Int>();
        startPath.Add(srcIndices);
        pathQueue.Enqueue(startPath);
        nodeQueue.Enqueue(srcIndices);

        while (nodeQueue.Count > 0) {
            Vector2Int node = nodeQueue.Dequeue();
            if (visited.Contains(node)) {
                continue;
            }
            List<Vector2Int> path = pathQueue.Dequeue();
            if (node == dstIndices) { // done
                path.Add(node);
                return path;
            }
            visited.Add(node);
            List<Vector2Int> neighbors = GetNeighbors(node);
            foreach (Vector2Int neighbor in neighbors) {
                Sprite sprite = GetSpriteAtIndices(neighbor.x, neighbor.y);
                if (sprite == null) { // can visit this next
                    List<Vector2Int> newPath = new List<Vector2Int>(path);
                    newPath.Add(neighbor);
                    pathQueue.Enqueue(newPath);
                    nodeQueue.Enqueue(neighbor);
                }
            }
        }

        return null;
    }

    List<Vector2Int> GetNeighbors(Vector2Int indices) {
        // return the four immediate neighbors, left, right, up, down
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (indices.x >= 0 && indices.x < gridDimension && indices.y >= 0 && indices.y < gridDimension) {
            if (indices.y >= 1) {
                neighbors.Add(new Vector2Int(indices.x, indices.y - 1));
            }
            if (indices.y < gridDimension - 1) {
                neighbors.Add(new Vector2Int(indices.x, indices.y + 1));
            }
            if (indices.x >= 1) {
                neighbors.Add(new Vector2Int(indices.x - 1, indices.y));
            }
            if (indices.x < gridDimension - 1) {
                neighbors.Add(new Vector2Int(indices.x + 1, indices.y));
            }
        }
        return neighbors;
    }

    void GameOver() {
        Debug.Log("Game over!");
    }

    bool IsGridFull() {
        return emptyIndices.Count == 0;
    }

    void ScoreMatches(int row, int col, CellController currController, bool globalScan) {
        // if scanning globally, only need to scan to the right and up
        HashSet<Vector2Int> matchedIndices = new HashSet<Vector2Int>();
        // only horizontal and vertical matches are possible
        List<Vector2Int> horizontalMatches = new List<Vector2Int>();
        // right
        for (int rr = row + 1; rr < gridDimension; rr++) {
            CellController controller = GetCellControllerAtIndices(rr, col);
            if (controller == null || controller.logicalSprite != controller.logicalSprite) {
                break;
            }
            horizontalMatches.Add(new Vector2Int(rr, col));
        }
        // left
        if (!globalScan) {
            for (int rr = row - 1; rr >= 0; rr--) {
                CellController controller = GetCellControllerAtIndices(rr, col);
                if (controller == null || controller.logicalSprite != controller.logicalSprite) {
                    break;
                }
                horizontalMatches.Add(new Vector2Int(rr, col));
            }
        }
        if (horizontalMatches.Count >= 2) {
            matchedIndices.UnionWith(horizontalMatches);
            matchedIndices.Add(new Vector2Int(row, col)); // add myself
        }

        List<Vector2Int> verticalMatches = new List<Vector2Int>();
        // up
        for (int cc = col + 1; cc < gridDimension; cc++) {
            CellController controller = GetCellControllerAtIndices(row, cc);
            if (controller == null || controller.logicalSprite != controller.logicalSprite) {
                break;
            }
            verticalMatches.Add(new Vector2Int(row, cc));
        }
        // down
        if (!globalScan) {
            for (int cc = col - 1; cc >= 0; cc--) {
                CellController controller = GetCellControllerAtIndices(row, cc);
                if (controller == null || controller.logicalSprite != controller.logicalSprite) {
                    break;
                }
                verticalMatches.Add(new Vector2Int(row, cc));
            }
        }
        if (verticalMatches.Count >= 2) {
            matchedIndices.UnionWith(verticalMatches);
            matchedIndices.Add(new Vector2Int(row, col)); // add myself
        }

        StartCoroutine("HighlightAndRemoveMatches", matchedIndices);

        // TODO: score
    }

    IEnumerator HighlightAndRemoveMatches(HashSet<Vector2Int> matchedIndices) {
        // highlight
        foreach (Vector2Int indices in matchedIndices) {
            GameObject cell = grid[indices.x, indices.y];
            cell.GetComponent<CellController>().Highlight();
        }
        yield return new WaitForSeconds(1);
        // remove
        foreach (Vector2Int indices in matchedIndices) {
            GetCellControllerAtIndices(indices.x, indices.y).logicalSprite = null;
            // mark cell as empty
            emptyIndices.Add(indices);
            // remove highlight
            GameObject cell = grid[indices.x, indices.y];
            cell.GetComponent<CellController>().Clear();
        }
    }

    void AddSprites() {
        int numSpritesToAdd = Random.Range(minSpritesToAdd, maxSpritesToAdd + 1);
        for (int unused = 0; unused < numSpritesToAdd; unused++) {
            if (emptyIndices.Count == 0) {
                break;
            }
            int idx = Random.Range(0, emptyIndices.Count);
            Vector2Int cell = emptyIndices[idx];
            int row = cell.x;
            int col = cell.y;
            // fill in a random sprite in this cell
            GetCellControllerAtIndices(row, col).logicalSprite = GetRandomSpriteForIndices(row, col, false);
            emptyIndices.RemoveAt(idx); // no longer empty
        }
    }

    Sprite GetRandomSprite(List<Sprite> sprites) {
        int idx = Random.Range(0, sprites.Count);
        return sprites[idx];
    }

    Sprite GetRandomSpriteForIndices(int row, int col, bool startup) {
        // avoid three in a row
        List<Sprite> possibleSprites = new List<Sprite>(sprites);

        // only need to check left and down when initializing the grid at startup
        // since the grid grows to the right and up
        Sprite left1 = GetSpriteAtIndices(row, col - 1);
        Sprite left2 = GetSpriteAtIndices(row, col - 2);
        Sprite down1 = GetSpriteAtIndices(row - 1, col);
        Sprite down2 = GetSpriteAtIndices(row - 2, col);

        // cannot use this sprite if there are already two identicals on one side
        if (left2 != null && left1 == left2) {
            possibleSprites.Remove(left1);
        }
        if (down2 != null && down1 == down2) {
            possibleSprites.Remove(down1);
        }

        if (!startup) {
            Sprite right1 = GetSpriteAtIndices(row, col + 1);
            Sprite right2 = GetSpriteAtIndices(row, col + 2);
            Sprite up1 = GetSpriteAtIndices(row + 1, col);
            Sprite up2 = GetSpriteAtIndices(row + 2, col);

            if (right2 != null && right1 == right2) {
                possibleSprites.Remove(right1);
            }
            if (up2 != null && up1 == up2) {
                possibleSprites.Remove(up1);
            }
            // cannot use this sprite if it's sandwiched between two identicals
            if (left1 != null && left1 == right1) {
                // implicitly right1 cannot be null
                possibleSprites.Remove(left1);
            }
            if (down1 != null && down1 == up1) {
                possibleSprites.Remove(down1);
            }
        }
        return GetRandomSprite(possibleSprites);
    }

    CellController GetCellControllerAtIndices(int row, int col) {
        if (col < 0 || col >= gridDimension || row < 0 || row >= gridDimension) {
            return null;
        }
        GameObject cell = grid[row, col];
        return cell.GetComponent<CellController>();
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
