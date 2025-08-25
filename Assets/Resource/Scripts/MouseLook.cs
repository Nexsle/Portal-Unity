using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float mouseSens = 100f;
    [SerializeField] float speed = 20f;
    public Transform playerBody;
    InputAction lookAction;
    InputAction moveAction;
    private float xRotation = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        moveAction = InputSystem.actions.FindAction("Move");

        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            playMove(moveValue);
        }

        Vector2 lookValue = lookAction.ReadValue<Vector2>();
        if (lookValue != Vector2.zero)
        {
            mouseMove(lookValue);   
        }
    }
    private void mouseMove(Vector2 lookValue)
    {
        float mouseX = lookValue.x * mouseSens * Time.deltaTime;
        float mouseY = lookValue.y * mouseSens * Time.deltaTime;

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    }

    private void playMove(Vector2 moveValue)
    {
        
    }
}

