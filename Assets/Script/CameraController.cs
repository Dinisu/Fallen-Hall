using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]private GameObject player;   // プレイヤーオブジェクト

    [SerializeField,Header("カメラのプレイヤーに対するオフセット")]
    private Vector3 cameraOffset = new Vector3(0f, 5f, -10f); // Inspector で調整可能

    void Update()
    {
        // プレイヤー位置 + 任意のオフセットをカメラ位置に設定
        transform.position = player.transform.position + cameraOffset;
    }
}
