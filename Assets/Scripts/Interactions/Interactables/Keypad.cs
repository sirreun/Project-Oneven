using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//source: https://youtu.be/gPPGnpV1Y1c?si=B4UcHoH_56jL7_G9 Template Method Pattern 
//Intructions: set object layer to interactable

public class Keypad : Interactable
{
    // The gameobject affected when the keypad is used.
    [SerializeField] private GameObject affectedGameObject;
    private bool isGreen = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Overrides the interact function from the Interactable base class.
    protected override void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);

        isGreen = !isGreen;
        if (isGreen)
        {
            affectedGameObject.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        }
        else
        {
            affectedGameObject.GetComponent<Renderer>().material.color = new Color(255, 255, 255);
        }
        
    }
}
