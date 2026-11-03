using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public float speed = 1.0f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direccion = new Vector3(horizontal, 0.0f, vertical);
        transform.Translate(direccion * Time.deltaTime * speed);

        // Le dice al Animator qué tan rápido te mueves
        animator.SetFloat("Speed", direccion.magnitude);
    }
}