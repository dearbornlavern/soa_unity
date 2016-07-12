using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using soa;

public class BluePoliceSim : MonoBehaviour 
{
    // How often assignments are re-evaluated and waypoint recomputed
    public float evalPeriod_s;

    // Oldest allowed actor belief
    public float beliefTimeout_ms;

    public float redBaseKeepoutDist;

    // Pointer to blue base to get task assignment
    NavMeshAgent thisNavAgent;
    SimControl simControlScript;
    SoaActor thisSoaActor;

    // Local copy of belief dictionary
    IEnumerable<Belief_Actor> actorBeliefs;

    // Local copy of red units
    Dictionary<int, RedUnitInfo> redUnitDatabase;

    // Time keeping
    float timeSinceLastEval_s;

    // Variables for keeping track of task
    public enum Task
    {
        NONE,
        PATROL,
        PURSUE
    };
    public Task currTask;
    public GameObject patrolTarget;
    public int pursueActorID;
    public int numVirtualCasualties;
    float clearPatrolRange;
    float transitionProbability;

    // Saving unity game object references
    List<GameObject> redUnits;
    List<GameObject> protectedSites;
    List<GameObject> redBases;

    // To keep track of protected site info
    Dictionary<string, Vector3> protectedSitePos;
    Dictionary<string, float> protectedSiteVirtualCasualties;
    Dictionary<string, float> protectedSiteTotalCasualties;

    // Awake is called first before anything else
    void Awake()
    {
        // Initialize databases
        protectedSitePos = new Dictionary<string, Vector3>();
        protectedSiteVirtualCasualties = new Dictionary<string, float>();
        protectedSiteTotalCasualties = new Dictionary<string, float>();

        // Get and store references
        simControlScript = GameObject.FindObjectOfType<SimControl>();
        thisNavAgent = gameObject.GetComponent<NavMeshAgent>();
        thisSoaActor = gameObject.GetComponent<SoaActor>();

        // Internal parameters
        clearPatrolRange = 0.50f; // Unity units
        transitionProbability = 1.0f;

        // Initial task
        currTask = Task.NONE;
        patrolTarget = null;
        pursueActorID = 0;

        // Red unit database
        redUnitDatabase = new Dictionary<int, RedUnitInfo>();
    }

    // Use this for initialization upon activation
	void Start () 
    {
        // Unlimited fuel tank
        thisSoaActor.fuelRemaining_s = float.PositiveInfinity;
        
        // Save references to Unity game objects for quick lookup of properties
        protectedSites = new List<GameObject>();
        protectedSites.AddRange(simControlScript.NgoSites);
        protectedSites.AddRange(simControlScript.Villages);
        redBases = simControlScript.RedBases;

        // Initialize protected site weights
        InitProtectedSites();
    }

    // Come up with initial weights for the protected sites
    void InitProtectedSites()
    {
        // First save the location of each base
        foreach (GameObject p in protectedSites)
        {
            protectedSitePos.Add(p.name, p.transform.position);
        }

        // Compute maxmin dist of each protected site to red bases
        Vector3 displacement;
        Dictionary<string, float> minDistToRed = new Dictionary<string, float>();
        foreach (GameObject p in protectedSites)
        {
            // Compute min distance between p and red bases
            float minDist = float.PositiveInfinity;
            foreach (GameObject r in redBases)
            {
                displacement = p.transform.position - r.transform.position;
                if (displacement.magnitude < minDist)
                {
                    minDist = displacement.magnitude;
                }
            }

            // Save min distance from p to red bases for use later
            minDistToRed.Add(p.name, minDist);
        }

        // Compute average and min of min distances to red bases
        float avgMinDist = 0.0f;
        float minMinDist = float.PositiveInfinity;
        float count = 0;
        foreach (GameObject p in protectedSites)
        {
            // Get distance to red
            float tempDist = minDistToRed[p.name];

            // Accumulate average
            avgMinDist += tempDist;
            count++;

            // Keep track of min of min
            if (tempDist < minMinDist)
            {
                minMinDist = tempDist;
            }
        }
        avgMinDist /= count;

        // Compute weights for each base
        float weightAccum = 0.0f;
        foreach (GameObject p in protectedSites)
        {
            float weight = Mathf.Exp(-1 * (minDistToRed[p.name] - minMinDist) / (avgMinDist - minMinDist));
            protectedSiteVirtualCasualties.Add(p.name, weight);
            weightAccum += weight;
        }

        // Normalize and then scale weights so that total number of virtual casualties dispersed across
        // bases equals numVirtual Casualties
        foreach (GameObject p in protectedSites)
        {
            protectedSiteVirtualCasualties[p.name] *= (numVirtualCasualties / weightAccum);
        }
    }

