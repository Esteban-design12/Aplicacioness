using UnityEngine;

public class Aviso : MonoBehaviour
{
    public GameObject textoInteraccion;
    public float distanciaInteraccion = 2f;

    private void Update()
    {
        Collider[] objetosCercanos = Physics.OverlapSphere(
            transform.position,
            distanciaInteraccion
        );

        bool hayInteractuable = false;

        foreach (Collider objeto in objetosCercanos)
        {
            if (objeto.CompareTag("Interactuable"))
            {
                hayInteractuable = true;
                break;
            }
        }

        textoInteraccion.SetActive(hayInteractuable);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, distanciaInteraccion);
    }
}