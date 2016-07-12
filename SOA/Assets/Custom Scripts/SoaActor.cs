using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using soa;
using Gamelogic.Grids;


public class SoaActor : MonoBehaviour, ISoaActor
{

    public int unique_id;
    public Affiliation affiliation;
    public int type;

    //Hex Cell Movement logging
    private SoaHexWorld hexGrid;
    private FlatHexPoint currentCell;
    private FlatHexPoint prevHexGridCell;
   

    // Simulated altitude [km]
    public float simAltitude_km;
    private float desiredAltitude_km;

    //Simulated position [km]
    protected float simX_km;
    protected float simZ_km;

    // Kinematic constraints
    public float minAltitude_km;
    public float maxAltitude_km;
    public float maxVerticalSpeed_m_s;

    private int[] idArray = new int[1];

    public enum ActorType
    {
        //BASE = 0, // Blue base is no longer a SoaActor, it is now a SoaSite
        SMALL_UAV = 1,
        HEAVY_UAV = 2,
        DISMOUNT = 3,
        TRUCK = 4,
        POLICE = 5,
        BALLOON = 6
    };

    public bool isAlive = false;
    private bool broadcastDeathNotice = false;
    public UInt32 numStorageSlots;
    public UInt32 numCasualtiesStored;
    public UInt32 numSuppliesStored;
    public UInt32 numCiviliansStored;
    public bool isWeaponized;
    public bool hasJammer;
    public float fuelRemaining_s;
    public float commsRange_km;

    public SoaSensor[] Sensors;
    public SoaWeapon[] Weapons;
    public SoaJammer jammer;
    public SoaClassifier[] Classifiers;
    public List<GameObject> Detections;
    public List<GameObject> Tracks;

    public List<Belief_Actor> killDetections = new List<Belief_Actor>();

    public DataManager dataManager;
    public bool simulateMotion;
    public bool useExternalWaypoint;
    public bool displayTruePosition = true;

    public Vector3 displayPosition;
    private Quaternion displayOrientation;

    private bool useGhostModel = false;
    private bool displayActor = true;

    public Dictionary<int, bool> classificationDictionary;

    protected BeliefRepository beliefRepo = new BeliefRepository(new ProtobufSerializer(), new SHA1_Hash());
    protected RepositoryState lastKnownState;

    private static System.DateTime epoch = new System.DateTime(1970, 1, 1);

    public SoldierWaypointMotion motionScript;
    public WaypointMotion wpMotionScript;
    public PlanarCoordinateMotion pcMotionScript;
    public NavMeshAgent navAgent;
    SimControl simControlScript;

    private float velocityX = 0;
    private float velocityY = 0;
    private float velocityZ = 0;

    private bool velocityXValid = false;
    private bool velocityYValid = false;
    private bool velocityZValid = false;

    private Communicator<int> communicator;

    NavMeshAgent nma;

	public int getID()
	{
		return unique_id;
	}

	public PositionKM getPositionInKilometers()
	{
		return new PositionKM(gameObject.transform.position.x / SimControl.KmToUnity,
							  simAltitude_km,
							  gameObject.transform.position.z / SimControl.KmToUnity);
	}

	public bool isBalloon()
	{
		return type == (int)SoaActor.ActorType.BALLOON;
	}

	public bool isBaseStation()
	{
		return this is SoaSite; //base station? Base Station?? THIS IS SOA SITE!!!
	}

	public float getCommsRangeKM()
	{
		return commsRange_km;
	}

	public BeliefRepository getRepository()
	{
		return beliefRepo;
	}

