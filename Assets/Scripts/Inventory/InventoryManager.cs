using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// Stores information about what items the player holds in their inventory, as well as manages dropping and picking up items
/// Note that this for player inventory, and not the hub inventory.
/// This inventory is only used while playing the game, and doesn't need to be saved ( TODO: except for reconnecting after a disconnect)

/// Instructions: InventoryManager.instance.FunctionNameGoesHere()

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance { get; private set; }

    private List<GameObject> inventory = new List<GameObject>();
    // Converses with PlayerManager through controls on what the current inventory slot is
    private int currentInventorySlot = 1; // Ranges from 1 to 5. Defaults to slot one. NOTE: may have specialized slots, ex. heavy items are always placed in slot one
    private int maxInventoryItems = 5; // The maximum number of items a player can hold.
    [SerializeField] private GameObject inventoryObject;
    private Vector3 itemRotation = new Vector3(-6f, 5f, -1f);
    private Vector3 playerHandPosition = new Vector3(0f,0f,0f);
    private float infrontOfPlayerModifier = 1f;
    [SerializeField] private GameObject player;

    [Header("Inventory UI")]
    [SerializeField] private List<GameObject> itemUI; // Note that if the max number of item slots is changed that this will need to be done manually.

    // Start is called before the first frame update
    void Awake()
    {
        //Makes and manages the instance for InventoryManager
        if (instance != null)
        {
            Debug.Log("Inventory Manager: found more than one DPM in the scene, the newest DPM will be destroyed");
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        InitializeInventoryUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //TODO: add in inventory traversal

    //TODO: make object disappear and attach to player
    public bool AddItemToInventory(GameObject itemObject)
    {
        if(itemObject == null)
        {
            Debug.LogWarning("Item " + itemObject.gameObject.name + " does not have an InventoryItem script attached to it. Please add it.");
            UpdateInventoryText();
            return false;
        }

        if (inventory.Count < maxInventoryItems)
        {
            //Note: this makes it so that the newest object always is added at the end, which means that when an item is dropped everything is moved up one
            playerHandPosition = player.transform.Find("PlayerHand").transform.position;
            /// Create new object that is a child of inventoryObject
            GameObject newItem = Instantiate(itemObject, playerHandPosition, Quaternion.identity, inventoryObject.transform);
            
            // Remove physics from item in hand.
            Destroy(newItem.GetComponent<Rigidbody>());

            // Make item smaller (may need to add by how much smaller into the InventoryItem class)
            newItem.transform.localScale = newItem.GetComponent<InventoryItem>().heldItemScale;

            // Only show item if currently on the item slot
            if (currentInventorySlot == (inventory.Count + 1))
            {
                newItem.SetActive(true);
            }
            else
            {
                newItem.SetActive(false);
            }
            
            inventory.Add(newItem);
            // Rotate the object in player's hand
            newItem.transform.Rotate(itemRotation);
            
            UpdateInventoryText();
            return true;
        }
        else
        {
            //TODO: Warn player that their inventory is full
            Debug.LogWarning("Inventory is full, cannot pick up item.");
            UpdateInventoryText();
            return false;
        }
    }

    // TODO: unattach object from player
    public void RemoveCurrentItemFromInventory()
    {
        if (currentInventorySlot > 0 && currentInventorySlot <= maxInventoryItems)
        {
            // Only drop items and not empty slots.
            if (inventory.Count >= currentInventorySlot && inventory.Count != 0)
            {
                Debug.Log("Dropping Item...");
                // Remove item from inventory list
                string droppedObjectName = inventory[currentInventorySlot - 1].gameObject.name; 

                // Drop object infront of player
                Vector3 newPosition = player.GetComponent<PlayerInteract>().RaycastEndPoint(infrontOfPlayerModifier);

                // Spawn in dropped item
                GameObject newItem = Instantiate(inventory[currentInventorySlot - 1].gameObject, newPosition, Quaternion.identity);

                newItem.SetActive(true);

                // Add gravity to item
                Rigidbody _rb = newItem.AddComponent<Rigidbody>();
                _rb.mass = newItem.GetComponent<InventoryItem>().itemMass;

                // Return item to correct scale
                newItem.transform.localScale = newItem.GetComponent<InventoryItem>().itemScale;

                Destroy(inventory[currentInventorySlot - 1]);
                inventory.RemoveAt(currentInventorySlot - 1);
                Debug.Log("Dropped item " + droppedObjectName + ".");
            }
            
        }

        UpdateInventoryText();
    }

    // Looks up the given itemID in the item database
    // NOTE: may be better to move this to the ItemManager Class?
    private InventoryItem ItemLookup(int itemID)
    {
        //TODO: add functionality after making item info window
        Debug.Log("ItemLookup not yet implemented.");
        return new InventoryItem();
    }

    /// Given -1 or +1 determines if we are adding or subtracting the current inventory slot.
    /// Returns the new current inventory slot.
    /// Only works with navigationUI (and not the number keys)
    public int ChangeCurrentInventorySlot(float direction)
    {
        //Debug.Log("Scroll Direction: " + direction);
        if (direction == 0)
        {
            Debug.Log("InventoryManager.cs: ChangeCurrentInventorySlot: direction given is 0, which is useless.");
            return -1;
        }
        else if (direction > 0)
        {
            // Scrolls Down.
            // Increases current inventory slot by one.
            if (currentInventorySlot == maxInventoryItems)
            {   
                SelectItem(1);
                currentInventorySlot = 1;
            }
            else
            {
                SelectItem(currentInventorySlot + 1);
                currentInventorySlot += 1;
            }
        }
        else
        {
            // Scrolls Up.
            // Decreases current inventory slot by one.
            if (currentInventorySlot == 1)
            {
                SelectItem(maxInventoryItems);
                currentInventorySlot = maxInventoryItems;
            }
            else
            {
                SelectItem(currentInventorySlot - 1);
                currentInventorySlot -= 1;
            }
        }

        //Debug.Log("Current inventory slot is No. " + currentInventorySlot);
        return currentInventorySlot;
    }

    /// Changes inventory slot based on the inputed number.
    public int ChangeCurrentInventorySlotWithNumber(int itemSlot)
    {
        if (itemSlot != currentInventorySlot)
        {
            SelectItem(itemSlot);
            currentInventorySlot = itemSlot;
        }
        
        return currentInventorySlot;
    }

    /// Moves the aquired object to the player to display it if the player is holding the object 
    /// (Does this by destroying the old one and spawning a new one?)
    private void MoveInventoryObjectToPlayer(GameObject itemObject)
    {

    }


    /// INVENTORY UI ANIMATOR
    private void InitializeInventoryUI()
    {
        currentInventorySlot = 1;

        // Select first item slot.
        itemUI[0].transform.GetChild(0).gameObject.SetActive(false);
        itemUI[0].transform.GetChild(1).gameObject.SetActive(true);

        // Only shows selected item
        if (inventory.Count >= 1)
        {
            inventory[0].SetActive(true);
        }

        // Deselect other item slots.
        for (int i = 1; i < maxInventoryItems; i++)
        {
            itemUI[i].transform.GetChild(0).gameObject.SetActive(true);
            itemUI[i].transform.GetChild(1).gameObject.SetActive(false);

            // Hides unselected items
            if (inventory.Count >= i)
            {
                inventory[i].SetActive(false);
            }
        }


        UpdateInventoryText();
    }

    /// Instructions: Must be called before cuttenInventorySlot is changed.
    private void SelectItem(int inventorySlot)
    {
        //Debug.Log("new inventory slot: " + inventorySlot + ", old inventory slot: " + currentInventorySlot);
        // Select new slot
        itemUI[inventorySlot - 1].transform.GetChild(0).gameObject.SetActive(false);
        itemUI[inventorySlot - 1].transform.GetChild(1).gameObject.SetActive(true);
        
        // Shows item (TODO: add switch item animation)
        if (inventory.Count >= inventorySlot && inventorySlot != 0)
        {
            inventory[inventorySlot - 1].SetActive(true);
        }
        

        // Deselect old slot
        itemUI[currentInventorySlot - 1].transform.GetChild(0).gameObject.SetActive(true);
        itemUI[currentInventorySlot - 1].transform.GetChild(1).gameObject.SetActive(false);

        if (inventory.Count >= currentInventorySlot && inventory.Count != 0)
        {
            inventory[currentInventorySlot - 1].SetActive(false);
        }
    }

    /// Updates the text in the inventory.
    /// Is called whenever an item is added or removed from the inventory, or when Awake.
    private void UpdateInventoryText()
    {
        Debug.Log("Updating inventory text.");
        int count = inventory.Count; // Only works since new items are always added to the bottom of the list, and when removed the list moves up

        for (int i = 0; i < maxInventoryItems; i++)
        {
            if (i < count)
            {
                itemUI[i].transform.Find("InventoryText").gameObject.GetComponent<TextMeshProUGUI>().text = inventory[i].gameObject.GetComponent<InventoryItem>().itemName;
            }
            else
            {
                itemUI[i].transform.Find("InventoryText").gameObject.GetComponent<TextMeshProUGUI>().text = "--";
            }
        }
    }

    // Calls the item interact function for the currently held object
    public void ItemInteract()
    {
        if (inventory.Count >= currentInventorySlot)
        {
            inventory[currentInventorySlot - 1].GetComponent<InventoryInteractable>().ItemInteract();
        }
    }
}
