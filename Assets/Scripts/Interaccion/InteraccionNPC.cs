using UnityEngine;

public class InteraccionNPC : MonoBehaviour
{

    [SerializeField] private GameObject burbujaDialogo;

    private void Start()
    {
        burbujaDialogo.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            burbujaDialogo.SetActive(false);
        }
    }

    public void Interaccionn()
    {
        Debug.Log("Interaccion!");

        burbujaDialogo.SetActive(true);
    }
}
