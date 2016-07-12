using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using soa;

public class HeavyUAVSim : MonoBehaviour 
{
    SimControl simControlScript;
    SoaActor thisSoaActor;
    public float fuelTankSize_s;
    public GameObject SupplyIcon;
    public GameObject CasualtyIcon;

    // Awake is called first before anything else
    void Awake()
    {
        thisSoaActor = gameObject.GetComponent<SoaActor>();
        simControlScript = GameObject.FindObjectOfType<SimControl>();
    }

	// Use this for initialization upon activation
	void Start () 
    {
        // Start on a full tank
        thisSoaActor.fuelRemaining_s = fuelTankSize_s;
    }
	
	// Update is called once per frame
	void Update () 
    {
        // Set icons
        SupplyIcon.SetActive(thisSoaActor.numSuppliesStored > 0);
        CasualtyIcon.SetActive(thisSoaActor.numCasualtiesStored > 0);
	}

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BlueBase"))
        {
            // Instant refuel
            thisSoaActor.fuelRemaining_s = fuelTankSize_s;

            // Process casualties/supplies
            BlueBaseSim b = other.gameObject.GetComponent<BlueBaseSim>();
            if (b != null)
            {
                // Deliver specified # of casualties first
                int numCasualtiesToDeliver = RequestCasualtyDelivery((int)thisSoaActor.numCasualtiesStored);
                for (int i = 0; i < numCasualtiesToDeliver; i++)
                {
                    simControlScript.soaEventLogger.LogCasualtyDelivery(gameObject.name, other.name);
                }
                b.Casualties += numCasualtiesToDeliver;
                thisSoaActor.numCasualtiesStored = (uint) (thisSoaActor.numCasualtiesStored - numCasualtiesToDeliver);                       
                
                // Pick up specified # of supplies next
                int numSuppliesToPickup = RequestSupplyPickup((int)thisSoaActor.GetNumFreeSlots(), b.Supply);
                b.Supply -= numSuppliesToPickup;
                thisSoaActor.numSuppliesStored = (uint) (thisSoaActor.numSuppliesStored + numSuppliesToPickup);

                // Make a new observation about the site
                thisSoaActor.RegisterSiteObservation(new soa.Belief_Base(
                    b.destination_id, b.gridCells, b.Supply));
            }
        }

        if (other.CompareTag("NGO"))
        {
            NgoSim n = other.gameObject.GetComponent<NgoSim>();
            if (n != null)
            {
                // Deliver specified # of supplies first
                int numSuppliesToDeliver = RequestSupplyDelivery((int)thisSoaActor.numSuppliesStored, n.destination_id);
                for (int i = 0; i < numSuppliesToDeliver; i++)
                {
                    simControlScript.soaEventLogger.LogSupplyDelivered(gameObject.name, other.name);
                }
                n.Supply += numSuppliesToDeliver;
                thisSoaActor.numSuppliesStored = (uint) (thisSoaActor.numSuppliesStored - numSuppliesToDeliver);

                // Pickup specified # of casualties next
                int numCasualtiesToPickup = RequestCasualtyPickup((int)thisSoaActor.GetNumFreeSlots(), n.Casualties, n.destination_id);
                n.Casualties -= numCasualtiesToPickup;
                thisSoaActor.numCasualtiesStored = (uint)(thisSoaActor.numCasualtiesStored + numCasualtiesToPickup);

                // Make a new observation about the site
                thisSoaActor.RegisterSiteObservation(new soa.Belief_NGOSite(
                    n.destination_id, n.gridCells, n.Supply, n.Casualties, n.Civilians));
            }
        }

