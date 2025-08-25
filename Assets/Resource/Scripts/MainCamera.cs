using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public Portal[] portals;

    void Awake()
    {
        portals = FindObjectsOfType<Portal>();
        Debug.Log("CHECK THE PORTAL AMOUNT: " + portals.Length);
    }

    private void LateUpdate()
    {
        Debug.Log("CHECK IF PRECULL CALLED");
        for (int i = 0; i < portals.Length; i++)
        {
            portals[i].PrePortalRender();
        }

        for (int i = 0; i < portals.Length; i++)
        {
            portals[i].Render();
        }

        for (int i = 0; i < portals.Length; i++)
        {
            portals[i].PostPortalRender();
        }
    }
}
