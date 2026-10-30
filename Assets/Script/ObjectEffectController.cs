using UnityEngine;

public class ObjectEffectController : MonoBehaviour
{
    public enum ObjectType
    {
        Spring,
        RecoveryItem,
        DamageObject
    }

    [Header("オブジェクトの種類")]
    [SerializeField] private ObjectType objectType;

    [Header("バネ設定")]
    [SerializeField] private float springBounceSpeed = 12f;
    [SerializeField] private float springCooldown = 0.25f;

    [Header("回復アイテム設定")]
    [SerializeField] private int recoverAmount = 1;
    [SerializeField] private bool destroyAfterUse = true;

    private float lastSpringTime = -Mathf.Infinity;
    private bool consumed = false;

    public bool ShouldDamagePlayer =>
        objectType == ObjectType.DamageObject;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player =
            collision.gameObject.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        OnPlayerContact(player, collision.GetContact(0).point);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        OnPlayerContact(player, other.ClosestPoint(player.transform.position));
    }

    public void OnPlayerContact(PlayerController player, Vector2 contactPoint)
    {
        if (player == null)
            return;

        switch (objectType)
        {
            case ObjectType.Spring:
                if (Time.time < lastSpringTime + springCooldown)
                    return;

                lastSpringTime = Time.time;
                player.Bounce(springBounceSpeed);
                break;

            case ObjectType.RecoveryItem:
                if (consumed)
                    return;

                if (player.TryHeal(recoverAmount))
                {
                    consumed = true;
                    if (destroyAfterUse)
                        Destroy(gameObject);
                }
                break;

            case ObjectType.DamageObject:
                player.TakeDamage(contactPoint);
                break;
        }
    }
}