    // Use this for initialization
    void Start()
    {
        //Initialize id array for photon
        idArray[0] = unique_id;

        // Alive at the beginning
        isAlive = true;

        displayPosition = transform.position;
        displayOrientation = transform.rotation;

        Sensors = transform.GetComponentsInChildren<SoaSensor>();

        // Look at my children and populate list of classifiers
        Classifiers = transform.GetComponentsInChildren<SoaClassifier>();

        // Look at my children and populate list of weapons
        Weapons = transform.GetComponentsInChildren<SoaWeapon>();

        // An actor is weaponized if it has a weapon with at least one enabled mode
        foreach(SoaWeapon tempWeapon in Weapons){
            foreach (WeaponModality tempModality in tempWeapon.modes)
            {
                if (tempModality.enabled)
                {
                    isWeaponized = true;
                    break;
                }
            }
            if (isWeaponized)
            {
                break;
            }
        }

    	// Determine whether the actor has a jammer that is active
	    jammer = transform.GetComponentInChildren<SoaJammer>();
	    hasJammer = jammer != null && jammer.isOn;

        // Get references to my motion and nav scripts
        motionScript = gameObject.GetComponent<SoldierWaypointMotion>();
        wpMotionScript = gameObject.GetComponent<WaypointMotion>();
        pcMotionScript = gameObject.GetComponent<PlanarCoordinateMotion>();
        navAgent = gameObject.GetComponent<NavMeshAgent>();
        hexGrid = GameObject.Find("Grid").GetComponent<SoaHexWorld>();

        // Save reference to simControl
        simControlScript = GameObject.FindObjectOfType<SimControl>();
        
        Debug.Log("SoaActor: Initialized all beliefDictionaries for " + gameObject.name);

        // Initialize a new classification dictionary
        classificationDictionary = new Dictionary<int, bool>();

        // Initialize initial altitude to be average of min and max altitudes
        //simAltitude_km = 0.5f * (minAltitude_km + maxAltitude_km);
        //desiredAltitude_km = simAltitude_km;

        // Set to alive now, must be last thing done
        isAlive = true;
        nma = GetComponent<NavMeshAgent>();
	}

    SortedDictionary<int, Belief> lookup(Belief.BeliefType key, SortedDictionary<Belief.Key, SortedDictionary<int, Belief>> dictionary)
    {
        return lookup(Belief.keyOf(key), dictionary);
    }

    SortedDictionary<int, Belief> lookup(Belief.Key key, SortedDictionary<Belief.Key, SortedDictionary<int, Belief>> dictionary)
    {
        SortedDictionary<int, Belief> beliefs;
        if (!dictionary.TryGetValue(key, out beliefs))
        {
            beliefs = new SortedDictionary<int, Belief>();
            dictionary[key] = beliefs;
        }
        return beliefs;
    }

    // Update is called once per frame
    void Update() 
    {
        if (dataManager != null && communicator == null)
        {
            communicator = dataManager.physicalNetworkLayer.BuildCommunicatorFor(unique_id);
            communicator.RegisterCallback(handleBeliefReceivedFrom);
        } 

        if (isAlive)
        {
            // Compute time since last frame
            float dt_s = Time.deltaTime * 60; // Time.deltaTime returns sec, recall 1 sec real time = 60 sec in sim world
            
            // Simulate altitude state
            if (simAltitude_km != desiredAltitude_km)
            {
                float diffAltitude_km = desiredAltitude_km - simAltitude_km;
                if (Mathf.Abs(diffAltitude_km) <= maxVerticalSpeed_m_s / 1000.0f * dt_s)
                {
                    // We can achieve the desired altitude in this time step
                    simAltitude_km = desiredAltitude_km;
                }
                else if (desiredAltitude_km > simAltitude_km)
                {
                    // Move max speed up
                    simAltitude_km += maxVerticalSpeed_m_s / 1000.0f * dt_s;
                }
                else
                {
                    // Move max speed down
                    simAltitude_km -= maxVerticalSpeed_m_s / 1000.0f * dt_s;
                }
            }

            // Decrement fuel accordingly
            // Note: Fuel is measured in seconds of simulated time, not real time
            fuelRemaining_s = Mathf.Max(0.0f, fuelRemaining_s - dt_s);
            if (fuelRemaining_s <= 0)
            {
                // Killed by insufficient fuel
                Kill("Insufficient Fuel");
            }
        }
	}

    public void handleBeliefReceivedFrom(Belief b, int agentID)
    {
        beliefRepo.Commit(b);
    }

    // Set simulated altitude but enforce min/max constraints
    public void SetSimAltitude(float simAltitude_km)
    {
        this.simAltitude_km = Mathf.Max(Mathf.Min(simAltitude_km, maxAltitude_km), minAltitude_km);
    }

    // Set desired altitude but enforce min/max constraints
    public void SetDesiredAltitude(float desiredAltitude_km)
    {
        this.desiredAltitude_km = Mathf.Max(Mathf.Min(desiredAltitude_km, maxAltitude_km), minAltitude_km);
    }

