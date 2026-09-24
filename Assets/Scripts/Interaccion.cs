using UnityEngine;

public class Interaccion : MonoBehaviour
{

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            float rangoInteraccion = 2f;
            Collider[] colliderArray = Physics.OverlapSphere(transform.position, rangoInteraccion);
            foreach (Collider collider in colliderArray)
            {
                if (collider.TryGetComponent(out InteraccionNPC interaccionNpc))
                {
                    interaccionNpc.Interaccionn();
                }
            }
        }


    }

}
