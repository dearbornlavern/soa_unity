// Additinonal using statements are needed if we are running in Unity
#if(UNITY_STANDALONE)
using UnityEngine;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using autonomy_msg;

namespace soa
{
    class ProtobufSerializer : Serializer
    {
        // Last message type must be NUM_MESSAGETYPE
        private enum MessageType
        {
            INVALID = 0,
            ACTOR, 
            BASE,
            CASUALTY_DELIVERY,
            CASUALTY_PICKUP,
            GRIDSPEC,
            MODE_COMMAND,
            NGOSITE, 
            ROADCELL, 
            SPOI, 
            SUPPLY_DELIVERY,
            SUPPLY_PICKUP,
            TERRAIN,
            TIME,
            VILLAGE, 
            WAYPOINT, 
            WAYPOINT_OVERRIDE,
            WAYPOINT_PATH,
            CUSTOM
        };

        // Constructor
        public ProtobufSerializer()
        {
            // Do nothing
        }

        // Serialize a Belief object into a byte array using Google Protobuf + 1 header byte
        public override byte[] serializeBelief(Belief belief)
        {
            // Variables to hold serialized message
            byte header;
            byte[] body, message;

            // Initialize based on type of belief
            switch (belief.getBeliefType())
            {
                case Belief.BeliefType.ACTOR:
                    { // Actor
                        Gpb_Actor.Builder proto = Gpb_Actor.CreateBuilder();
                        Belief_Actor b = (Belief_Actor)belief;
                        proto.SetUniqueId(b.getUnique_id());
                        proto.SetAffiliation(b.getAffiliation());
                        proto.SetType(b.getType());
                        proto.SetIsAlive(b.getIsAlive());
                        proto.SetNumStorageSlots(b.getNumStorageSlots());
                        proto.SetNumCasualtiesStored(b.getNumCasualtiesStored());
                        proto.SetNumSuppliesStored(b.getNumSuppliesStored());
                        proto.SetNumCiviliansStored(b.getNumCiviliansStored());
                        proto.SetIsWeaponized(b.getIsWeaponized());
                        proto.SetHasJammer(b.getHasJammer());
                        proto.SetFuelRemaining(b.getFuelRemaining());
                        proto.SetPosX(b.getPos_x());
                        proto.SetPosY(b.getPos_y());
                        proto.SetPosZ(b.getPos_z());
                        // Optional fields
                        if (b.getVelocity_x_valid())
                        {
                            proto.SetVelocityX(b.getVelocity_x());
                        }
                        if (b.getVelocity_y_valid())
                        {
                            proto.SetVelocityY(b.getVelocity_y());
                        }
                        if (b.getVelocity_z_valid())
                        {
                            proto.SetVelocityZ(b.getVelocity_z());
                        }
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.ACTOR;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.BASE:
                    { // Base
                        Gpb_Base.Builder proto = Gpb_Base.CreateBuilder();
                        Belief_Base b = (Belief_Base)belief;
                        proto.SetId(b.getId());
                        proto.SetSupplies(b.getSupplies());
                        // Copy contents of cell list
                        List<GridCell> cells = b.getCells();
                        for (int i = 0; i < cells.Count; i++)
                        {
                            Gpb_GridCell.Builder g = Gpb_GridCell.CreateBuilder();
                            g.SetRow(cells[i].getRow());
                            g.SetCol(cells[i].getCol());
                            proto.AddCells(g.Build());
                        }
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.BASE;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.CASUALTY_DELIVERY:
                    { // Casualty Delivery
                        Gpb_CasualtyDelivery.Builder proto = Gpb_CasualtyDelivery.CreateBuilder();
                        Belief_Casualty_Delivery b = (Belief_Casualty_Delivery)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetGreedy(b.getGreedy());
                        proto.SetMultiplicity(b.getMultiplicity());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.CASUALTY_DELIVERY;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.CASUALTY_PICKUP:
                    { // Casualty Pickup
                        Gpb_CasualtyPickup.Builder proto = Gpb_CasualtyPickup.CreateBuilder();
                        Belief_Casualty_Pickup b = (Belief_Casualty_Pickup)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetGreedy(b.getGreedy());
                        // Copy contents of id list
                        int[] ids = b.getIds();
                        for (int i = 0; i < ids.Length; i++)
                        {
                            proto.AddIds(ids[i]);
                        }
                        // Copy contents of multiplicity list
                        int[] multiplicity = b.getMultiplicity();
                        for (int i = 0; i < multiplicity.Length; i++)
                        {
                            proto.AddMultiplicity(multiplicity[i]);
                        }
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.CASUALTY_PICKUP;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.GRIDSPEC:
                    { // Grid Specification
                        Gpb_GridSpec.Builder proto = Gpb_GridSpec.CreateBuilder();
                        Belief_GridSpec b = (Belief_GridSpec)belief;
                        proto.SetWidth(b.getWidth());
                        proto.SetHeight(b.getHeight());
                        proto.SetGridOriginX(b.getGridOrigin_x());
                        proto.SetGridOriginZ(b.getGridOrigin_z());
                        proto.SetGridToWorldScale(b.getGridToWorldScale());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.GRIDSPEC;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.MODE_COMMAND:
                    { // Mode Command
                        Gpb_Mode_Command.Builder proto = Gpb_Mode_Command.CreateBuilder();
                        Belief_Mode_Command b = (Belief_Mode_Command)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetModeId(b.getMode_id());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.MODE_COMMAND;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.NGOSITE:
                    { // NGO Site
                        Gpb_NGOSite.Builder proto = Gpb_NGOSite.CreateBuilder();
                        Belief_NGOSite b = (Belief_NGOSite)belief;
                        proto.SetId(b.getId());
                        proto.SetSupplies(b.getSupplies());
                        proto.SetCasualties(b.getCasualties());
                        proto.SetCivilians(b.getCivilians());
                        // Copy contents of cell list
                        List<GridCell> cells = b.getCells();
                        for (int i = 0; i < cells.Count; i++)
                        {
                            Gpb_GridCell.Builder g = Gpb_GridCell.CreateBuilder();
                            g.SetRow(cells[i].getRow());
                            g.SetCol(cells[i].getCol());
                            proto.AddCells(g.Build());
                        }
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.NGOSITE;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.ROADCELL:
                    { // Road Cell
                        Gpb_RoadCell.Builder proto = Gpb_RoadCell.CreateBuilder();
                        Belief_RoadCell b = (Belief_RoadCell)belief;
                        proto.SetIsRoadEnd(b.getIsRoadEnd());
                        // Copy contents of cell
                        GridCell cell = b.getCell();
                        Gpb_GridCell.Builder g = Gpb_GridCell.CreateBuilder();
                        g.SetRow(cell.getRow());
                        g.SetCol(cell.getCol());
                        proto.SetCell(g.Build());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.ROADCELL;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.SPOI:
                    { // SPOI
                        Gpb_SPOI.Builder proto = Gpb_SPOI.CreateBuilder();
                        Belief_SPOI b = (Belief_SPOI)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetPosX(b.getPos_x());
                        proto.SetPosY(b.getPos_y());
                        proto.SetPosZ(b.getPos_z());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.SPOI;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.SUPPLY_DELIVERY:
                    { // Supply Delivery
                        Gpb_SupplyDelivery.Builder proto = Gpb_SupplyDelivery.CreateBuilder();
                        Belief_Supply_Delivery b = (Belief_Supply_Delivery)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetGreedy(b.getGreedy());
                        // Copy contents of id list
                        int[] ids = b.getIds();
                        for (int i = 0; i < ids.Length; i++)
                        {
                            proto.AddIds(ids[i]);
                        }
                        // Copy contents of multiplicity list
                        int[] multiplicity = b.getMultiplicity();
                        for (int i = 0; i < multiplicity.Length; i++)
                        {
                            proto.AddMultiplicity(multiplicity[i]);
                        }                        
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.SUPPLY_DELIVERY;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.SUPPLY_PICKUP:
                    { // Supply Pickup
                        Gpb_SupplyPickup.Builder proto = Gpb_SupplyPickup.CreateBuilder();
                        Belief_Supply_Pickup b = (Belief_Supply_Pickup)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetGreedy(b.getGreedy());
                        proto.SetMultiplicity(b.getMultiplicity());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.SUPPLY_PICKUP;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.TERRAIN:
                    { // Terrain
                        Gpb_Terrain.Builder proto = Gpb_Terrain.CreateBuilder();
                        Belief_Terrain b = (Belief_Terrain)belief;
                        proto.SetType(b.getType());
                        // Copy contents of cell list
                        List<GridCell> cells = b.getCells();
                        for (int i = 0; i < cells.Count; i++)
                        {
                            Gpb_GridCell.Builder g = Gpb_GridCell.CreateBuilder();
                            g.SetRow(cells[i].getRow());
                            g.SetCol(cells[i].getCol());
                            proto.AddCells(g.Build());
                        }
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.TERRAIN;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.TIME:
                    { // Time
                        Gpb_Time.Builder proto = Gpb_Time.CreateBuilder();
                        Belief_Time b = (Belief_Time)belief;
                        proto.SetTime(b.getTime());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.TIME;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.VILLAGE:
                    { // Village
                        Gpb_Village.Builder proto = Gpb_Village.CreateBuilder();
                        Belief_Village b = (Belief_Village)belief;
                        proto.SetId(b.getId());
                        proto.SetSupplies(b.getSupplies());
                        proto.SetCasualties(b.getCasualties());
                        // Copy contents of cell list
                        List<GridCell> cells = b.getCells();
                        for (int i = 0; i < cells.Count; i++)
                        {
                            Gpb_GridCell.Builder g = Gpb_GridCell.CreateBuilder();
                            g.SetRow(cells[i].getRow());
                            g.SetCol(cells[i].getCol());
                            proto.AddCells(g.Build());
                        }
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.VILLAGE;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.WAYPOINT:
                    { // Waypoint
                        Gpb_Waypoint.Builder proto = Gpb_Waypoint.CreateBuilder();
                        Belief_Waypoint b = (Belief_Waypoint)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetPosX(b.getPos_x());
                        proto.SetPosY(b.getPos_y());
                        proto.SetPosZ(b.getPos_z());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.WAYPOINT;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.WAYPOINT_OVERRIDE:
                    { // Waypoint Override
                        Gpb_Waypoint_Override.Builder proto = Gpb_Waypoint_Override.CreateBuilder();
                        Belief_Waypoint_Override b = (Belief_Waypoint_Override)belief;
                        proto.SetRequestTime(b.getRequest_time());
                        proto.SetActorId(b.getActor_id());
                        proto.SetPosX(b.getPos_x());
                        proto.SetPosY(b.getPos_y());
                        proto.SetPosZ(b.getPos_z());
                        // Add on belief time
                        proto.SetBeliefTime(b.getBeliefTime());
                        // Form header + serialized message
                        header = (byte)MessageType.WAYPOINT_OVERRIDE;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                case Belief.BeliefType.WAYPOINT_PATH:
                    {
                        Gpb_WaypointPath.Builder proto = Gpb_WaypointPath.CreateBuilder();
                        Belief_WaypointPath b = (Belief_WaypointPath)belief;
                        proto.SetActorId(b.getId());
                        proto.SetBeliefTime(b.getBeliefTime());
                        proto.SetRequestTime(b.getRequestTime());

                        foreach(Waypoint point in b.getWaypoints())
                        {
                            Gpb_SingleWaypoint.Builder pointBuilder = Gpb_SingleWaypoint.CreateBuilder();
                            pointBuilder.SetX(point.x);
                            pointBuilder.SetY(point.y);
                            pointBuilder.SetZ(point.z);
                            pointBuilder.SetHeading(point.heading);
                            pointBuilder.SetVisited(point.visited);
                            proto.AddWaypoints(pointBuilder.Build());
                        }

                        header = (byte)MessageType.WAYPOINT_PATH;
                        body = proto.Build().ToByteArray();

                        break;
                    }
                case Belief.BeliefType.CUSTOM:
                    { // Custom
                        Gpb_Custom.Builder proto = Gpb_Custom.CreateBuilder();
                        Belief_Custom b = (Belief_Custom)belief;
                        proto.SetData(Google.ProtocolBuffers.ByteString.CopyFrom(b.getData()));
                        // Add on belief time
                        proto.SetCustomType(b.getCustomType());
                        proto.SetActorId(b.getId());
                        proto.SetBeliefTime(b.getBeliefTime());

                        // Form header + serialized message
                        header = (byte)MessageType.CUSTOM;
                        body = proto.Build().ToByteArray();
                        break;
                    }
                default:
                    // Unrecognized type, return empty array
                    Log.debug("ProtobufSerializer.serializeBelief(): Unrecognized Belief type");
                    return new byte[0];
            }

            // Return serialized message (header + body)
            message = new Byte[body.Length + 1];
            message[0] = header;
            System.Buffer.BlockCopy(body, 0, message, 1, body.Length);
            return message;
        }

        // Deserialize
        public override Belief generateBelief(byte[] serial)
        {
            // New belief that we will return
            Belief b;

//			Log.debug("Received a message with " + serial.Length + " bytes");

            // Break string into header and body
            MessageType headerType = (MessageType) serial[0];
            Byte[] body = new Byte[serial.Length-1];
            System.Buffer.BlockCopy(serial, 1, body, 0, serial.Length - 1);

            // Initialize based on type of belief
            switch (headerType)
            {
                case MessageType.ACTOR:
                    { // Actor
                        Gpb_Actor proto = Gpb_Actor.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Actor(
                            proto.UniqueId,
                            proto.Affiliation,
                            proto.Type,
                            proto.IsAlive,
                            proto.NumStorageSlots,
                            proto.NumCasualtiesStored,
                            proto.NumSuppliesStored,
                            proto.NumCiviliansStored,
                            proto.IsWeaponized,
                            proto.HasJammer,
                            proto.FuelRemaining,
                            proto.PosX,
                            proto.PosY,
                            proto.PosZ,
                            proto.HasVelocityX,
                            proto.VelocityX,
                            proto.HasVelocityY,
                            proto.VelocityY,
                            proto.HasVelocityZ,
                            proto.VelocityZ);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.BASE:
                    { // Base
                        Gpb_Base proto = Gpb_Base.CreateBuilder().MergeFrom(body).Build();
                        List<GridCell> cells = new List<GridCell>(proto.CellsCount);
                        for (int i = 0; i < proto.CellsCount; i++)
                        {
                            cells.Add(new GridCell(proto.CellsList[i].Row, proto.CellsList[i].Col));
                        }
                        b = new Belief_Base(
                            proto.Id,
                            cells,
                            proto.Supplies);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.CASUALTY_DELIVERY:
                    { // Casualty delivery
                        Gpb_CasualtyDelivery proto = Gpb_CasualtyDelivery.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Casualty_Delivery(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.Greedy,
                            proto.Multiplicity);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }

                case MessageType.CASUALTY_PICKUP:
                    { // Casualty pickup
                        Gpb_CasualtyPickup proto = Gpb_CasualtyPickup.CreateBuilder().MergeFrom(body).Build();
                        int[] ids = new int[proto.IdsCount];
                        for (int i = 0; i < proto.IdsCount; i++)
                        {
                            ids[i] = proto.IdsList[i];
                        }
                        int[] multiplicity = new int[proto.MultiplicityCount];
                        for (int i = 0; i < proto.MultiplicityCount; i++)
                        {
                            multiplicity[i] = proto.MultiplicityList[i];
                        }
                        b = new Belief_Casualty_Pickup(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.Greedy,
                            ids,
                            multiplicity);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.GRIDSPEC:
                    { // Grid Specification
                        Gpb_GridSpec proto = Gpb_GridSpec.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_GridSpec(
                            proto.Width,
                            proto.Height,
                            proto.GridOriginX,
                            proto.GridOriginZ,
                            proto.GridToWorldScale);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.MODE_COMMAND:
                    { // Mode Command
                        Gpb_Mode_Command proto = Gpb_Mode_Command.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Mode_Command(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.ModeId);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.NGOSITE:
                    { // NGO Site
                        Gpb_NGOSite proto = Gpb_NGOSite.CreateBuilder().MergeFrom(body).Build();
                        List<GridCell> cells = new List<GridCell>(proto.CellsCount);
                        for (int i = 0; i < proto.CellsCount; i++)
                        {
                            cells.Add(new GridCell(proto.CellsList[i].Row, proto.CellsList[i].Col));
                        }
                        b = new Belief_NGOSite(
                            proto.Id,
                            cells,
                            proto.Supplies,
                            proto.Casualties,
                            proto.Civilians);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.ROADCELL:
                    { // Road Cell
                        Gpb_RoadCell proto = Gpb_RoadCell.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_RoadCell(
                            proto.IsRoadEnd,
                            new GridCell(proto.Cell.Row, proto.Cell.Col));
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.SPOI:
                    { // SPOI
                        Gpb_SPOI proto = Gpb_SPOI.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_SPOI(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.PosX,
                            proto.PosY,
                            proto.PosZ);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.SUPPLY_DELIVERY:
                    { // Supply delivery
                        Gpb_SupplyDelivery proto = Gpb_SupplyDelivery.CreateBuilder().MergeFrom(body).Build();
                        int[] ids = new int[proto.IdsCount];
                        for (int i = 0; i < proto.IdsCount; i++)
                        {
                            ids[i] = proto.IdsList[i];
                        }
                        int[] multiplicity = new int[proto.MultiplicityCount];
                        for (int i = 0; i < proto.MultiplicityCount; i++)
                        {
                            multiplicity[i] = proto.MultiplicityList[i];
                        }
                        b = new Belief_Supply_Delivery(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.Greedy,
                            ids,
                            multiplicity);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.SUPPLY_PICKUP:
                    { // Supply pickup
                        Gpb_SupplyPickup proto = Gpb_SupplyPickup.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Supply_Pickup(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.Greedy,
                            proto.Multiplicity);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.TERRAIN:
                    { // Terrain
                        Gpb_Terrain proto = Gpb_Terrain.CreateBuilder().MergeFrom(body).Build();
                        List<GridCell> cells = new List<GridCell>(proto.CellsCount);
                        for (int i = 0; i < proto.CellsCount; i++)
                        {
                            cells.Add(new GridCell(proto.CellsList[i].Row, proto.CellsList[i].Col));
                        }
                        b = new Belief_Terrain(
                            proto.Type,
                            cells);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.TIME:
                    { // Time
                        Gpb_Time proto = Gpb_Time.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Time(
                            proto.Time);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.VILLAGE:
                    { // Village
                        Gpb_Village proto = Gpb_Village.CreateBuilder().MergeFrom(body).Build();
                        List<GridCell> cells = new List<GridCell>(proto.CellsCount);
                        for (int i = 0; i < proto.CellsCount; i++)
                        {
                            cells.Add(new GridCell(proto.CellsList[i].Row, proto.CellsList[i].Col));
                        }
                        b = new Belief_Village(
                            proto.Id,
                            cells,
                            proto.Supplies,
                            proto.Casualties);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.WAYPOINT:
                    { // Waypoint
                        Gpb_Waypoint proto = Gpb_Waypoint.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Waypoint(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.PosX,
                            proto.PosY,
                            proto.PosZ);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.WAYPOINT_OVERRIDE:
                    { // Waypoint Override
                        Gpb_Waypoint_Override proto = Gpb_Waypoint_Override.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Waypoint_Override(
                            proto.RequestTime,
                            proto.ActorId,
                            proto.PosX,
                            proto.PosY,
                            proto.PosZ);
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.WAYPOINT_PATH:
                    {
                        Gpb_WaypointPath proto = Gpb_WaypointPath.CreateBuilder().MergeFrom(body).Build();
                        List<Waypoint> waypoints = new List<Waypoint>();
                        for (int i = 0; i < proto.WaypointsCount; ++i)
                        {
                            Gpb_SingleWaypoint protoPoint = proto.WaypointsList[i];
                            Waypoint waypoint = new Waypoint();
                            waypoint.x = protoPoint.X;
                            waypoint.y = protoPoint.Y;
                            waypoint.z = protoPoint.Z;
                            waypoint.heading = protoPoint.Heading;
                            waypoint.visited = protoPoint.Visited;
                            waypoints.Add(waypoint);
                        }

                        b = new Belief_WaypointPath(proto.RequestTime, proto.ActorId, waypoints);
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                case MessageType.CUSTOM:
                    { // Time
                        Gpb_Custom proto = Gpb_Custom.CreateBuilder().MergeFrom(body).Build();
                        b = new Belief_Custom(
                            proto.CustomType,
                            proto.ActorId,
                            proto.Data.ToByteArray());
                        // Add on belief time
                        b.setBeliefTime(proto.BeliefTime);
                        break;
                    }
                default:
                    // Unrecognized type
                    Log.debug("ProtobufSerializer.generateBelief(): Unrecognized header type " + headerType);
                    // Don't create a new belief object and return to caller
                    return null;
            }

            // Return the created belief object
            return b;
        }
    }
}