    // Called when the actor has been killed
    public virtual void Kill(string killerName)
    {
        if (isAlive)
        {
            // Set that it is no longer alive
            isAlive = false;

            // Log event
            if (gameObject.CompareTag("HeavyUAV"))
            {
                simControlScript.soaEventLogger.LogHeavyUAVLost(gameObject.name, killerName);
            }
            else if (gameObject.CompareTag("SmallUAV"))
            {
                simControlScript.soaEventLogger.LogSmallUAVLost(gameObject.name, killerName);
            }
            

                // Convert position from Unity to km for Belief_Actor
                Belief_Actor newActorData = new Belief_Actor(
                    unique_id, (int)affiliation, type, isAlive, 
                    numStorageSlots, numCasualtiesStored,
                    numSuppliesStored, numCiviliansStored,
                    isWeaponized, hasJammer, fuelRemaining_s,
                    transform.position.x / SimControl.KmToUnity,
                    simAltitude_km,
                    transform.position.z / SimControl.KmToUnity);

                //addBeliefToUnmergedBeliefDictionary(newActorData);
                addMyBeliefData(newActorData);
                //addBeliefToBeliefDictionary(newActorData);
                //addBelief(newActorData, remoteBeliefs);


            // Don't move anymore
            simulateMotion = false;
            if (navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.enabled = false;
            }
            if (motionScript != null)
            {
                motionScript.enabled = false;
            }

            // TODO: Make sure dead unit does not act as relay for comms

            // Change appearance to look destroyed
            Vector3 destroyedPos = transform.position;
            destroyedPos.y = 0.85f;
            transform.position = destroyedPos;
        }
    }

    //Set this actors position and orientation
    public virtual void LateUpadte()
    {
        if (displayActor)
        {
            if (displayTruePosition)
            {
                //TODO turn on real model, destroy/turn off other models
            }
            else if(!useGhostModel)
            {
                //TODO spawn substitute model, turn off real model
            }else
            {
                //TODO spawn ghost model at display position, turn off real model
            }
        }
        else
        {
            //TODO set model not visisble
        }

    }

