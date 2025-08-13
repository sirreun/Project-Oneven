using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//source: https://youtu.be/gPPGnpV1Y1c?si=B4UcHoH_56jL7_G9 Template Method Pattern 

public abstract class Interactable : MonoBehaviour
{
    public bool useEvents;
    // The prompt that is displayed when the player looks at an interactable.
    public string interactionPrompt;
    
    public void BaseInteract()
    {
        if (useEvents)
        {
            GetComponent<InteractionEvent>().OnInteract.Invoke();
        }

        Interact();
    }

    protected virtual void Interact()
    {
        /// No code is written in this template function. It will be overridden by subclasses.
    }
}
