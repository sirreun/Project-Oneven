using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//source: https://youtu.be/rJqP5EesxLk?si=MABhf12EGIqK84uM

public class PlayerManager : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    
    [Header("Player Movement")]
    public float playerSpeed = 3f;
    public float gravity = -9.8f;
    public float jumpHeight = 1.5f;
    private int jumpXDirection;
    private int jumpZDirection;

    [Header("Player Look")]
    public Camera cam;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;
    private float xRotation = 0f;

    [Header("Player Crouch")]
    public float crouchTimer = 0f;
    private bool lerpCrouch;
    private bool isCrouching = false;
    private float crouchSpeedReduction = 1.5f; // by how much player speed is subtracted when crouching
    private float crouchJumpReduction = 0.7f;

    private bool isSprinting = false;



    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;

        // Calls the implement crouch function
        if (lerpCrouch)
        {
            ImplementCrouch();
        }

    }

    /// Receive movement inputs for InputManager.cs and apply them to the character controller
    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        
        if (!isGrounded)
        {
            // Don't allow direction changes with WASD
            if (input.x > 0)
            {
                if (jumpXDirection < 0)
                {
                    input.x *= -1;
                }
            }
            else if (input.x < 0)
            {
                if (jumpXDirection > 0)
                {
                    input.x *= -1;
                }
            }

            if (input.y > 0)
            {
                if (jumpZDirection < 0)
                {
                    input.y *= -1;
                }
            }
            else if (input.y < 0)
            {
                if (jumpZDirection > 0)
                {
                    input.y *= -1;
                }
            }
        }
        
        // Translates 2D to 3D
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        

        controller.Move(transform.TransformDirection(moveDirection) * playerSpeed * Time.deltaTime);

        // Accounts for gravity
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    /// Receive jump inputs for InputManager.cs and apply them to the character controller
    public void Jump()
    {
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -1.5f * gravity); // TODO: figure out why times -3
        
        // Used for jump direction locking
        // TODO: fix this so that it actually works
        if (playerVelocity.x > 0)
        {
            jumpXDirection = 1;
        }
        else if (playerVelocity.x < 0)
        {
            jumpXDirection = -1;
        }

        if (playerVelocity.z > 0)
        {
            jumpZDirection = 1;
        }
        else if (playerVelocity.z < 0)
        {
            jumpZDirection = -1;
        }
    }

    /// Receive looking inputs for InputManager.cs and apply them to the character controller
    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        // Calculate the camera rotation for looking up and down
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80, 80); // has a min of -80 and max of 80

        // Apply the rotation to the camera transform
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // Rotate player to look left and right
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);

    }

    public void ProcessScroll(Vector2 input)
    {
        float direction = input.y;

        if (direction != 0)
        {
            InventoryManager.instance.ChangeCurrentInventorySlot(direction);
        }
    }

    private void ImplementCrouch()
    {
        crouchTimer += Time.deltaTime;
        float p = crouchTimer / 1;
        p *= p;

        if (isCrouching)
        {
            controller.height = Mathf.Lerp(controller.height, 1, p);
        }
        else
        {
            controller.height = Mathf.Lerp(controller.height, 2, p);
        }

        if (p > 1)
        {
            // reset
            lerpCrouch = false;
            crouchTimer = 0f;
        }
    }

    /// Toggles if the player is crouching
    public void Crouch()
    {
        isCrouching = !isCrouching;
        crouchTimer = 0f;
        lerpCrouch = true;
        
        if (isCrouching)
        {
            playerSpeed -= crouchSpeedReduction;
            jumpHeight -= crouchJumpReduction;

            // Stop sprint if sprinting
            if(isSprinting)
            {
                isSprinting = false;
                playerSpeed -= 3f;
            }
        }
        else
        {
            playerSpeed += crouchSpeedReduction;
            jumpHeight += crouchJumpReduction;
        }
    }

    /// Toggles if the player is sprinting
    public void Sprint()
    {
        isSprinting = !isSprinting;

        // Player cannot sprint while crouching: Unsure if this also needs to be here
        if (isCrouching)
        {
            isSprinting = false;
            return;
        }
        
        if (isSprinting)
        {
            playerSpeed += 3f;
        }
        else
        {
            playerSpeed -= 3f;
        }
    }

    public void DropItem()
    {
        InventoryManager.instance.RemoveCurrentItemFromInventory();
    }
}
