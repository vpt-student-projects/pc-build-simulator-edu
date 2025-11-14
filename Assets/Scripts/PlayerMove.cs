using UnityEngine;

public class PlayerMove : MonoBehaviour 
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 7f;
    public float crouchHeight = 1f;
    
    private Rigidbody rb;
    private float verticalRotation = 0f;
    private bool isGrounded = false;
    private float originalHeight;
    
    public Transform cameraPivot;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        originalHeight = transform.localScale.y;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (cameraPivot == null)
            cameraPivot = transform.Find("CameraPivot");
    }
    
    void Update()
    {
        // Вращение камеры
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0f, mouseX, 0f);
        
        // ПРЫЖОК
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
        
        // ПРИСЕДАНИЕ
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(transform.localScale.x, originalHeight, transform.localScale.z);
        }
    }
    
    void FixedUpdate()
    {
        // Движение вперед-назад и влево-вправо
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = (transform.right * horizontal + transform.forward * vertical) * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }
    
    // Проверка касания земли
    void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}