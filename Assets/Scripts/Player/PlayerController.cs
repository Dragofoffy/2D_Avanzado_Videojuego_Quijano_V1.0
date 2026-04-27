using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float speed = 5f;

    [Header("Configuración de Stun (Efecto Visual)")]
    [Tooltip("Probabilidad de que el jugador gire al ser golpeado (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)] public float stunSpinProbability = 0.5f;
    public GameObject stunEffectObject;

    private Rigidbody2D rb;
    private Animator animator;
    [HideInInspector] public bool isKnockedBack = false;
    private Coroutine currentKnockbackCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Configuración física inicial
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        // MANTENEMOS FreezeRotation para que el jugador no se vuelva loco
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (stunEffectObject != null) stunEffectObject.SetActive(false);
    }

    void Update()
    {
        // Si está aturdido, bloqueamos el input de movimiento
        if (isKnockedBack) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveX, moveY).normalized;

        rb.linearVelocity = movement * speed;

        // Actualizar parámetros del animador
        if (movement.magnitude > 0.1f)
        {
            animator.SetFloat("MoveX", moveX);
            animator.SetFloat("MoveY", moveY);
        }
    }

    // El Zombie llama a este método al colisionar
    public void StartKnockback(float duration)
    {
        if (currentKnockbackCoroutine != null) StopCoroutine(currentKnockbackCoroutine);
        currentKnockbackCoroutine = StartCoroutine(KnockbackRoutine(duration));
    }

    IEnumerator KnockbackRoutine(float duration)
    {
        isKnockedBack = true;

        // 1. Activar efectos visuales
        if (stunEffectObject != null) stunEffectObject.SetActive(true);

        // 2. Lógica de Probabilidad para el giro (90 grados)
        // Random.value genera un numero entre 0.0 y 1.0
        if (Random.value <= stunSpinProbability)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }

        // 3. Fricción alta durante el empuje
        float originalDrag = rb.linearDamping;
        rb.linearDamping = 4f;

        yield return new WaitForSeconds(duration);

        // 4. Restauración
        transform.rotation = Quaternion.identity; // Vuelve a la rotación original (0,0,0)

        if (stunEffectObject != null) stunEffectObject.SetActive(false);

        rb.linearDamping = originalDrag;
        isKnockedBack = false;
        currentKnockbackCoroutine = null;
    }
}