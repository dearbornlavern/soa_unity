using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using soa;

public class SoaClassifier : MonoBehaviour
{
    // Unity exposed parameters
    public PerceptionModality[] modes;

    // Reference to soaActor
    SoaActor thisSoaActor;

    // Use this for initialization
    void Start()
    {
        // Save reference to my SoaActor
        thisSoaActor = gameObject.GetComponentInParent<SoaActor>();
    }

    public void UpdateClassifications(List<GameObject> detections)
    {
        SoaActor targetActor;
        foreach (GameObject target in detections)
        {
            try
            {
                // Save pointer to target's SoaActor
                targetActor = target.GetComponent<SoaActor>();

                // The object being detected must be alive, only classify if we haven't already
                if (targetActor.isAlive && !thisSoaActor.checkClassified(targetActor.unique_id))
                {
                    updateClassificationFor(target, targetActor);
                }
            }
            catch (Exception e)
            {
                Log.error(e.ToString());
            }
        }
    }

    private void updateClassificationFor(GameObject target, SoaActor targetActor)
    {
        // Loop through all possible classifier modes
        foreach (PerceptionModality mode in modes)
        {
            // Compute slant range in km
            float slantRange = Mathf.Sqrt(
                ((transform.position.x - target.transform.position.x) / SimControl.KmToUnity) * ((transform.position.x - target.transform.position.x) / SimControl.KmToUnity) +
                (thisSoaActor.simAltitude_km - targetActor.simAltitude_km) * (thisSoaActor.simAltitude_km - targetActor.simAltitude_km) + // Recall that altitude is kept track of separately
                ((transform.position.z - target.transform.position.z) / SimControl.KmToUnity) * ((transform.position.z - target.transform.position.z) / SimControl.KmToUnity)
            );

            // If the game object matches its intended target
            if (mode.tagString == target.tag)
            {
                // Determine whether it was classified
                if (slantRange <= mode.RangeP1)
                {
                    // Successful kill, 100% probabiltiy
                    thisSoaActor.setClassified(targetActor.unique_id);
                }
                else if (slantRange <= mode.RangeMax)
                {
                    // Probability of being classified    
                    if (UnityEngine.Random.value < (mode.RangeMax - slantRange) / (mode.RangeMax - mode.RangeP1))
                    {
                        // Target was unlucky and was classified
                        thisSoaActor.setClassified(targetActor.unique_id);
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update() { }
}
