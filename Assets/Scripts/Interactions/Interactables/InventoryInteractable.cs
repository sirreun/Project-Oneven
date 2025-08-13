using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


///Intructions: set object layer to interactable
/// Make sure item has both InventoryInteractable and InventoryItem

public class InventoryInteractable : Interactable
{
    [Tooltip("The function called when pressing Q and holding this item.")]
    public UnityEvent[] itemInteractFunctions;
    
    public bool itemOn {get; private set;}
    private float powerLevel = 100; // Current power the device has. If the item is not powered, this number will not change.
    private float rateOfPowerDrain = 20; // Per real time minute
    private System.TimeSpan timeDelta; 
    private System.DateTime startTime; 
    private double parsedTime; // Unit: minutes

    void Start()
    {
        itemOn = false;
    }

    // Overrides the interact function from the Interactable base class.
    protected override void Interact()
    {
        // Add item to inventory through the Inventory Manager
        // Hide the physical object
        if(InventoryManager.instance.AddItemToInventory(this.gameObject))
        {
            Debug.Log("Added " + gameObject.name + " to inventory.");
            //this.gameObject.SetActive(false); //make item not visible (it will be moved by inventory manager to the player's hand)
            Destroy(gameObject);
        }
    }

    // Item interact function that generally is used to turn on the item
    public void ItemInteract()
    {
        foreach (UnityEvent itemInteractFunction in itemInteractFunctions)
        {
            itemInteractFunction.Invoke();
        }
    }

    // Toggles the item on and off if it has enough power.
    public void ItemOn()
    {
        if (powerLevel > 0)
        {
            itemOn = !itemOn;

            if (itemOn)
            {
                // Item is being turned on.
                startTime = System.DateTime.Now;
            }
            else
            {
                // Item is being turned off.
                timeDelta = System.DateTime.Now.Subtract(startTime);
            }
        }
        else
        {
            itemOn = false;
        }
    }

    void FixedUpdate()
    {
        if (itemOn)
        {
            parsedTime = timeDelta.TotalMinutes + System.DateTime.Now.Subtract(startTime).TotalMinutes;
            
            if (parsedTime > 1f)
            {
                // A minute has passed, reduce power
                ChangePowerLevel(-1 * rateOfPowerDrain);
                startTime = System.DateTime.Now; // reset start time
                //parsedTime = 0f; // reset time parsed
            }
            //Debug.Log(this.gameObject.name + " power level is " + powerLevel);
        }
    }

    private void ChangePowerLevel(float value)
    {
        powerLevel += value;
    }
}
