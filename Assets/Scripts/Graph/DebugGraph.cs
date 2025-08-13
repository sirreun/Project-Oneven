using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeMonkey.Utils;

public class DebugGraph : MonoBehaviour
{
    [SerializeField] private Sprite circleSprite;
    private RectTransform graphContainer;
    private const float maxRadiation = 243f;
    public int maxReadings = 100; // TODO: Should be determined by graphContainer size
    private int numberOfReadings = 0;
    private List<float> data = new List<float>();
    private float graphHeight;
    private float graphLength;
    public bool ShowDebugGraph = false;

    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        graphHeight = graphContainer.sizeDelta.y;
        graphLength = graphContainer.sizeDelta.x;

        //CreateCircle(new Vector2(200,200));
    }

    private void Update()
    {
        

        // Updates the graph based on the new values given.
        // Remove all old points.
        foreach (Transform dataPoint in graphContainer)
        {
            if (dataPoint.gameObject.tag == "dataPoint")
            {
                //Debug.Log("Destroying " + dataPoint.gameObject.name);
                Destroy(dataPoint.gameObject);
            }
        }

        if (!ShowDebugGraph)
        {
            return;
        }
        
        GameObject lastCalledDataPoint = null;
        for (int i = 0; i < data.Count; i++)
        {
            GameObject newDataPoint = CreateCircle(new Vector2(i, data[i])); // normalize x numbers: (i/maxReadings) * graphLength?
            //TODO: put normalizing y values ^here incase the graph size changes? if needed
            if (lastCalledDataPoint != null)
            {
                // Not the first point, can make a connection
                // TODO: fix bug where position/transform is incorrect (position and length are static for some reason)
                //CreateDotConnection(lastCalledDataPoint.GetComponent<RectTransform>().anchoredPosition, newDataPoint.GetComponent<RectTransform>().anchoredPosition);
            }
            lastCalledDataPoint = newDataPoint;
            
        }
    }

    private GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject("circle", typeof(Image));
        gameObject.transform.gameObject.tag = "dataPoint";
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(1, 2); // Data point size
        rectTransform.anchorMin = new Vector2 (0, 0);
        rectTransform.anchorMax = new Vector2 (0, 0);

        //Debug.Log("DebugGraph: Added point at (" + anchoredPosition.x + ", " + anchoredPosition.y + ").");
        return gameObject;
    }

    public void AddData(float rawData)
    {
        numberOfReadings += 1;
        if (numberOfReadings >= maxReadings)
        {
            numberOfReadings = maxReadings;
            // Remove first item from the data list.
            data.RemoveAt(0);
        }
        //xPosition = numberOfReadings;
        float yPosition = (rawData / maxRadiation) * graphHeight; // Normalizes data point to graph container

        data.Add(yPosition);
    }

    private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
    {
        GameObject gameObject = new GameObject("dotConnection", typeof(Image));
        gameObject.transform.gameObject.tag = "dataPoint";
        gameObject.transform.SetParent(graphContainer, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 direction = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);
        rectTransform.anchorMin = new Vector2 (0, 0);
        rectTransform.anchorMax = new Vector2 (0, 0);
        rectTransform.sizeDelta = new Vector2(100, 1); 
        rectTransform.position = dotPositionA + (direction * distance * 0.5f);
        rectTransform.localEulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(direction));

    }
}
