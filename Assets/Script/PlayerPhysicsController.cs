using System.Collections.Generic;
using UnityEngine;

// Rigidbody2Dを使わず、速度計算とColliderのクエリでPlayerを移動させます。
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerPhysicsController : MonoBehaviour
{
    // 左右入力による加速度と、入力方向へ加速するときの最大速度です。
    [Header("移動設定")]
    [SerializeField] private float horizontalForce = 10f;
    [SerializeField] private float maxHorizontalSpeed = 8f;

    // Physics2D.gravityに重力倍率を掛けて落下速度を計算します。
    // 上入力は速い落下を減速し、下入力は下向きの加速度を追加します。
    [Header("落下設定")]
    [SerializeField] private float gravityScale = 0.2f;
    [SerializeField] private float fallAcceleration = 5f;
    [SerializeField] private float fallDeceleration = 5f;
    [SerializeField] private float minFallSpeed = 10f;
    [SerializeField] private float maxFallSpeed = 20f;

    // ノックバックは現在の速度に加算します。collisionSkinは壁へのめり込みを
    // 防ぐため、Castで衝突を検出した位置から少し手前で止める距離です。
    [Header("接触設定")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float collisionSkin = 0.01f;

    // 物理ステップごとの配列確保を避けるため、Cast/Overlapの結果配列を再利用します。
    private const int QueryCapacity = 32;

    private readonly RaycastHit2D[] castResults = new RaycastHit2D[QueryCapacity];
    private readonly Collider2D[] overlapResults = new Collider2D[QueryCapacity];
    // 前回と今回の接触先を比較し、重なっている間毎回ではなく
    // 接触が始まったときに一度だけ効果を呼び出します。
    private readonly HashSet<Collider2D> previousContacts = new();
    private readonly HashSet<Collider2D> currentContacts = new();

    private CircleCollider2D playerCollider;
    private PlayerController playerController;
    private ContactFilter2D contactFilter;
    private Vector2 velocity;

    // PlayerControllerがノックバック方向を決めるときに現在速度を参照します。
    public Vector2 Velocity => velocity;

    private void Awake()
    {
        // 必須コンポーネントのColliderを移動・接触判定に使います。
        playerCollider = GetComponent<CircleCollider2D>();
        playerController = GetComponent<PlayerController>();

        // Triggerもクエリ対象に含めます。Rigidbody2Dの衝突イベントを使わずに
        // アイテムとの接触を検出するためです。
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        contactFilter.useTriggers = true;
    }

    private void FixedUpdate()
    {
        // 必須コンポーネントが取得できない場合やゲームオーバー中は移動しません。
        if (playerCollider == null || playerController == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        float deltaTime = Time.fixedDeltaTime;
        Vector2 moveInput = playerController.MoveInput;

        // 固定時間刻みを使って重力を速度に加えます。この速度を元に
        // この後の移動量と衝突クエリの距離を計算します。
        velocity += Physics2D.gravity * gravityScale * deltaTime;

        // 入力方向の速度が上限に達していない場合だけ、左右の加速度を加えます。
        if (moveInput.x != 0f &&
            !((moveInput.x > 0f && velocity.x >= maxHorizontalSpeed) ||
              (moveInput.x < 0f && velocity.x <= -maxHorizontalSpeed)))
        {
            velocity.x += moveInput.x * horizontalForce * deltaTime;
        }

        // 上入力は落下速度がminFallSpeedより速い間だけ減速に使います。
        // 下入力では落下速度をさらに上げます。
        if (moveInput.y > 0f && velocity.y < -minFallSpeed)
            velocity.y += fallDeceleration * deltaTime;
        else if (moveInput.y < 0f)
            velocity.y -= fallAcceleration * deltaTime;

        // クエリ間の移動距離が大きくなりすぎないよう、落下速度に上限を設けます。
        if (velocity.y < -maxFallSpeed)
            velocity.y = -maxFallSpeed;

        // 今回の物理ステップで接触したColliderを集めます。各軸の移動後に
        // SyncTransformsを呼び、次の軸のクエリに更新済み位置を反映します。
        currentContacts.Clear();
        Physics2D.SyncTransforms();
        // X軸とY軸を分けて移動します。一方の軸で壁に当たっても、もう一方の
        // 軸の移動を止めず、壁に沿って進めるようにします。
        MoveAlongAxis(velocity.x * deltaTime, true);
        Physics2D.SyncTransforms();
        MoveAlongAxis(velocity.y * deltaTime, false);
        Physics2D.SyncTransforms();
        CollectOverlaps();
        ProcessNewContacts();
    }

    private void MoveAlongAxis(float movement, bool horizontal)
    {
        // この軸の移動量がほぼ0なら、CastとTransform更新を行いません。
        if (Mathf.Abs(movement) <= Mathf.Epsilon)
            return;

        // 移動方向へColliderをCastします。collisionSkin分だけ距離を足して
        // 壁の手前で止まり、Collider同士がめり込むのを防ぎます。
        Vector2 direction = horizontal
            ? Vector2.right * Mathf.Sign(movement)
            : Vector2.up * Mathf.Sign(movement);
        float distance = Mathf.Abs(movement);
        int hitCount = playerCollider.Cast(
            direction,
            contactFilter,
            castResults,
            distance + collisionSkin
        );

        bool hasBlockingHit = false;
        RaycastHit2D blockingHit = default;
        float allowedDistance = distance;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castResults[i];
            // Triggerはアイテム効果の接触先として記録しますが、移動は止めません。
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            // 壁から離れる方向への移動なら、ブロックする衝突として扱いません。
            if (Vector2.Dot(direction, hit.normal) >= 0f)
                continue;

            // この移動距離内にある実体Colliderのうち、最も近い衝突を採用します。
            if (hit.distance <= distance + collisionSkin &&
                (!hasBlockingHit || hit.distance < blockingHit.distance))
            {
                hasBlockingHit = true;
                blockingHit = hit;
                allowedDistance = Mathf.Max(0f, hit.distance - collisionSkin);
            }
        }

        float contactDistance = hasBlockingHit
            ? blockingHit.distance + collisionSkin
            : distance;

        // ブロックする壁より手前で通過したTriggerと、移動中に到達したColliderを
        // 接触処理用の集合へ記録します。
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castResults[i];
            if (hit.collider != null && hit.distance <= contactDistance)
                currentContacts.Add(hit.collider);
        }

        // 衝突までに許された距離だけTransformを直接移動します。
        transform.position += (Vector3)(direction * allowedDistance);

        if (hasBlockingHit)
        {
            currentContacts.Add(blockingHit.collider);
            // 壁に向かう軸の速度を0にし、壁へ押し続ける代わりに
            // もう一方の軸に沿って移動できるようにします。
            if (horizontal)
                velocity.x = 0f;
            else
                velocity.y = 0f;
        }
    }

    private void CollectOverlaps()
    {
        // Castは移動中の接触を検出します。Overlapは、物理ステップの終わりに
        // 既に重なっているアイテムやTriggerを検出します。
        int overlapCount = playerCollider.Overlap(contactFilter, overlapResults);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D other = overlapResults[i];
            // Player自身のColliderや子オブジェクトのColliderは接触対象から除外します。
            if (other != null && other != playerCollider &&
                !other.transform.IsChildOf(transform))
            {
                currentContacts.Add(other);
            }
        }
    }

    private void ProcessNewContacts()
    {
        // 前回のステップに存在しなかった接触先だけPlayerControllerへ通知します。
        // UnityのOnCollisionEnter/OnTriggerEnterに近い「接触開始時」の動作です。
        foreach (Collider2D other in currentContacts)
        {
            if (other != null && !previousContacts.Contains(other))
            {
                Vector2 contactPoint = other.ClosestPoint(transform.position);
                playerController.TakeContactAction(other, contactPoint);
            }
        }

        // 今回の接触先を保存し、次の物理ステップで比較できるようにします。
        previousContacts.Clear();
        foreach (Collider2D other in currentContacts)
        {
            if (other != null)
                previousContacts.Add(other);
        }
    }

    public void Bounce(float upwardSpeed)
    {
        // 横方向の速度は保ったまま、上方向の速度だけを設定します。
        velocity.y = upwardSpeed;
    }

    public void ApplyKnockback(Vector2 direction)
    {
        // ノックバック方向と強さを、現在速度へ瞬間的に加算します。
        velocity += direction * knockbackForce;
    }
}
