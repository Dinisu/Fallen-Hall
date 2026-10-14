using UnityEngine;

public class ObjectEffectController : MonoBehaviour
{
    public enum ObjectType
    {
        Spring,       // バネ
        RecoveryItem, // 回復アイテム
        DamageObject  // ダメージオブジェクト
    }

    [Header("オブジェクトの種類")]
    [SerializeField] private ObjectType objectType;

    [Header("バネ設定")]
    [SerializeField] private float springBounceSpeed = 12f;
    [SerializeField] private float springCooldown = 0.25f;

    [Header("回復アイテム設定")]
    [SerializeField] private int recoverAmount = 1;
    [SerializeField] private bool destroyAfterUse = true;//使用後に消えるかどうか

    private float lastSpringTime = -Mathf.Infinity;
    private bool consumed = false;

    /// <summary>
    /// このオブジェクトが接触時にダメージを与えるか
    /// </summary>
    public bool ShouldDamagePlayer =>
        objectType == ObjectType.DamageObject;

    /// <summary>
    /// 通常の衝突時
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player =
            collision.gameObject.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        ApplyEffect(player);
    }

    /// <summary>
    /// Trigger接触時
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        ApplyEffect(player);
    }

    /// <summary>
    /// enumに応じて接触時の処理を変更
    /// </summary>
    private void ApplyEffect(PlayerController player)
    {
        switch (objectType)
        {
            case ObjectType.Spring:

                // 短時間に何度も反発するのを防ぐ
                if (Time.time < lastSpringTime + springCooldown)
                    return;

                lastSpringTime = Time.time;

                player.Bounce(springBounceSpeed);
                break;

            case ObjectType.RecoveryItem:

                // 使用済みなら処理しない
                if (consumed)
                    return;

                // HPを回復できた場合だけ使用済みにする
                if (player.TryHeal(recoverAmount))
                {
                    consumed = true;

                    if (destroyAfterUse)
                    {
                        Destroy(gameObject);
                    }
                }

                break;

            case ObjectType.DamageObject:

                // ダメージ処理はPlayerController側で行う
                break;
        }
    }
}