using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float horizontalForce = 5f;

    [Header("落下設定")]
    [SerializeField] private float fallAcceleration = 2f;
    [SerializeField] private float fallDeceleration = 2f;

    [Header("速度制限")]
    [SerializeField] private float maxHorizontalSpeed = 8f;
    [SerializeField] private float minFallSpeed = 2f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("ダメージ設定")]
    [SerializeField] private int maxHp = 3;
    [SerializeField] private float damageInterval = 0.5f;

    [Header("反発設定")]
    [SerializeField] private float knockbackForce = 5f;

    private int currentHp;
    private float lastDamageTime = -Mathf.Infinity;

    private Rigidbody2D rb;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHp = maxHp;
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        MoveHorizontal();
        ControlFallSpeed();
        LimitSpeed();
    }

    /// <summary>
    /// 入力を取得
    /// </summary>
    private void GetInput()
    {
        moveInput = Vector2.zero;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // 左右入力
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
        {
            moveInput.x = -1f;
        }
        else if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
        {
            moveInput.x = 1f;
        }

        // 上下入力
        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
        {
            moveInput.y = 1f;
        }
        else if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
        {
            moveInput.y = -1f;
        }
    }

    /// <summary>
    /// 左右移動
    /// </summary>
    private void MoveHorizontal()
    {
        if (moveInput.x == 0)
            return;

        rb.AddForce(
            Vector2.right * moveInput.x * horizontalForce,
            ForceMode2D.Force
        );
    }

    /// <summary>
    /// 落下速度を調整
    /// </summary>
    private void ControlFallSpeed()
    {
        // 上入力：落下を減速
        if (moveInput.y > 0 && rb.linearVelocity.y < 0)
        {
            rb.AddForce(
                Vector2.up * fallDeceleration,
                ForceMode2D.Force
            );
        }

        // 下入力：落下を加速
        else if (moveInput.y < 0)
        {
            rb.AddForce(
                Vector2.down * fallAcceleration,
                ForceMode2D.Force
            );
        }
    }

    /// <summary>
    /// 速度を制限
    /// </summary>
    private void LimitSpeed()
    {
        Vector2 velocity = rb.linearVelocity;

        // 横方向の速度制限
        velocity.x = Mathf.Clamp(
            velocity.x,
            -maxHorizontalSpeed,
            maxHorizontalSpeed
        );

        // 下方向への速度制限
        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
        }

        // 減速入力中、落下速度が最低速度より遅くなったら補正
        if (moveInput.y > 0 &&
            velocity.y < 0 &&
            velocity.y > -minFallSpeed)
        {
            velocity.y = -minFallSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ダメージ間隔中なら処理しない
        if (Time.time < lastDamageTime + damageInterval)
            return;

        lastDamageTime = Time.time;

        // HPを減らす
        currentHp--;
        Debug.Log($"ダメージ！ 残りHP：{currentHp}");

        // 接触箇所からプレイヤーの中心へ向かう方向を取得
        ContactPoint2D contact = collision.GetContact(0);

        Vector2 knockbackDirection =
            ((Vector2)transform.position - contact.point).normalized;

        // 接触位置がプレイヤーの中心に近い場合の保険
        if (knockbackDirection == Vector2.zero)
        {
            knockbackDirection = -rb.linearVelocity.normalized;
        }

        // 接触箇所の反対側へ弾く
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        if (currentHp <= 0)
        {
            GameManager.Instance.GameOver();
        }
    }
}