	// Update is called once per frame
	void Update () 
    {
        // Increment time elapsed
        timeSinceLastEval_s += Time.deltaTime;

        // Take action if update period reached
        if (timeSinceLastEval_s >= evalPeriod_s)
        {
            // Update protected site casualty count
            updateProtectedSiteCasualtyCount();

            // Get the latest belief dictionary and red unit list from SoaActor
            actorBeliefs = gameObject.GetComponent<SoaActor>().FindAllBeliefs<Belief_Actor>(Belief.BeliefType.ACTOR);

            // Update red unit database
            updateRedUnitDatabase();

            // Make a note of those who are candidates for being pursued
            markPursueCandidates();

            // Assign a task
            assignTask();

            // Compute control law for that task
            thisNavAgent.SetDestination(computeControl());

            // Reset timer
            timeSinceLastEval_s = 0.0f;
        }
	}

    void updateProtectedSiteCasualtyCount()
    {
        // Update total casualty count for each protected site
        foreach (GameObject p in protectedSites)
        {
            switch (p.tag)
            {
                case "NGO":
                    protectedSiteTotalCasualties[p.name] =
                        protectedSiteVirtualCasualties[p.name] + p.GetComponent<NgoSim>().Casualties;
                    break;
                case "Village":
                    protectedSiteTotalCasualties[p.name] =
                        protectedSiteVirtualCasualties[p.name] + p.GetComponent<VillageSim>().Casualties;
                    break;
                default:
                    Debug.LogError("Unrecognized p.tag " + p.tag);
                    break;
            }
        }
    }

    // Store information on whether each red unit is carrying a civilian or not
    void updateRedUnitDatabase()
    {
        // Clear
        redUnitDatabase.Clear();

        // Get all red units
        redUnits = new List<GameObject>();
        redUnits.AddRange(GameObject.FindGameObjectsWithTag("RedTruck"));
        redUnits.AddRange(GameObject.FindGameObjectsWithTag("RedDismount"));

        // Loop through all actor beliefs and update the database
        foreach (Belief_Actor b in actorBeliefs)
        {
            // We care only about red units
            Belief_Actor ba = (Belief_Actor)b;

            if (ba.getAffiliation().Equals((int)Affiliation.RED))
            {
                // Save the info if the belief is new enough
                // Note: Watch out for the difference of two unsigned numbers!
                if (thisSoaActor.getCurrentTime_ms() <= ba.getBeliefTime() || 
                    (float)(thisSoaActor.getCurrentTime_ms() - ba.getBeliefTime()) <= beliefTimeout_ms)
                {
                    RedUnitInfo redUnitInfo = new RedUnitInfo(ba, redUnits, redBases, protectedSites);
                    if (redUnitInfo.distToClosestRedBase > redBaseKeepoutDist * SimControl.KmToUnity)
                    {
                        redUnitDatabase.Add(ba.getId(), new RedUnitInfo(ba, redUnits, redBases, protectedSites));
                    }
                }
            }
        }
    }

    // Keeps track of a pursue list (really map) with hysterisis distances to closest base as condition
    // for being added/removed from map.  Each key is an actor ID for a red unit and the value is the
    // updated distance to closest base
    void markPursueCandidates()
    {
        // Look at each red unit in the database and determine if it should be a candidate to be pursued
        foreach (int id in redUnitDatabase.Keys)
        {
            // Get the record
            RedUnitInfo redUnitInfo = redUnitDatabase[id];

            // Estimate how long it takes for me to get there
            float myTravelTime = GetRangeTo(redUnitInfo.closestRedBasePos) / thisNavAgent.speed;

            // Estimate red unit round trip time
            float redRoundTripTime;
            if (redUnitInfo.hasCivilian)
            {
                // Guess that it is going straight to closest base
                redRoundTripTime = redUnitInfo.distToClosestRedBase / redUnitInfo.maxSpeed;

                // Determine if I can get it before it returns to base (include some inefficiencies)
                if (myTravelTime < redRoundTripTime)
                {
                    // Mark as someone who can be caught in time
                    redUnitInfo.isCatchable = true;
                }
            }
            else
            {
                if (redUnitInfo.distToClosestRedBase > redBaseKeepoutDist*SimControl.KmToUnity)
                {
                    // Guess that it will first go to closest protected site and then to closest base
                    redRoundTripTime = (redUnitInfo.distToClosestProtectedSite +
                        Vector3.Distance(redUnitInfo.closestProtectedSitePos, redUnitInfo.closestRedBasePos))
                        / redUnitInfo.maxSpeed;

                    // Determine if I can get it before it returns to base (include some inefficiencies)
                    if (myTravelTime < redRoundTripTime)
                    {
                        // Mark as someone who can be caught in time
                        redUnitInfo.isCatchable = true;
                    }
                }
            }
        }
    }

