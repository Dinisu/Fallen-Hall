using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("プレイヤー")]
    [SerializeField] private Transform player;

    [Header("深度UI")]
    [SerializeField] private TextMeshProUGUI depthText;

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

        Debug.Log("ゲームオーバー");

        // 後でゲームオーバーUIなどをここに追加
    }
}