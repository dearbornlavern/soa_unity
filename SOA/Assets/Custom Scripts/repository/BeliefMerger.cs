using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    class BeliefMerger
    {

        public Belief merge(Belief oldBelief, Belief b)
        {
            if (b.getBeliefType() == Belief.BeliefType.ACTOR)
            {
                // Convert to belief actors
                Belief_Actor oldActorBelief = (Belief_Actor)oldBelief;
                Belief_Actor incomingActorBelief = (Belief_Actor)b;

                // To keep track of what to merge
                bool incomingBeliefMoreRecent = !incomingActorBelief.getIsAlive() && oldActorBelief.getIsAlive() ||
                    incomingActorBelief.getBeliefTime() > oldActorBelief.getBeliefTime();

                bool onlyIncomingBeliefIsClassified = oldActorBelief.getAffiliation() == (int)Affiliation.UNCLASSIFIED &&
                    incomingActorBelief.getAffiliation() != (int)Affiliation.UNCLASSIFIED;

                bool onlyOldBeliefIsClassified = oldActorBelief.getAffiliation() != (int)Affiliation.UNCLASSIFIED &&
                    incomingActorBelief.getAffiliation() == (int)Affiliation.UNCLASSIFIED;

                if (!incomingBeliefMoreRecent && !onlyIncomingBeliefIsClassified)
                {
                    return oldBelief;
                }
                else if (incomingBeliefMoreRecent && onlyOldBeliefIsClassified)
                {
                    return mergeActorBeliefs(oldActorBelief, incomingActorBelief);
                }
                else if (!incomingBeliefMoreRecent && onlyIncomingBeliefIsClassified)
                {
                    return mergeActorBeliefs(incomingActorBelief, oldActorBelief);
                }
                else
                {
                    return incomingActorBelief;
                }
            } // end actor merge policy
            else
            {
                // General merge policy (take newest belief) for every belief except actor  
                if (oldBelief.getBeliefTime() < b.getBeliefTime())
                {
                    return b;
                }
                else
                {
                    return oldBelief;
                }
            }
        }

        private Belief mergeActorBeliefs(Belief_Actor withClassification, Belief_Actor newerData)
        {
            Belief newBelief = new Belief_Actor(
                        newerData.getUnique_id(),
                        withClassification.getAffiliation(),
                        newerData.getType(),
                        newerData.getIsAlive(),
                        withClassification.getNumStorageSlots(),
                        withClassification.getNumCasualtiesStored(),
                        withClassification.getNumSuppliesStored(),
                        withClassification.getNumCiviliansStored(),
                        withClassification.getIsWeaponized(),
                        withClassification.getHasJammer(),
                        newerData.getFuelRemaining(),
                        newerData.getPos_x(),
                        newerData.getPos_y(),
                        newerData.getPos_z(),
                        newerData.getVelocity_x_valid(),
                        newerData.getVelocity_x(),
                        newerData.getVelocity_y_valid(),
                        newerData.getVelocity_y(),
                        newerData.getVelocity_z_valid(),
                        newerData.getVelocity_z());
            newBelief.setBeliefTime(newerData.getBeliefTime());

            return newBelief;
        }
    }
}