    // Assigns a task to current police unit based on the pursueList
    public void assignTask()
    {
        if (redUnitDatabase.Keys.Count == 0)
        {
            // Transition from pursue to patrol
            if (currTask != Task.PATROL)
            {
                // Pick a new patrol target
                currTask = Task.PATROL;
                patrolTarget = assignNewProtectedSite(patrolTarget);

            }
            else if (Vector3.Distance(transform.position, patrolTarget.transform.position) <= clearPatrolRange * SimControl.KmToUnity)
            {
                // We are close enough to a target to clear it, choose to stay or leave with some probability
                if (Random.value <= transitionProbability)
                {
                    patrolTarget = assignNewProtectedSite(patrolTarget);
                }
                else
                {
                    // We choose to stay here for another update, keep the same task
                }
            }
            else
            {
                // We are patrolling but have not gotten to target yet, keep the same task
            }
        }
        else
        {
            // Set intent to pursue
            currTask = Task.PURSUE;

            // Create 4 lists for determining who to chase after
            List<int> isCatchableHasCivilian = new List<int>();
            List<int> isCatchableNoCivilian = new List<int>();
            List<int> notCatchableHasCivilian = new List<int>();
            List<int> notCatchableNoCivilian = new List<int>();

            // Pick a pursuit candidate with civilian to pursue
            float minCost = float.PositiveInfinity;
                
            // Categorize the candidates
            foreach (int id in redUnitDatabase.Keys)
            {
                // Get the record
                RedUnitInfo redUnitInfo = redUnitDatabase[id];

                if (redUnitInfo.isCatchable && redUnitInfo.hasCivilian)
                {
                    isCatchableHasCivilian.Add(id);
                }
                else if (redUnitInfo.isCatchable && !redUnitInfo.hasCivilian)
                {
                    isCatchableNoCivilian.Add(id);
                }
                else if (!redUnitInfo.isCatchable && redUnitInfo.hasCivilian)
                {
                    notCatchableHasCivilian.Add(id);
                }
                else
                {
                    notCatchableNoCivilian.Add(id);
                }
            }

            // Find the pursuit target based on priority and min cost
            if(isCatchableHasCivilian.Count != 0)
            {
                // First priority: is catchable and has civilian
                foreach (int id in isCatchableHasCivilian)
                {
                    float actorCost = GetRangeTo(redUnitDatabase[id].pos);
                    if(actorCost < minCost)
                    {
                        minCost = actorCost;
                        pursueActorID = id;
                    }
                }
            }
            if(float.IsInfinity(minCost) && isCatchableNoCivilian.Count != 0)
            {
                // Second priority: is catchable but no civilian
                foreach (int id in isCatchableNoCivilian)
                {
                    float actorCost = GetRangeTo(redUnitDatabase[id].pos);
                    if (actorCost < minCost)
                    {
                        minCost = actorCost;
                        pursueActorID = id;
                    }
                }
            }
            if (float.IsInfinity(minCost) && notCatchableHasCivilian.Count != 0)
            {
                // Third priority: not catchable but has civilian
                foreach (int id in notCatchableHasCivilian)
                {
                    float actorCost = GetRangeTo(redUnitDatabase[id].pos);
                    if (actorCost < minCost)
                    {
                        minCost = actorCost;
                        pursueActorID = id;
                    }
                }
            }
            if (float.IsInfinity(minCost) && notCatchableNoCivilian.Count != 0)
            {
                // Last priority: not catchable and no civilian
                foreach (int id in notCatchableNoCivilian)
                {
                    float actorCost = GetRangeTo(redUnitDatabase[id].pos);
                    if (actorCost < minCost)
                    {
                        minCost = actorCost;
                        pursueActorID = id;
                    }
                }
            }         
        }
    }

