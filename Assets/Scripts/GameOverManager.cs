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
        newHighScoreText.gameObject.SetActive(false);
        int score = PlayerPrefs.GetInt("CurrentScore");
        int highScore = LoadHighScore();
        if (score > highScore) {
            highScore = score;
            SaveHighScore(highScore);
            newHighScoreText.gameObject.SetActive(true);
            SoundManager.Instance.PlaySound(SoundType.TypeNewHighScore);
        } else {
            SoundManager.Instance.PlaySound(SoundType.TypeGameOver);
        }
        highScoreText.text = "highest score: " + highScore.ToString();
    }

    public void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    int LoadHighScore() {
        return PlayerPrefs.GetInt("HighScore", 0);
    }

    void SaveHighScore(int highScore) {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }
}