    /**
     * Update the position of the actor vectors.  This function is called when in simulation mode
     * If Sim is in charge of waypoints, get the current target from the motion script and add to 
     * belief map.  Otherwise get the waypointBelief from the dictionary and set as navAgent destination.
     * 
     * TODO create functions for when in client mode and only reading position data
     */ 
    public virtual void updateActor()
    {
        if (!isAlive)
        {
            return;
        }

        Belief myActor = null;
        displayTruePosition = true;

        if (myActor != null && myActor.getId() != unique_id)
        {
            Debug.LogError("Update Actor " + unique_id + " has invalid id " + myActor.getId());
            return;
        }

        if (nma != null)
        {
            velocityX = (nma.velocity.x / SimControl.KmToUnity) / 60f * 1000f;
            velocityXValid = true;
            velocityY = (nma.velocity.y / SimControl.KmToUnity) / 60f * 1000f;
            velocityYValid = true;
            velocityZ = (nma.velocity.z / SimControl.KmToUnity) / 60f * 1000f;
            velocityZValid = true;

            //Debug.Log("VELOCITY " + unique_id + " " + (nma.velocity.magnitude / 60f * 1000f));
        }

        if (simulateMotion)
        {
            displayActor = true;

            // Setup waypoint belief
            if (useExternalWaypoint)
            {
                Belief_Waypoint newWaypoint = beliefRepo.Find<Belief_Waypoint>(Belief.BeliefType.WAYPOINT, unique_id);
                //Debug.Log(name);
                motionScript.On = false;
                if (newWaypoint != null)
                {
                    // Received a waypoint in km, transform to Unity coordinates and then use it to set the nav agent's destination
                    if (navAgent != null)
                    {
                        navAgent.SetDestination(SimControl.ConstrainUnityDestinationToBoard(
                            new Vector3(
                                newWaypoint.getPos_x() * SimControl.KmToUnity,
                                transform.position.y, // Nav agent ignores the y coordinate (altitude)
                                newWaypoint.getPos_z() * SimControl.KmToUnity
                            )
                        ));
                    }
                    
                    // Set the desired altitude separately [km]
                    SetDesiredAltitude(newWaypoint.getPos_y());
                }
                else
                if(navAgent != null)
                {
                    // Stay put
                    navAgent.SetDestination(SimControl.ConstrainUnityDestinationToBoard(
                        new Vector3(transform.position.x, transform.position.y, transform.position.z)));
                }
            }
            else if (motionScript != null)
            {
                // Convert position coordinates from unity to km before making new belief waypoint
                Belief_Waypoint newWaypoint = new Belief_Waypoint((ulong)(System.DateTime.UtcNow - epoch).Milliseconds, unique_id,
                    motionScript.targetPosition.x / SimControl.KmToUnity,
                    desiredAltitude_km,
                    motionScript.targetPosition.z / SimControl.KmToUnity);
                addMyBeliefData(newWaypoint);
            }
            else if (wpMotionScript != null)
            {
                // Special case for blue balloon
                // Convert position coordinates from unity to km before making new belief waypoint
                Belief_Waypoint newWaypoint = new Belief_Waypoint((ulong)(System.DateTime.UtcNow - epoch).Milliseconds, unique_id,
                    wpMotionScript.targetPosition.x / SimControl.KmToUnity,
                    desiredAltitude_km,
                    wpMotionScript.targetPosition.z / SimControl.KmToUnity);
                addMyBeliefData(newWaypoint);
            }
            else
            {
                // Special case for blue balloon
                // Convert position coordinates from unity to km before making new belief waypoint
                Belief_Waypoint newWaypoint = new Belief_Waypoint((ulong)(System.DateTime.UtcNow - epoch).Milliseconds, unique_id,
                    pcMotionScript.targetPosition.x / SimControl.KmToUnity,
                    desiredAltitude_km,
                    pcMotionScript.targetPosition.z / SimControl.KmToUnity);
                addMyBeliefData(newWaypoint);
            }

            // Convert position from Unity to km for Belief_Actor
            simX_km = transform.position.x / SimControl.KmToUnity;
            simZ_km = transform.position.z / SimControl.KmToUnity;

            Belief_Actor newActorData = new Belief_Actor(
                unique_id, (int)affiliation, type, isAlive, 
                numStorageSlots, numCasualtiesStored,
                numSuppliesStored, numCiviliansStored,
                isWeaponized, hasJammer, fuelRemaining_s,
                simX_km, simAltitude_km, simZ_km,
                velocityXValid, velocityX,
                velocityYValid, velocityY,
                velocityZValid, velocityZ);
            
            addMyBeliefData(newActorData);

            if (dataManager != null)
                dataManager.addBeliefToDataManager(newActorData, unique_id);
            
            // Clear and update classifications
            foreach (SoaClassifier c in Classifiers)
            {
                c.UpdateClassifications(Detections);
            }

            // Update each weapon
            foreach (SoaWeapon w in Weapons)
            {
                w.UpdateWeapon(Detections);
            }

            // Go through each detected object's Soa Actor, get unique ID, affiliation, and pos.  Broadcast belief out to everyone
            // Send detections to Sim Controller to be logged
            foreach (GameObject gameObject in Detections)
            {
                SoaActor soaActor = gameObject.GetComponent<SoaActor>();

                // Actor's position must be converted from Unity to km when generating belief
                Belief_Actor detectedActor;
                if (classificationDictionary.ContainsKey(soaActor.unique_id) && classificationDictionary[soaActor.unique_id])
                {
                    // Me or one of my peers has classified this actor before, provide actual affiliation, isWeaponized info, and storage
                    detectedActor = new Belief_Actor(
                        soaActor.unique_id, (int)soaActor.affiliation, soaActor.type, soaActor.isAlive, 
                        soaActor.numStorageSlots, soaActor.numCasualtiesStored,
                        soaActor.numSuppliesStored, soaActor.numCiviliansStored,
                        soaActor.isWeaponized, soaActor.hasJammer, soaActor.fuelRemaining_s,
                        gameObject.transform.position.x / SimControl.KmToUnity,
                        soaActor.simAltitude_km,
                        gameObject.transform.position.z / SimControl.KmToUnity);
                }
                else
                {
                    // No one that I'm aware of has classified this actor before, hide affiliation, weapon/jamming, and storage
                    detectedActor = new Belief_Actor(
                        soaActor.unique_id, (int)Affiliation.UNCLASSIFIED, soaActor.type, soaActor.isAlive,
                        0, 0,
                        0, 0,
                        false, false, soaActor.fuelRemaining_s,
                        gameObject.transform.position.x / SimControl.KmToUnity,
                        soaActor.simAltitude_km,
                        gameObject.transform.position.z / SimControl.KmToUnity);
                }

                addMyBeliefData(detectedActor);
                simControlScript.logDetectedActor(unique_id, detectedActor);
            }
            Detections.Clear();

            //Evaluate if grid cell changed and log
            if (prevHexGridCell != null)
            {
                currentCell = hexGrid.Map[transform.position];
                if (prevHexGridCell != currentCell)
                {
                    prevHexGridCell = currentCell;
                    simControlScript.logGridCellMove(unique_id, currentCell.X, currentCell.Y);
                    //Debug.LogWarning("Actor " + unique_id + " Moved to cell " + prevHexGridCell.X + " " + prevHexGridCell.Y);
                }
            }
            else
            {
                currentCell = hexGrid.Map[transform.position];
                prevHexGridCell = currentCell;
            }
            
            List<Belief_Actor> localKillDetections = new List<Belief_Actor>();
            lock(killDetections)
            {
                localKillDetections.AddRange(killDetections);
                killDetections.Clear();
            }

            foreach (Belief_Actor belief_actor in localKillDetections)
            {
                addMyBeliefData(new Belief_Actor(
                    belief_actor.getId(), (int)belief_actor.getAffiliation(), belief_actor.getType(), false,
                    belief_actor.getNumStorageSlots(), belief_actor.getNumCasualtiesStored(),
                    belief_actor.getNumSuppliesStored(), belief_actor.getNumCiviliansStored(),
                    belief_actor.getIsWeaponized(), belief_actor.getHasJammer(), belief_actor.getFuelRemaining(),
                    belief_actor.getPos_x(),
                    belief_actor.getPos_y(),
                    belief_actor.getPos_z()));
            }

            // ???
            useGhostModel = false;           
        }
    }