    public GameObject assignNewProtectedSite(GameObject currPatrolTarget){
        // Cost vector
        float[] weight = new float[protectedSites.Count];

        // Assign costs to travel to each
        float weightAccum = 0.0f;
        for(int i=0; i<protectedSites.Count; i++)
        {
            GameObject p = protectedSites[i];
            if (currPatrolTarget != null && currPatrolTarget == p)
            {
                // Do not go back to myself
                weight[i] = 0.0f;
            }
            else
            {
                // Weight
                weight[i] = protectedSiteVirtualCasualties[p.name] * protectedSiteVirtualCasualties[p.name] / GetRangeTo(p);
            }
            weightAccum += weight[i];
        }

        // Pick one site at random with weights as the probabilty distribution
        float randEval = Random.RandomRange(0, weightAccum);

        // Find which one was chosen
        float accum = 0.0f;
        GameObject chosenSite = protectedSites[protectedSites.Count-1];
        for(int i=0; i<protectedSites.Count; i++)
        {
            accum += weight[i];
            if (randEval <= accum)
            {
                chosenSite = protectedSites[i];
                break;
            }
        }

        // Return the chosen site
        return chosenSite;
    }

    public Vector3 computeControl()
    {
        switch (currTask)
        {
            case Task.PATROL:
                // Just naively goto patrol target for now
                return patrolTarget.transform.position;

            case Task.PURSUE:
                // TODO: Implement motion camouflage or intercept
                // Just do classical pursuit for now
                return redUnitDatabase[pursueActorID].pos;
            
            default:
                // Unrecognized task
                Debug.LogError("computeControl(): Unrecognized currTask " + currTask.ToString());
                return new Vector3(float.NaN, float.NaN, float.NaN);
        }
    }

    public float GetRangeTo(GameObject destination)
    {
        return GetRangeTo(destination.transform.position);
    }

    public float GetRangeTo(Vector3 position)
    {
        NavMeshPath thisPath = new NavMeshPath();
        if (thisNavAgent.CalculatePath(position, thisPath))
        {
            return PathLength(thisPath);
        }
        else
        {
            return -1f;
        }
    }

    float PathLength(NavMeshPath path)
    {
        if (path.corners.Length < 2)
            return 0f;

        Vector3 previousCorner = path.corners[0];
        float lengthSoFar = 0.0F;
        int i = 1;
        while (i < path.corners.Length)
        {
            Vector3 currentCorner = path.corners[i];
            lengthSoFar += Vector3.Distance(previousCorner, currentCorner);
            previousCorner = currentCorner;
            i++;
        }
        return lengthSoFar;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RedTruck"))
        {
            RedTruckSim r = other.gameObject.GetComponent<RedTruckSim>();
            if (r != null)
            {
                Debug.Log(transform.name + " intercepts " + other.name);
            }
        }
        if (other.CompareTag("RedDismount"))
        {
            RedDismountSim r = other.gameObject.GetComponent<RedDismountSim>();
            if (r != null)
            {
                Debug.Log(transform.name + " intercepts " + other.name);
            }
        }
        if (other.CompareTag("BlueBase"))
        {
            // Get an observation about the blue base
            BlueBaseSim b = other.gameObject.GetComponent<BlueBaseSim>();
            if (b != null)
            {
                thisSoaActor.RegisterSiteObservation(new soa.Belief_Base(
                    b.destination_id, b.gridCells, b.Supply));
            }
        }
        if (other.CompareTag("NGO"))
        {
            // Get an observation about the NGO
            NgoSim n = other.gameObject.GetComponent<NgoSim>();
            if (n != null)
            {
                thisSoaActor.RegisterSiteObservation(new soa.Belief_NGOSite(
                    n.destination_id, n.gridCells, n.Supply, n.Casualties, n.Civilians));
            }
        }
        if (other.CompareTag("Village"))
        {
            // Get an observation about the village
            VillageSim v = other.gameObject.GetComponent<VillageSim>();
            if (v != null)
            {
                thisSoaActor.RegisterSiteObservation(new soa.Belief_Village(
                    v.destination_id, v.gridCells, v.Supply, v.Casualties));
            }
        }
    }
}
