using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// TODO: Create a funtion to add a new source to the list after a scene has begun
// TODO: Add support for when a source blocking material is present

public class RadiationDetector : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask mask; // Should always be Radiation
    [SerializeField] private GameObject player; // Used to get one starting point
    private Vector3 playerPosition;

    public float noiseGeneration = 0.02f; // by default is 2%, how much noise is generated
    public float readingReliability = 85f; // by default is 85%, how reliable the detector readings are
    public float detectionDistance = 15f; // by default is 200
    private const float maxRadiationStrength = 243f;
    private float sourceIncrease = 1.05f; // How much a reading increases by when you are looking at it

    private float reading;

    private RadiationSource[] sources;

    public DebugGraph debugGraph;

    // TODO: add on and off functionality to detectors
    private bool radiationDetectorOn = true;
    
    // Start is called before the first frame update
    void Start()
    {
        // Retrieve all instances of RadiationSources and add to the sources list.
        sources = FindObjectsOfType<RadiationSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // Update if item is on:
        radiationDetectorOn = this.gameObject.GetComponent<InventoryInteractable>().itemOn;

        if (radiationDetectorOn)
        {
            GetRadiationReading();
        }

        DebugChangeColor();
        
    }

    private void GetRadiationReading()
    {
        reading = 0f; // reset value

        // Determine player distance from all radiation sources, and find which ones are near enough to the player.
        // Store the distance for reading output.
        float distance; 
        for (int i = 0; i < sources.Length; i++)
        {
            Vector3 sourcePosition = sources[i].GetPosition();
            playerPosition = player.transform.position;

            // 3D distance formula
            distance = Mathf.Pow(sourcePosition.x - playerPosition.x, 2) + Mathf.Pow(sourcePosition.y - playerPosition.y, 2) + Mathf.Pow(sourcePosition.z - playerPosition.z, 2);
            distance = Mathf.Sqrt(distance);

            // Check if detector would have picked up the source
            if (distance < detectionDistance)
            {
                //Debug.Log("In detection distance: " + distance);
                if (distance == 0f)
                {
                    reading += sources[i].radiationStrength;
                }
                else
                {
                    reading += sources[i].radiationStrength * (sources[i].radiationRadius + detectionDistance)/Mathf.Pow(distance, 3);
                }
                
                //Debug.Log("Originial Reading: " + reading);
            }
        }


        Ray ray = new Ray(cam.transform.position, cam.transform.forward); // TODO: maybe rotation shouldn't be the cam, and instead the detector item
        // Stores the collision information.
        RaycastHit hitInformation;

        // Only continues to if statement if the ray hits something. Use the ray to determine if the player is facing towards the source
        if (Physics.Raycast(ray, out hitInformation, detectionDistance, mask))
        {
            if(hitInformation.collider.GetComponent<RadiationSource>() != null)
            {
                // Get radiation reading
                RadiationSource source = hitInformation.collider.GetComponent<RadiationSource>();
                float totalDistance = source.radiationRadius + hitInformation.distance; // TODO: Could use a radition dignal manager to check how far the player is from radition sources... perhaos would work better

                // Determine the angle from which you are looking away from the source. Use this value to make the sourceIncrease modifier.
                // 3D distance formula
                Vector3 sourcePosition = source.GetPosition();
                distance = Mathf.Pow(sourcePosition.x - playerPosition.x, 2) + Mathf.Pow(sourcePosition.y - playerPosition.y, 2) + Mathf.Pow(sourcePosition.z - playerPosition.z, 2);
                distance = Mathf.Sqrt(distance);
                
                float height = Mathf.Pow(distance, 2) - Mathf.Pow(hitInformation.distance, 2);
                float maxHeight = source.radiationRadius;
                if (height == 0f)
                {
                    sourceIncrease = 1f;
                }
                else
                {
                    sourceIncrease = height/Mathf.Pow(maxHeight, 2) + 1f; // could change back to maxHeight *4 instead of maxHeight^2
                }
                

                float signalStrength = sourceIncrease * (source.radiationStrength/maxRadiationStrength);

                //reading = source.radiationStrength * (totalDistance/(source.radiationRadius + detectionDistance));

                if (Random.Range(1f, 100f) > readingReliability)
                {
                    // Unreliable reading, modify based on noise generation
                    float noise = Random.Range(1f - noiseGeneration, 1f + noiseGeneration);
                    reading = reading * noise * signalStrength;
                }

            }
        }
        else
        {
            // Adds random reading noise
            if (Random.Range(1f, 100f) > readingReliability)
            {
                // Unreliable reading, modify based on noise generation
                float noise = Random.Range(1f - noiseGeneration, 1f + noiseGeneration);
                reading = reading * noise;
            }
            
        }

        if (reading > 243f)
        {
            float noise = 1f;
            if (Random.Range(1f, 100f) > readingReliability)
            {
                // Unreliable reading, modify based on noise generation
                noise = Random.Range(1f - (noiseGeneration/2f), 1f);
                reading = reading * noise;
            }
            reading = 243f * noise;
        }

        //Debug.Log("Radiation Reading: " + reading);
        debugGraph.AddData(reading);
    }

    private void DebugChangeColor()
    {
        var _renderer = this.gameObject.GetComponent<Renderer>();
        if (radiationDetectorOn)
        {
            _renderer.material.color = Color.green;
        }
        else
        {
            _renderer.material.color = Color.gray;
        }
    }
}