    // Used for saving site observation beliefs to ownship and forwarding to neighbors
    public void RegisterSiteObservation(Belief b)
    {
        if (b is Belief_Base || b is Belief_NGOSite || b is Belief_Village)
        {
            addMyBeliefData(b);
        }
    }

    //Add data from the sensors of this actor (position updates, sensor data).  This goes to the belief dictionary and the remote beliefs to be sent.
    private void addMyBeliefData(Belief b)
    {
		if (beliefRepo.Commit(b))
		{
			dataManager.synchronizeBelief(b, unique_id);
		}
    }

    public bool checkClassified(int uniqueId)
    {
        return classificationDictionary.ContainsKey(uniqueId) && classificationDictionary[uniqueId];
    }

    public void setClassified(int uniqueId)
    {
        // Set classification dictionary value to true if not already
        classificationDictionary[uniqueId] = true;
    }

	public void addExternalBelief(Belief b)
	{
		beliefRepo.Commit(b);
	}

    public void addBeliefToBeliefDictionary(Belief b)
    {
		addMyBeliefData(b);
    }

    //Broadcast new beliefs that have been added this update cycle and then clear them from remotebeliefs after they have been sent
    public void updateRemoteAgent()
    {
		dataManager.synchronizeRepository(unique_id);
    }

    public BeliefType Find<BeliefType> (Belief.BeliefType type, int id) where BeliefType : Belief
    {
        return beliefRepo.Find<BeliefType>(type, id);
    }

    public IEnumerable<BeliefType> FindAllBeliefs<BeliefType>(Belief.BeliefType type) where BeliefType : Belief
    {
        return beliefRepo.FindAll<BeliefType>(type);
    }

    public virtual void broadcastCommsLocal()
    {
        if (communicator == null) return;

        communicator.Broadcast(beliefRepo.GetAllBeliefs());
    }

    //
    public Vector3 getPositionVector_km()
    {
        return new Vector3(simX_km, simAltitude_km, simZ_km);
    }

    // Get current time
    // Note: This is a very large number.  When comparing (subtracting) times, do all math in
    // ulong and then cast at end to float as needed.  Do NOT cast to float before subtracting.
    public ulong getCurrentTime_ms()
    {
        return (ulong)(System.DateTime.UtcNow - epoch).Ticks / 10000;
    }

    public UInt32 GetNumFreeSlots()
    {
        return numStorageSlots - numCasualtiesStored - numCiviliansStored - numSuppliesStored;
    }
}
