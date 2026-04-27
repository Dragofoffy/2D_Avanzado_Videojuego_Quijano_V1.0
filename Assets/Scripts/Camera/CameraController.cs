using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; // Arrastra tu jugador aquí
    public float smoothTime = 0.3f; // Tiempo de suavizado (menor = más rápido)
    private Vector3 velocity = Vector3.zero;

    void LateUpdate() // LateUpdate es vital para cámaras, se ejecuta después del movimiento
    {
        if (player == null) return;

        // Definir posición objetivo (manteniendo la Z de la cámara original)
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);

        // Suavizar el movimiento
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}