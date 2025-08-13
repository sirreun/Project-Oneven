using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//source: https://youtu.be/rJqP5EesxLk?si=MABhf12EGIqK84uM

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;

    private PlayerManager playerManager;

    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        playerManager = GetComponent<PlayerManager>();

        // Callback context to call Jump function
        onFoot.Jump.performed += ctx => playerManager.Jump();

        // Callback context to call Crouch function
        onFoot.Crouch.performed += ctx => playerManager.Crouch();

        // Callback context to call Sprint function
        onFoot.Sprint.performed += ctx => playerManager.Sprint();

        // Callback context to call DropItem function
        onFoot.DropItem.performed += ctx => playerManager.DropItem();

        // Callback context to navigate inventory
        onFoot.InventoryItem1.performed += ctx => InventoryManager.instance.ChangeCurrentInventorySlotWithNumber(1);
        onFoot.InventoryItem2.performed += ctx => InventoryManager.instance.ChangeCurrentInventorySlotWithNumber(2);
        onFoot.InventoryItem3.performed += ctx => InventoryManager.instance.ChangeCurrentInventorySlotWithNumber(3);
        onFoot.InventoryItem4.performed += ctx => InventoryManager.instance.ChangeCurrentInventorySlotWithNumber(4);
        onFoot.InventoryItem5.performed += ctx => InventoryManager.instance.ChangeCurrentInventorySlotWithNumber(5);

        // Callback context to interact with a held item
        onFoot.ItemInteract.performed += ctx => InventoryManager.instance.ItemInteract();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Tell the player manager to move using the value from the movement input action.
        playerManager.ProcessMove(onFoot.Movement.ReadValue<Vector2>());

        // Tell the player manager to navigate inventory UI from the navigateUI input action
        playerManager.ProcessScroll(onFoot.NavigateInventory.ReadValue<Vector2>());

    }

    void LateUpdate()
    {
        // Tell the player manager to look using the value from the look input action.
        playerManager.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        onFoot.Enable();
    }
    private void Disable()
    {
        onFoot.Disable();
    }

    /// Returns a bool representing if Interact has been triggered.
    public bool InteractTriggered()
    {
        return playerInput.OnFoot.Interact.triggered;
    }

}
