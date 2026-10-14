using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("プレイヤー")]
    [SerializeField] private Transform player;

    [Header("深度UI")]
    [SerializeField] private TextMeshProUGUI depthText;

    [Header("ゲームオーバーUI")]
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private bool isGameOver = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        UpdateDepth();
    }

    /// <summary>
    /// プレイヤーのY座標から深度を計算して表示
    /// </summary>
    private void UpdateDepth()
    {
        if (player == null || depthText == null)
            return;

        // 下に行くほど深度が増えるようにする
        float depth = Mathf.Max(0f, -player.position.y);

        depthText.text = $"深度:{depth:F1}m";
    }

    /// <summary>
    /// ゲームオーバー処理
    /// </summary>
    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        // プレイヤーの深度を取得
        float depth = Mathf.Max(0f, -player.position.y);

        // スコアと深度を表示
        scoreText.text = $"スコア\n深度：{depth:F1}m";

        // 深度を非表示
        depthText.gameObject.SetActive(false);
        // ゲームオーバーUIを表示
        gameOverText.gameObject.SetActive(true);

        // ゲームを停止
        Time.timeScale = 0f;

        Debug.Log("ゲームオーバー");
    }
}