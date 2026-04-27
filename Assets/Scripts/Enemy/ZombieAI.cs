using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class ZombieAI : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public Transform player;
    public float speed = 2f;
    public LayerMask zombieLayer;

    [Header("Configuración: Separación (Evitar amigos)")]
    public float separationRadius = 1.0f;
    public float separationForce = 0.2f;

    [Header("Indicador de Confusión (Jajaja!)")]
    public GameObject confuseIndicator; // ARRASTRA AQUÍ TU OBJETO DE INTERROGACIÓN
    // Qué tan brusco debe ser el giro para que se confunda (mayor = más difícil)
    [Range(0.1f, 1f)] public float confusionThreshold = 0.8f;
    private Vector2 lastDirection; // Para comparar con la actual
    private float confuseTimer = 0f; // Tiempo que permanece visible

    [Header("Configuración: Ataque")]
    public float launchForce = 10f;
    public float liftForce = 4f;
    public float castTime = 0.1f;
    public float cooldownTime = 5f;
    public float stunDuration = 2f;

    [Header("Efectos Visuales")]
    public GameObject attackEffect;
    public GameObject cooldownIndicator;
    public float attackAnimationDuration = 0.4f;

    private int originalSortingOrder = 18;
    private SpriteRenderer attackEffectRenderer;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 smoothedDirection;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        SetAsDynamic();

        if (attackEffect != null)
        {
            attackEffect.SetActive(false);
            attackEffectRenderer = attackEffect.GetComponent<SpriteRenderer>();
            originalSortingOrder = attackEffectRenderer.sortingOrder;
        }
        if (cooldownIndicator != null) cooldownIndicator.SetActive(false);
        if (confuseIndicator != null) confuseIndicator.SetActive(false); // Apagado al inicio
    }

    private void SetAsDynamic()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void SetAsKinematic()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (isAttacking || Time.time < nextAttackTime)
        {
            // Apagamos el indicador de confusión si estaba encendido al atacar
            if (confuseIndicator != null && confuseIndicator.activeSelf) confuseIndicator.SetActive(false);
            return;
        }

        if (player != null)
        {
            // --- MOVIMIENTO ---
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            Vector2 separation = GetSeparationForce();
            Vector2 finalDirection = (directionToPlayer + (separation * separationForce)).normalized;

            smoothedDirection = Vector2.MoveTowards(smoothedDirection, finalDirection, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = smoothedDirection * speed;

            // --- ANIMACIÓN ---
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                animator.SetFloat("MoveZombieX", smoothedDirection.x);
                animator.SetFloat("MoveZombieY", smoothedDirection.y);
            }

            // --- LÓGICA DE CONFUSIÓN (Jajaja!) ---
            DetectConfusion(finalDirection);
        }
    }

    void DetectConfusion(Vector2 currentDirection)
    {
        if (confuseIndicator == null) return;

        // Comparamos la dirección actual con la del frame anterior
        // Usamos Dot (Producto punto): si es < confusionThreshold, el giro es brusco
        float directionChange = Vector2.Dot(lastDirection, currentDirection);

        if (directionChange < confusionThreshold)
        {
            // ¡GIRO BRUSCO DETECTADO! Activamos la interrogación
            confuseIndicator.SetActive(true);
            confuseTimer = 0.5f; // Duración visible (medio segundo)
        }

        // Guardamos la dirección para el siguiente frame
        lastDirection = currentDirection;

        // Gestión del tiempo de visibilidad
        if (confuseIndicator.activeSelf)
        {
            confuseTimer -= Time.fixedDeltaTime;
            if (confuseTimer <= 0f)
            {
                confuseIndicator.SetActive(false);
            }
        }
    }

    // ... (El resto de tu código, GetSeparationForce, OnCollisionEnter2D, AtaqueZombie, etc, sigue igual)
    Vector2 GetSeparationForce()
    {
        Vector2 force = Vector2.zero;
        Collider2D[] others = Physics2D.OverlapCircleAll(transform.position, separationRadius, zombieLayer);
        int count = 0;
        foreach (Collider2D other in others)
        {
            if (other.gameObject != this.gameObject)
            {
                Vector2 diff = (Vector2)(transform.position - other.transform.position);
                force += diff; count++;
            }
        }
        if (count > 0) force /= count;
        return force;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isAttacking && Time.time >= nextAttackTime)
        {
            PlayerController playerScript = collision.gameObject.GetComponent<PlayerController>();
            if (playerScript != null) StartCoroutine(AtaqueZombie(playerScript));
        }
    }

    IEnumerator AtaqueZombie(PlayerController playerScript)
    {
        isAttacking = true;
        SetAsKinematic();
        Vector2 dir = (player.position - transform.position).normalized;
        bool isAttackingUp = false;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0) animator.SetTrigger("AttackRight");
            else animator.SetTrigger("AttackLeft");
        }
        else
        {
            if (dir.y > 0) { animator.SetTrigger("AttackUp"); isAttackingUp = true; }
            else animator.SetTrigger("AttackDown");
        }
        if (attackEffect != null)
        {
            if (isAttackingUp) attackEffectRenderer.sortingOrder = 16;
            else attackEffectRenderer.sortingOrder = originalSortingOrder;
            attackEffect.SetActive(true);
        }
        yield return new WaitForSeconds(castTime);
        if (playerScript != null)
        {
            Vector2 directionToPlayer = (playerScript.transform.position - transform.position).normalized;
            playerScript.StartKnockback(stunDuration);
            playerScript.GetComponent<Rigidbody2D>().AddForce(directionToPlayer * launchForce + Vector2.up * liftForce, ForceMode2D.Impulse);
        }
        yield return new WaitForSeconds(attackAnimationDuration);
        if (attackEffect != null) { attackEffect.SetActive(false); attackEffectRenderer.sortingOrder = originalSortingOrder; }
        if (cooldownIndicator != null) cooldownIndicator.SetActive(true);
        yield return new WaitForSeconds(cooldownTime);
        if (cooldownIndicator != null) cooldownIndicator.SetActive(false);
        SetAsDynamic();
        isAttacking = false;
        nextAttackTime = Time.time + 0.1f;
    }
}