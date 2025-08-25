using UnityEngine;
using UnityEngine.Rendering;

public class MainCameraTest : MonoBehaviour
{
    Portal[] portals;

    void LateUpdate()
    {
        // Alternative: Call before rendering each frame
        Debug.Log("LateUpdate called - backup method");
        // Your portal code here
    }

    // Or use Unity's render pipeline events:
    void OnEnable()
    {
        Camera.onPreCull += HandlePreCull;
    }

    void OnDisable()
    {
        Camera.onPreCull -= HandlePreCull;
    }

    void HandlePreCull(Camera cam)
    {
        if (cam == GetComponent<Camera>())
        {
            Debug.Log("Camera.onPreCull event fired!");
            // Your portal code here
        }
    }
}