        if (other.CompareTag("Village"))
        {
            VillageSim v = other.gameObject.GetComponent<VillageSim>();
            if (v != null)
            {
                // Deliver specified # of supplies first
                int numSuppliesToDeliver = RequestSupplyDelivery((int)thisSoaActor.numSuppliesStored, v.destination_id);
                for (int i = 0; i < numSuppliesToDeliver; i++)
                {
                    simControlScript.soaEventLogger.LogSupplyDelivered(gameObject.name, other.name);
                }
                v.Supply += numSuppliesToDeliver;
                thisSoaActor.numSuppliesStored = (uint)(thisSoaActor.numSuppliesStored - numSuppliesToDeliver);

                // Pickup specified # of casualties next twupy1
                int numCasualtiesToPickup = RequestCasualtyPickup((int)thisSoaActor.GetNumFreeSlots(), v.Casualties, v.destination_id);
                v.Casualties -= numCasualtiesToPickup;
                thisSoaActor.numCasualtiesStored = (uint)(thisSoaActor.numCasualtiesStored + numCasualtiesToPickup);

                // Make a new observation about the site
                thisSoaActor.RegisterSiteObservation(new soa.Belief_Village(
                    v.destination_id, v.gridCells, v.Supply, v.Casualties));
            }
        }
    }

    #region Casualty/Supply Pickup/Delivery Helper Functions
    // Checks current inventory and belief to determine the number of casualties to deliver
    private int RequestCasualtyDelivery(int currentInventory)
    {
        Belief_Casualty_Delivery b = thisSoaActor.Find<Belief_Casualty_Delivery>(soa.Belief.BeliefType.CASUALTY_DELIVERY, thisSoaActor.unique_id);
        if (b != null)
        {
            if (b.getGreedy() || b.getMultiplicity() < 0)
            {
                // Greedy behavior, deliver all of current inventory.  No information to update
                // Negative multiplicity is also greedy
                return currentInventory;
            }
            else
            {
                // Only deliver min of currentInventory and multiplicity
                int quantityToDeliver = (currentInventory < b.getMultiplicity()) ? currentInventory : b.getMultiplicity();

                // Update the belief if we are delivering anything
                if (quantityToDeliver > 0)
                {
                    Belief_Casualty_Delivery newBelief = new Belief_Casualty_Delivery(
                        b.getRequest_time(), b.getActor_id(),
                        b.getGreedy(), b.getMultiplicity() - quantityToDeliver);
                    thisSoaActor.addBeliefToBeliefDictionary(newBelief);
                }

                // Return the quantity to deliver
                return quantityToDeliver;
            }
        }
        else
        {
            // No entry exists, default to greedy behavior.
            return currentInventory;
        }
    }

    // Checks current available slots and belief to determine the number of supplies to pickup
    private int RequestSupplyPickup(int currentAvailableSlots, float availableSupplyCount)
    {
        // Floor the supply count
        int flooredSupplyCount = (int)Mathf.Floor(availableSupplyCount);

        Belief_Supply_Pickup b = thisSoaActor.Find<Belief_Supply_Pickup>(soa.Belief.BeliefType.SUPPLY_PICKUP, thisSoaActor.unique_id);
        if (b != null)
        {
            if (b.getGreedy() || b.getMultiplicity() < 0)
            {
                // Greedy behavior, pickup as much as you can.  No information to update
                // Negative multiplicity is also greedy
                // Amount to pick up is the min of currentAvailableSlots and flooredSupplyCount
                return (currentAvailableSlots < flooredSupplyCount) ? currentAvailableSlots : flooredSupplyCount;
            }
            else
            {
                // Only pickup min of currentAvailableSlots, flooredSupplyCount, and multiplicity
                int quantityToPickup = (currentAvailableSlots < flooredSupplyCount) ? currentAvailableSlots : flooredSupplyCount;
                quantityToPickup = (quantityToPickup < b.getMultiplicity()) ? quantityToPickup : b.getMultiplicity();

                // Update the belief if we are picking up anything
                if (quantityToPickup > 0)
                {
                    Belief_Supply_Pickup newBelief = new Belief_Supply_Pickup(
                        b.getRequest_time(), b.getActor_id(),
                        b.getGreedy(), b.getMultiplicity() - quantityToPickup);
                    thisSoaActor.addBeliefToBeliefDictionary(newBelief);
                }

                // Return the quantity to pickup
                return quantityToPickup;
            }
        }
        else
        {
            // No entry exists, default to greedy behavior.
            return (currentAvailableSlots < flooredSupplyCount) ? currentAvailableSlots : flooredSupplyCount;
        }
    }

    // Request supply delivery
    private int RequestSupplyDelivery(int currentInventory, int destinationId)
    {
        // Get the belief dictionary and belief
        Belief_Supply_Delivery b = thisSoaActor.Find<Belief_Supply_Delivery>(soa.Belief.BeliefType.SUPPLY_DELIVERY, thisSoaActor.unique_id);
        if (b != null)
        {
            // Lookup destination id's multiplicity, no entry defaults to 0
            int[] ids = b.getIds();
            int[] multiplicity = b.getMultiplicity();
            int destinationMultiplicity = 0;
            int numEntries = (ids.Length < multiplicity.Length) ? ids.Length : multiplicity.Length;
            int foundIdx = -1;
            for (int i = 0; i < numEntries; i++)
            {
                if(ids[i] == destinationId)
                {
                    foundIdx = i;
                    destinationMultiplicity = multiplicity[i];
                    break;
                }
            }

            if (b.getGreedy() || destinationMultiplicity < 0)
            {
                // Greedy behavior, deliver all of current inventory.  No information to update
                // Negative multiplicity is also greedy
                return currentInventory;
            }
            else
            {
                // Only deliver min of currentInventory and multiplicity
                int quantityToDeliver = (currentInventory < destinationMultiplicity) ? currentInventory : destinationMultiplicity;

                // Update the belief if we are delivering anything
                if (quantityToDeliver > 0)
                {
                    multiplicity[foundIdx] -= quantityToDeliver;
                    Belief_Supply_Delivery newBelief = new Belief_Supply_Delivery(
                        b.getRequest_time(), b.getActor_id(),
                        b.getGreedy(), ids, multiplicity);
                    thisSoaActor.addBeliefToBeliefDictionary(newBelief);
                }

                // Return the quantity to deliver
                return quantityToDeliver;
            }
        }
        else
        {
            // No entry exists, default to gredy behavior.
            return currentInventory;
        }
    }

    // Request casualty pickup
    private int RequestCasualtyPickup(int currentAvailableSlots, float availableSupplyCount, int destinationId)
    {
        // Floor the supply count
        int flooredSupplyCount = (int)Mathf.Floor(availableSupplyCount);

        // Get the belief dictionary and belief
        Belief_Casualty_Pickup b = thisSoaActor.Find<Belief_Casualty_Pickup>(soa.Belief.BeliefType.CASUALTY_PICKUP, thisSoaActor.unique_id);
        if (b != null)
        {
            // Lookup destination id's multiplicity, no entry defaults to 0
            int[] ids = b.getIds();
            int[] multiplicity = b.getMultiplicity();
            int destinationMultiplicity = 0;
            int numEntries = (ids.Length < multiplicity.Length) ? ids.Length : multiplicity.Length;
            int foundIdx = -1;
            for (int i = 0; i < numEntries; i++)
            {
                if (ids[i] == destinationId)
                {
                    foundIdx = i;
                    destinationMultiplicity = multiplicity[i];
                    break;
                }
            }

            if (b.getGreedy() || destinationMultiplicity < 0)
            {
                // Greedy behavior, pickup as much as you can.  No information to update
                // Negative multiplicity is also greedy
                // Amount to pick up is the min of currentAvailableSlots and flooredSupplyCount
                return (currentAvailableSlots < flooredSupplyCount) ? currentAvailableSlots : flooredSupplyCount;
            }
            else
            {
                // Only pickup min of currentAvailableSlots, flooredSupplyCount, and multiplicity
                int quantityToPickup = (currentAvailableSlots < flooredSupplyCount) ? currentAvailableSlots : flooredSupplyCount;
                quantityToPickup = (quantityToPickup < destinationMultiplicity) ? quantityToPickup : destinationMultiplicity;

                // Update the belief if we are picking up anything
                if (quantityToPickup > 0)
                {
                    multiplicity[foundIdx] -= quantityToPickup;
                    Belief_Casualty_Pickup newBelief = new Belief_Casualty_Pickup(
                        b.getRequest_time(), b.getActor_id(),
                        b.getGreedy(), ids, multiplicity);
                    thisSoaActor.addBeliefToBeliefDictionary(newBelief);
                }

                // Return the quantity to pickup
                return quantityToPickup;
            }
        }
        else
        {
            // No entry exists, default to greedy behavior.  No information to update.
            return (currentAvailableSlots < flooredSupplyCount) ? currentAvailableSlots : flooredSupplyCount;
        }
    }
   
    #endregion
}
