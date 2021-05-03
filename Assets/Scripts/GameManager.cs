using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class GameManager : MonoBehaviour
{
    int minSpritesToAdd = 2;
    int maxSpritesToAdd = 35; // note that int Range is exclusive
    int gridDimension = 8;
    int scoreMultiplier = 10; // 10 points per planet
    float pixelScale = 1;
    float randomValueThreshold = 0.8f; // fill only 20% of the cells on startup

    // placeholder put into positions along a planet's move path
    public Sprite highlightSprite;
    public List<Sprite> sprites;
    public GameObject cellPrefab;

    // logical representation of cells, [0, 0] is at bottom-left
    // grows upward and to the right
    GameObject[,] grid;
    List<Vector2Int> emptyIndices; // indices at which sprite is null

    // GUI
    public GameObject titleScreen;
    public GameObject gameOverScreen;
    public TextMeshProUGUI scoreText;

    int _score;
    public int score {
        get { return _score; }
        set {
            _score = value;
            scoreText.text = "score: " + _score.ToString();
        }
    }

    // singleton
    public static GameManager Instance { get; private set; }

    void Awake() {
        Instance = this;
        score = 0;
        gameOverScreen.SetActive(false);
    }

    void Start() {
        emptyIndices = new List<Vector2Int>();
        grid = new GameObject[gridDimension, gridDimension];
        InitGrid();
    }

    void OnMouseDown() {
        if (IsShowingTitle()) {
            StartGame();
        }
    }

    bool IsShowingTitle() {
        return titleScreen.activeSelf;
    }

    void StartGame() {
        GetComponent<BoxCollider2D>().enabled = false;
        titleScreen.SetActive(false);
        scoreText.gameObject.SetActive(true);
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
                    cell.GetComponent<SpriteRenderer>().sprite = GetRandomSpriteForIndices(row, col, true);
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
        List<Vector2Int> path = BreadthFirstSearch(srcIndices, dstIndices);
        if (path == null) {
            return false;
        }

        // actually make the move
        SpriteRenderer srcRenderer = GetSpriteRendererAtIndices(srcIndices.x, srcIndices.y);
        GameObject srcCell = grid[srcIndices.x, srcIndices.y];
        srcCell.GetComponent<CellController>().Clear();

        // logical update before GUI update
        emptyIndices.Remove(dstIndices); // dst now occupied
        emptyIndices.Add(srcIndices); // src now empty

        // set dst sprite for ScoreMatches detection
        dstRenderer.sprite = srcRenderer.sprite;
        // this coroutine changes emptyIndices internally
        StartCoroutine("MoveAlongPath", path);

        // detect and score matches
        ScoreMatches(dstIndices.x, dstIndices.y, dstRenderer);

        return true;
    }

    IEnumerator MoveAlongPath(List<Vector2Int> path) {
        // start at src and end one node before dst
        for (int i = 0; i < path.Count - 1; i++) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(path[i].x, path[i].y);
            renderer.sprite = highlightSprite;
        }
        // before waiting, mark the indices at which sprites are going to be added as occupied
        // this function call changes emptyIndices internally
        HashSet<Vector2Int> indicesToAddSprites = GetIndicesToAddSprites();

        yield return new WaitForSeconds(1);
        // undo highlight
        for (int i = 0; i < path.Count - 1; i++) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(path[i].x, path[i].y);
            renderer.sprite = null;
        }
        // add new sprites
        foreach (Vector2Int indices in indicesToAddSprites) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(indices.x, indices.y);
            renderer.sprite = GetRandomSpriteForIndices(indices.x, indices.y, false);
        }

        // detect endgame if grid is full
        if (IsGridFull()) {
            GameOver();
        }
    }

    List<Vector2Int> BreadthFirstSearch(Vector2Int srcIndices, Vector2Int dstIndices) {
        // identify a path from srcIndices to dstIndices, could be null
        // the path include src and dst
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<List<Vector2Int>> pathQueue = new Queue<List<Vector2Int>>();

        List<Vector2Int> startPath = new List<Vector2Int>();
        startPath.Add(srcIndices);
        pathQueue.Enqueue(startPath);

        while (pathQueue.Count > 0) {
            List<Vector2Int> path = pathQueue.Dequeue();
            Vector2Int node = path[path.Count - 1];
            if (visited.Contains(node)) {
                continue;
            }
            if (node == dstIndices) { // done
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

    void ToggleColliders(bool enabled) {
        // cells are not clickable when collider disabled
        for (int row = 0; row < gridDimension; row++) {
            for (int col = 0; col < gridDimension; col++) {
                grid[row, col].GetComponent<BoxCollider2D>().enabled = enabled;
            }
        }
    }

    void GameOver() {
        Debug.Log("Game over!");
        ToggleColliders(false);
        // record score
        PlayerPrefs.SetInt("score", score);
        PlayerPrefs.Save();
        gameOverScreen.SetActive(true);
        // stop bgm and play sound effect
        GameObject.Find("Main Camera").GetComponent<AudioSource>().Pause();
        SoundManager.Instance.PlaySound(SoundType.TypeGameOver);
    }

    bool IsGridFull() {
        return emptyIndices.Count == 0;
    }

    void ScoreMatches(int row, int col, SpriteRenderer currRenderer) {
        Assert.AreNotEqual(currRenderer.sprite, highlightSprite);
        // if scanning globally, only need to scan to the right and up
        HashSet<Vector2Int> matchedIndices = new HashSet<Vector2Int>();
        // only horizontal and vertical matches are possible
        List<Vector2Int> horizontalMatches = new List<Vector2Int>();
        // right
        for (int rr = row + 1; rr < gridDimension; rr++) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(rr, col);
            if (renderer == null || renderer.sprite != currRenderer.sprite) {
                break;
            }
            horizontalMatches.Add(new Vector2Int(rr, col));
        }
        // left
        for (int rr = row - 1; rr >= 0; rr--) {
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
        // up
        for (int cc = col + 1; cc < gridDimension; cc++) {
            SpriteRenderer renderer = GetSpriteRendererAtIndices(row, cc);
            if (renderer == null || renderer.sprite != currRenderer.sprite) {
                break;
            }
            verticalMatches.Add(new Vector2Int(row, cc));
        }
        // down
        for (int cc = col - 1; cc >= 0; cc--) {
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

        if (matchedIndices.Count == 0) {
            SoundManager.Instance.PlaySound(SoundType.TypeSelect);
            return;
        }

        SoundManager.Instance.PlaySound(SoundType.TypeMatch);
        StartCoroutine("HighlightAndRemoveMatches", matchedIndices);
        // accumulate score
        score += matchedIndices.Count * scoreMultiplier;
    }

    IEnumerator HighlightAndRemoveMatches(HashSet<Vector2Int> matchedIndices) {
        // highlight
        foreach (Vector2Int indices in matchedIndices) {
            GameObject cell = grid[indices.x, indices.y];
            cell.GetComponent<CellController>().Highlight();
            // logically mark cell as empty before waiting
            emptyIndices.Add(indices);
        }
        yield return new WaitForSeconds(1);
        // remove
        foreach (Vector2Int indices in matchedIndices) {
            GetSpriteRendererAtIndices(indices.x, indices.y).sprite = null;
            // remove highlight
            GameObject cell = grid[indices.x, indices.y];
            cell.GetComponent<CellController>().Clear();
        }
    }

    HashSet<Vector2Int> GetIndicesToAddSprites() {
        // this function changes emptyIndices internally
        HashSet<Vector2Int> indicesToAddSprites = new HashSet<Vector2Int>();
        int numSpritesToAdd = Random.Range(minSpritesToAdd, maxSpritesToAdd + 1);
        for (int unused = 0; unused < numSpritesToAdd; unused++) {
            if (emptyIndices.Count == 0) {
                break;
            }
            int idx = Random.Range(0, emptyIndices.Count);
            indicesToAddSprites.Add(emptyIndices[idx]);
            emptyIndices.RemoveAt(idx); // mark as no longer empty
        }
        return indicesToAddSprites;
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
