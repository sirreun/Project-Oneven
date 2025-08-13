using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    // The distance from which a player can interact with an interactable.
    [SerializeField] private float distance = 3f;
    // Determines which layer(s) the raycast recognizes; in this case, the Interactable layer.
    [SerializeField] private LayerMask mask;
    private PlayerUI playerUI;
    private InputManager inputManager;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<PlayerManager>().cam;
        playerUI = GetComponent<PlayerUI>();
        inputManager = GetComponent<InputManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Interactable UI is cleared when not looking at an interactable.
        playerUI.UpdateText(string.Empty);
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        //Debug.DrawRay(ray.origin,ray.direction * distance);
        
        // Stores the collision information.
        RaycastHit hitInformation;
        // Only continues to if statement if the ray hits something.
        if (Physics.Raycast(ray, out hitInformation, distance, mask))
        {
            if(hitInformation.collider.GetComponent<Interactable>() != null)
            {
                Interactable interactable = hitInformation.collider.GetComponent<Interactable>();
                playerUI.UpdateText(interactable.interactionPrompt);
                if (inputManager.InteractTriggered())
                {
                    interactable.BaseInteract();
                }
            }
        }
    }

    public Vector3 RaycastEndPoint(float distance)
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        return ray.GetPoint(distance);
    }
}
