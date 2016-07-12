using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using soa;

public class SoaSensor : MonoBehaviour 
{
    // Sensor attributes
    public float beamwidthDeg;
    public Vector3 boresightUnitVector;
    
    // Sensor internal bookkeeping
    public PerceptionModality[] modes;
    public SoaActor soaActor;
    public List<GameObject> possibleDetections;

	void Start () 
    {
        boresightUnitVector = new Vector3(0, -1, 0); // Pointed straight down by default
        soaActor = gameObject.GetComponentInParent<SoaActor>();
	}

	void Update () 
    {
	}

    // Cone beam antenna pattern
    public bool CheckSensorFootprint(GameObject target)
    {
        // Re-evaluate boresight if sensor is gimbaled (balloon)
        if (soaActor.type == (int)SoaActor.ActorType.BALLOON)
        {
            // Get the belief dictionary and SPOI Belief
            Belief_SPOI b = soaActor.Find<Belief_SPOI>(soa.Belief.BeliefType.SPOI, soaActor.unique_id);
            if (b != null)
            {
                // Extract SPOI (x,y,z) in sim coordinates [km]
                Vector3 spoi_km = new Vector3(
                    b.getPos_x(),
                    b.getPos_y(),
                    b.getPos_z());

                // Update the sensor boresight vector
                boresightUnitVector.x = spoi_km.x - transform.position.x / SimControl.KmToUnity; // [km]
                boresightUnitVector.y = spoi_km.y - soaActor.simAltitude_km; // [km]
                boresightUnitVector.z = spoi_km.z - transform.position.z / SimControl.KmToUnity; // [km]
                boresightUnitVector.Normalize();
            }
        }

        // Compute relative vector
        SoaActor targetActor = target.GetComponent<SoaActor>();
        Vector3 sensorToTargetUnitVector;
        sensorToTargetUnitVector.x = (target.transform.position.x - transform.position.x) / SimControl.KmToUnity; // [km]
        sensorToTargetUnitVector.y = targetActor.simAltitude_km - soaActor.simAltitude_km; // [km]
        sensorToTargetUnitVector.z = (target.transform.position.z - transform.position.z) / SimControl.KmToUnity; // [km]
        sensorToTargetUnitVector.Normalize();

        // Compute target cone angle relative to boresight 
        double coneAngleDeg = (180.0f / Mathf.PI) * Mathf.Acos(
            sensorToTargetUnitVector.x * boresightUnitVector.x +
            sensorToTargetUnitVector.y * boresightUnitVector.y +
            sensorToTargetUnitVector.z * boresightUnitVector.z);

        // Check if target is within beamwidthDeg/2 of boresight
        return coneAngleDeg <= (beamwidthDeg / 2);
    }

    public void CheckDetections(List<GameObject> targets)
    {
        SoaActor targetActor;

        foreach (GameObject target in targets)
        {
            // Get pointer to target's SoaActor
            targetActor = target.GetComponent<SoaActor>();

            // The object being detected must be alive and within the sensor's footprint
            if (targetActor.isAlive && CheckSensorFootprint(target))
            {
                // Loop through all possible detect modes
                foreach (PerceptionModality mode in modes)
                {
                    // Compute slant range in km
                    float slantRange = Mathf.Sqrt(
                        ((transform.position.x - target.transform.position.x) / SimControl.KmToUnity) * ((transform.position.x - target.transform.position.x) / SimControl.KmToUnity) + 
                        (soaActor.simAltitude_km - targetActor.simAltitude_km) * (soaActor.simAltitude_km - targetActor.simAltitude_km) + // Recall that altitude is kept track of separately
                        ((transform.position.z - target.transform.position.z) / SimControl.KmToUnity) * ((transform.position.z - target.transform.position.z) / SimControl.KmToUnity)
                        );

                    // Compute detection
                    if (mode.tagString == target.tag)
                    {
                        if (slantRange < mode.RangeMax)
                        {
                            if (slantRange < mode.RangeP1)
                            {
                                // Debug.Log(soaActor.name + " detects " + target.name + " at " + slantRange + "km");
                                LogDetection(target.gameObject);
                            }
                            else
                            {
                                if (Random.value < (mode.RangeMax - slantRange) / (mode.RangeMax - mode.RangeP1))
                                {
                                    // Debug.Log(soaActor.name + " detects " + target.name + " at " + slantRange + "km");
                                    LogDetection(target.gameObject);
                                }
                                else
                                {
                                    // Debug.Log(soaActor.name + " failed detect of " + target.name + " at " + slantRange + "km");
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public void logKill(SoaActor killedActor)
    {
        lock(soaActor.killDetections)
        {
            soaActor.killDetections.Add(new soa.Belief_Actor(
                killedActor.unique_id, (int)killedActor.affiliation, killedActor.type, false,
                killedActor.numStorageSlots, killedActor.numCasualtiesStored,
                killedActor.numSuppliesStored, killedActor.numCiviliansStored,
                killedActor.isWeaponized, killedActor.hasJammer, killedActor.fuelRemaining_s,
                killedActor.transform.position.x / SimControl.KmToUnity,
                killedActor.simAltitude_km,
                killedActor.transform.position.z / SimControl.KmToUnity));
        }
    }

    void LogDetection(GameObject detectedObject)
    {
        if (soaActor.Detections.IndexOf(detectedObject) == -1)
        {
            soaActor.Detections.Add(detectedObject);
        }
    }
}
