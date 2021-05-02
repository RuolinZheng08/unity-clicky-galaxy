using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI newHighScoreText;

    void OnEnable() {
        int score = GameManager.Instance.score;
        int highScore = LoadHighScore();
        if (score > highScore) {
            highScore = score;
            SaveHighScore(highScore);
            newHighScoreText.gameObject.SetActive(true);
        }
        highScoreText.text = "highest score: " + highScore.ToString();
    }

    public void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    int LoadHighScore() {
        return PlayerPrefs.GetInt("highScore", 0);
    }

    void SaveHighScore(int highScore) {
        PlayerPrefs.SetInt("highScore", highScore);
    }
}
