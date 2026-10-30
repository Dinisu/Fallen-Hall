using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    // HPの初期値と、連続してダメージを受けるのを防ぐ待ち時間です。
    [Header("ダメージ設定")]
    [SerializeField] private int maxHp = 3;
    [SerializeField] private float damageInterval = 0.5f;

    private int currentHp;
    private float lastDamageTime = -Mathf.Infinity;
    private Vector2 moveInput;
    private PlayerPhysicsController physicsController;

    // GameManagerや物理制御側がHPと入力を参照するための読み取り専用プロパティです。
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public Vector2 MoveInput => moveInput;

    private void Awake()
    {
        // HPを初期化し、移動・反発を担当する物理コンポーネントを取得します。
        currentHp = maxHp;
        physicsController = GetComponent<PlayerPhysicsController>();
    }

    private void Update()
    {
        // 入力はUpdateで読み取り、PlayerPhysicsControllerがFixedUpdateで移動に使います。
        GetInput();
    }

    private void GetInput()
    {
        // キーを離した状態やキーボード未接続時に前フレームの入力が残らないようにします。
        moveInput = Vector2.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            // ゲームオーバー中はSpaceで時間停止を解除して現在のシーンを再読み込みします。
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            // ゲームオーバー中は移動入力を物理制御へ渡しません。
            return;
        }

        // 左右キーまたはA/Dを水平移動入力へ変換します。
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
            moveInput.x = -1f;
        else if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
            moveInput.x = 1f;

        // 上下キーまたはW/Sを落下速度の調整入力へ変換します。
        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
            moveInput.y = 1f;
        else if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
            moveInput.y = -1f;
    }

    public void TakeContactAction(Collider2D other, Vector2 contactPoint)
    {
        // 接触先が破棄済みなら処理しません。
        if (other == null)
            return;

        ObjectEffectController objectEffect =
            other.GetComponentInParent<ObjectEffectController>();

        // アイテムなら種類別の効果を呼び、通常のColliderなら接触ダメージにします。
        if (objectEffect != null)
            objectEffect.OnPlayerContact(this, contactPoint);
        else
            TakeDamage(contactPoint);
    }
    public void TakeDamage(Vector2 contactPoint)
    {
        // ダメージ間隔中はHPやノックバックを重ねて適用しません。
        if (Time.time < lastDamageTime + damageInterval)
            return;

        lastDamageTime = Time.time;
        currentHp--;
        Debug.Log($"ダメージ！ 残りHP：{currentHp}");

        // 接触点から離れる方向へノックバックさせます。
        if (physicsController != null)
        {
            Vector2 knockbackDirection =
                ((Vector2)transform.position - contactPoint).normalized;

            // 接触点がPlayerの中心と一致した場合は、現在の速度と反対向きを使います。
            if (knockbackDirection == Vector2.zero)
                knockbackDirection = -physicsController.Velocity.normalized;

            physicsController.ApplyKnockback(knockbackDirection);
        }

        // HPが0以下になったらGameManagerへゲームオーバーを通知します。
        if (currentHp <= 0 && GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    public bool TryHeal(int amount)
    {
        // 回復量が無効、またはHPが満タンなら回復できません。
        if (amount <= 0 || currentHp >= maxHp)
            return false;

        int previousHp = currentHp;
        // 最大HPを超えないように回復量を制限します。
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        Debug.Log($"HP回復！ {previousHp} → {currentHp}");
        return true;
    }

    public void Bounce(float upwardSpeed)
    {
        // 反発速度の設定は物理演算を担当するコンポーネントへ委譲します。
        if (physicsController != null)
            physicsController.Bounce(upwardSpeed);
    }
}
