using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// This side of the item class deals with the funcitonality of the item when a player has it in inventory
/// TODO: connects to a gameobject that the player can hold
/// TODO: Has information about how you can interact with the object
public class InventoryItem : GameItem
{
    public bool canBeInHub = true;
    public bool canBeInField = true;

    public bool isHeavyItem = false; //unsure yet if this be separated through subclasses rather than a bool

    public Sprite itemIcon;

    public Vector3 itemScale;
    public Vector3 heldItemScale;

}
