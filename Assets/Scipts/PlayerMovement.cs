using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    private Rigidbody rb;
    private Transform cameraTransform;
    private InputSystem_Actions inputActions;

    void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.Enable();
        inputActions.Player.Interact.Enable();
        inputActions.Player.Interact.performed += OnInteractPerformed;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.y + camRight * input.x;

        if (moveDir != Vector3.zero)
        {
            Vector3 moveAmount = moveDir.normalized * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveAmount);
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            Quaternion smoothRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothRotation);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Interact performed");
        TryInteract();
    }

    private void TryInteract()
    {
        float interactRange = 2f;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactRange))
        {
            var interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }

    void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractPerformed;
    }
}
