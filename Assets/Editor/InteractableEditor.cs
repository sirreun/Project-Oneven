using UnityEditor;

// source: https://youtu.be/_UIiwzfZoZA?si=Z6SLicH814lWWRqd

[CustomEditor(typeof(Interactable), true)]
public class InteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Interactable interactable = (Interactable)target; //target is the current object being inspected
        base.OnInspectorGUI();

        if (target.GetType() == typeof(EventOnlyInteractable))
        {
            interactable.interactionPrompt = EditorGUILayout.TextField("Interaction Prompt", interactable.interactionPrompt);
            EditorGUILayout.HelpBox("Event Only Interactable. Can only use Unity Events.", MessageType.Info);

            if (interactable.GetComponent<InteractionEvent>() == null)
            {
                interactable.useEvents = true;
                interactable.gameObject.AddComponent<InteractionEvent>();
            }
        }
        else
        {
            if (interactable.useEvents)
            {
                if (interactable.GetComponent<InteractionEvent>() == null)
                {
                    interactable.gameObject.AddComponent<InteractionEvent>();
                }
            }
            else
            {
                if (interactable.GetComponent<InteractionEvent>() != null)
                {
                    DestroyImmediate(interactable.GetComponent<InteractionEvent>());
                }
            }
        }
        
    }
}
