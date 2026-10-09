
using System.Collections.Generic;
using UnityEngine;

public class StageGenerator : MonoBehaviour
{
    [Header("プレイヤー")]
    [SerializeField] private Transform player;

    [Header("ステージPrefab")]
    [SerializeField] private GameObject[] stagePrefabs;

    [Header("生成設定")]
    [SerializeField] private float pieceHeight = 20f;         //ステージピースの高さ
    [SerializeField] private int initialPieceCount = 5;       //初期ステージピース数
    [SerializeField] private float spawnAheadDistance = 40f;  //前方スポーン距離
    [SerializeField] private float deleteBehindDistance = 30f;//後方距離を削除

    // 生成済みステージピース
    private readonly List<GameObject> generatedStagePieces = new();

    // 次に生成するステージピースの番号
    private int nextStagePieceIndex = 0;

    // 次のステージピースを生成するY座標
    private float nextSpawnPieceY;

    private void Start()
    {
        // このオブジェクトのY座標を開始位置にする
        nextSpawnPieceY = transform.position.y;

        // 初期ステージピースを生成
        for (int i = 0; i < initialPieceCount; i++)
        {
            GenerateStagePiece();
        }
    }

    private void Update()
    {
        if (player == null || stagePrefabs == null || stagePrefabs.Length == 0)
            return;

        GenerateStagesAhead();
        DeletePassedStagePieces();
    }

    /// <summary>
    /// ステージピースを1つ生成する
    /// </summary>
    private void GenerateStagePiece()
    {
        GameObject prefab = stagePrefabs[nextStagePieceIndex];

        if (prefab == null)
            return;

        Vector3 spawnPosition = new Vector3(
            transform.position.x,
            nextSpawnPieceY,
            transform.position.z
        );

        GameObject stage = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );

        generatedStagePieces.Add(stage);

        // 次の生成位置を下げる
        nextSpawnPieceY -= pieceHeight;

        // Prefabの範囲内でランダムに選択
        if (stagePrefabs != null && stagePrefabs.Length > 0)
        {
            nextStagePieceIndex = Random.Range(0, stagePrefabs.Length);
        }
    }

    /// <summary>
    /// プレイヤーの進行に合わせて先のステージピースを生成
    /// </summary>
    private void GenerateStagesAhead()
    {
        float playerY = player.position.y;

        // 生成済みステージピースの終端がプレイヤーに近づいたら追加生成
        while (nextSpawnPieceY > playerY - spawnAheadDistance)
        {
            GenerateStagePiece();
        }
    }

    /// <summary>
    /// プレイヤーが通過したステージピースを削除
    /// </summary>
    private void DeletePassedStagePieces()
    {
        float playerY = player.position.y;

        for (int i = generatedStagePieces.Count - 1; i >= 0; i--)
        {
            GameObject stage = generatedStagePieces[i];

            if (stage == null)
            {
                generatedStagePieces.RemoveAt(i);
                continue;
            }

            // ステージピースの下端を計算
            float stagePieceBottomY = stage.transform.position.y - pieceHeight;

            // プレイヤーが十分下まで進んだら削除
            if (playerY < stagePieceBottomY - deleteBehindDistance)
            {
                Destroy(stage);
                generatedStagePieces.RemoveAt(i);
            }
        }
    }
}