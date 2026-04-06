using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCam : MonoBehaviour
{
    public float flySpeed = 10f;
    public float lookSensitivity = 0.1f;

    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference verticalAction;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Update()
    {
        // obrót
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        rotationX -= lookInput.y * lookSensitivity;
        rotationY += lookInput.x * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);

        // ruch
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float verticalInput = verticalAction.action.ReadValue<float>();

        Vector3 direction = transform.forward * moveInput.y + transform.right * moveInput.x + Vector3.up * verticalInput;
        transform.position += direction * flySpeed * Time.deltaTime;
    }

    public void SetPosition(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
        rotationX = rot.eulerAngles.x;
        rotationY = rot.eulerAngles.y;
    }
}