using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public float speed = 3.0f;
    public float velocidadGiro = 540f;

    // La Candileja (u otros scripts) bloquean el movimiento con esto
    public static bool puedeMoverse = true;

    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        puedeMoverse = true; // por si quedó bloqueado de una partida anterior
    }

    void Update()
    {
        // Si está bloqueado, el personaje se queda quieto en Idle
        if (!puedeMoverse)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direccion = new Vector3(horizontal, 0.0f, vertical);

        if (direccion.magnitude > 0.1f)
        {
            transform.position += direccion.normalized * speed * Time.deltaTime;

            Quaternion destino = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, destino, velocidadGiro * Time.deltaTime);
        }

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Clamp01(direccion.magnitude));
    }

    void OnDisable()
    {
        puedeMoverse = true;
    }
}