using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameItem : MonoBehaviour
{
    public int itemID;
    public string itemName;
    public int itemClass = 1; // uses numbers 1-7
    public int itemMass;
}
