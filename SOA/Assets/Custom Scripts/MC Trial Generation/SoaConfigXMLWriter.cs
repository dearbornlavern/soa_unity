#if(UNITY_STANDALONE)
using UnityEngine;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace soa
{
    class SoaConfigXMLWriter
    {
        public static void Write(SoaConfig soaConfig, string xmlFilename)
        {
            // Create new XML document
            XmlDocument xmlDoc = new XmlDocument();

            // Create "SoaConfig" node
            XmlNode configNode = xmlDoc.CreateElement("SoaConfig");
            xmlDoc.AppendChild(configNode);

            // Populate "Network" node
            PopulateNetwork(xmlDoc, configNode, soaConfig);

            // Populate "Logger" node
            PopulateLogger(xmlDoc, configNode, soaConfig);

            // Populate "Simulation" node
            PopulateSimulation(xmlDoc, configNode, soaConfig);

            // Populate "FuelDefaults" node
            PopulateFuelDefaults(xmlDoc, configNode, soaConfig);

            // Populate "StorageDefaults" node
            PopulateStorageDefaults(xmlDoc, configNode, soaConfig);

            // Populate "SensorDefaults" node
            PopulateSensorDefaults(xmlDoc, configNode, soaConfig);

            // Populate "ClassifierDefaults" node
            PopulateClassifierDefaults(xmlDoc, configNode, soaConfig);

            // Populate "CommsDefaults" node
            PopulateCommsDefaults(xmlDoc, configNode, soaConfig);

            // Populate "JammerDefaults" node
            PopulateJammerDefaults(xmlDoc, configNode, soaConfig); 

            // Populate "Sites" node
            PopulateSites(xmlDoc, configNode, soaConfig);

            // Populate "Local" node
            PopulateLocal(xmlDoc, configNode, soaConfig);

            // Populate "Remote" node
            PopulateRemote(xmlDoc, configNode, soaConfig);

            // Write to file
            xmlDoc.Save(xmlFilename);
        }

        private static void AddAttribute(XmlDocument xmlDoc, XmlNode node, string attribute, string value)
        {
            if (attribute != null && value != null)
            {
                XmlAttribute newAttribute = xmlDoc.CreateAttribute(attribute);
                newAttribute.Value = value;
                node.Attributes.Append(newAttribute);
            }
        }

        private static void AddOptionalAttribute(XmlDocument xmlDoc, XmlNode node, string attribute, Optional<bool> value)
        {
            if (attribute != null && value != null && value.GetIsSet())
            {
                XmlAttribute newAttribute = xmlDoc.CreateAttribute(attribute);
                newAttribute.Value = value.ToString();
                node.Attributes.Append(newAttribute);
            }
        }

        private static void AddOptionalAttribute(XmlDocument xmlDoc, XmlNode node, string attribute, Optional<int> value)
        {
            if (attribute != null && value != null && value.GetIsSet())
            {
                XmlAttribute newAttribute = xmlDoc.CreateAttribute(attribute);
                newAttribute.Value = value.ToString();
                node.Attributes.Append(newAttribute);
            }
        }

        private static void AddOptionalAttribute(XmlDocument xmlDoc, XmlNode node, string attribute, Optional<float> value)
        {
            if (attribute != null && value != null && value.GetIsSet())
            {
                XmlAttribute newAttribute = xmlDoc.CreateAttribute(attribute);
                newAttribute.Value = value.ToString();
                node.Attributes.Append(newAttribute);
            }
        }

        private static void AddOptionalAttribute(XmlDocument xmlDoc, XmlNode node, string attribute, Optional<string> value)
        {
            if (attribute != null && value != null && value.GetIsSet())
            {
                XmlAttribute newAttribute = xmlDoc.CreateAttribute(attribute);
                newAttribute.Value = value.ToString();
                node.Attributes.Append(newAttribute);
            }
        }

        private static void PopulateNetwork(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "Network" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("Network");
            configNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "redRoom",  soaConfig.networkRedRoom);
            AddAttribute(xmlDoc, node, "blueRoom", soaConfig.networkBlueRoom);
        }

        private static void PopulateLogger(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "Logger" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("Logger");
            configNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "outputFile",              soaConfig.loggerOutputFile);
            AddAttribute(xmlDoc, node, "enableLogToFile",         soaConfig.enableLogToFile.ToString());
            AddAttribute(xmlDoc, node, "enableLogEventsToFile",   soaConfig.enableLogEventsToFile.ToString());
            AddAttribute(xmlDoc, node, "enableLogToUnityConsole", soaConfig.enableLogToUnityConsole.ToString());
        }

        private static void PopulateSimulation(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "Simulation" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("Simulation");
            configNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "simulationRandomSeed", soaConfig.simulationRandomSeed.ToString());
            AddAttribute(xmlDoc, node, "gameDurationHr", soaConfig.gameDurationHr.ToString());
            AddAttribute(xmlDoc, node, "probRedDismountHasWeapon", soaConfig.probRedDismountHasWeapon.ToString());
            AddAttribute(xmlDoc, node, "probRedTruckHasWeapon",    soaConfig.probRedTruckHasWeapon.ToString());
            AddAttribute(xmlDoc, node, "probRedTruckHasJammer",    soaConfig.probRedTruckHasJammer.ToString());
        }

        private static void PopulateFuelDefaults(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "FuelDefaults" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("FuelDefaults");
            configNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "heavyUAVFuelTankSize_s", soaConfig.defaultHeavyUAVFuelTankSize_s.ToString());
            AddAttribute(xmlDoc, node, "smallUAVFuelTankSize_s", soaConfig.defaultSmallUAVFuelTankSize_s.ToString());
        }

        private static void PopulateStorageDefaults(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "StorageDefaults" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("StorageDefaults");
            configNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "heavyUAVNumStorageSlots", soaConfig.defaultHeavyUAVNumStorageSlots.ToString());
            AddAttribute(xmlDoc, node, "redDismountNumStorageSlots", soaConfig.defaultRedDismountNumStorageSlots.ToString());
            AddAttribute(xmlDoc, node, "redTruckNumStorageSlots", soaConfig.defaultRedTruckNumStorageSlots.ToString());
        }

        private static void PopulateSensorDefaults(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create SensorDefaults node
            XmlNode sensorDefaultsNode = xmlDoc.CreateElement("SensorDefaults");
            configNode.AppendChild(sensorDefaultsNode);

            // Make an entry for each key
            XmlNode platformNode;
            foreach (string platformName in soaConfig.defaultSensorModalities.Keys)
            {
                // Create node and add to perception group
                platformNode = xmlDoc.CreateElement(platformName);
                sensorDefaultsNode.AppendChild(platformNode);

                // Add beamwidth_deg attribute only for UAVs and Balloons and for sensor only
                switch (platformName)
                {
                    case "HeavyUAV":
                    case "SmallUAV":
                    case "BlueBalloon":
                        if (soaConfig.defaultSensorBeamwidths.ContainsKey(platformName))
                        {
                            AddAttribute(xmlDoc, platformNode, "beamwidth_deg", soaConfig.defaultSensorBeamwidths[platformName].ToString());
                        }
                        break;
                }

                // Add modes
                PopulatePerceptionModes(xmlDoc, platformNode, soaConfig.defaultSensorModalities[platformName]);
            }
        }

        private static void PopulateClassifierDefaults(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create ClassifierDefaults node
            XmlNode classifierDefaultsNode = xmlDoc.CreateElement("ClassifierDefaults");
            configNode.AppendChild(classifierDefaultsNode);

            // Make an entry for each key
            XmlNode platformNode;
            foreach (string platformName in soaConfig.defaultClassifierModalities.Keys)
            {
                // Create node and add to perception group
                platformNode = xmlDoc.CreateElement(platformName);
                classifierDefaultsNode.AppendChild(platformNode);

                // Add modes
                PopulatePerceptionModes(xmlDoc, platformNode, soaConfig.defaultClassifierModalities[platformName]);
            }
        }

        private static void PopulatePerceptionModes(XmlDocument xmlDoc, XmlNode parentNode,
            List<PerceptionModality> modes)
        {
            XmlNode modalityNode;
            foreach (PerceptionModality mode in modes)
            {
                // Create and attach mode node
                modalityNode = xmlDoc.CreateElement("Mode");
                parentNode.AppendChild(modalityNode);

                // Add attributes
                AddAttribute(xmlDoc, modalityNode, "tag", mode.tagString);
                AddAttribute(xmlDoc, modalityNode, "RangeP1_km", mode.RangeP1.ToString());
                AddAttribute(xmlDoc, modalityNode, "RangeMax_km", mode.RangeMax.ToString());
            }
        }

        private static void PopulateCommsDefaults(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create CommsDefaults node
            XmlNode commsDefaultsNode = xmlDoc.CreateElement("CommsDefaults");
            configNode.AppendChild(commsDefaultsNode);

            // Make an entry for each key
            XmlNode platformNode;
            foreach (string platformName in soaConfig.defaultCommsRanges.Keys)
            {
                // Create and attach node
                platformNode = xmlDoc.CreateElement("Node");
                commsDefaultsNode.AppendChild(platformNode);

                // Add attributes
                AddAttribute(xmlDoc, platformNode, "tag", platformName);
                AddAttribute(xmlDoc, platformNode, "commsRange_km", soaConfig.defaultCommsRanges[platformName].ToString());
            }
        }

        private static void PopulateJammerDefaults(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create JammerDefaults node
            XmlNode jammerDefaultsNode = xmlDoc.CreateElement("JammerDefaults");
            configNode.AppendChild(jammerDefaultsNode);

            // Make an entry for each key
            XmlNode platformNode;
            foreach (string platformName in soaConfig.defaultJammerRanges.Keys)
            {
                // Create and attach node
                platformNode = xmlDoc.CreateElement("Node");
                jammerDefaultsNode.AppendChild(platformNode);

                // Add attributes
                AddAttribute(xmlDoc, platformNode, "tag", platformName);
                AddAttribute(xmlDoc, platformNode, "jammerRange_km", soaConfig.defaultJammerRanges[platformName].ToString());
            }
        }

        private static void PopulateSites(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "Sites" node and append to configNode
            XmlNode sitesNode = xmlDoc.CreateElement("Sites");
            configNode.AppendChild(sitesNode);

            // Make a node for each site
            XmlNode node;
            foreach(SiteConfig siteConfig in soaConfig.sites)
            {
                switch (siteConfig.GetConfigType())
                {
                    case SiteConfig.ConfigType.BLUE_BASE:
                        node = xmlDoc.CreateElement("BlueBase");
                        sitesNode.AppendChild(node);
                        AddAttribute(xmlDoc, node, "name", siteConfig.name);
                        AddAttribute(xmlDoc, node, "x_km", siteConfig.x_km.ToString());
                        AddAttribute(xmlDoc, node, "z_km", siteConfig.z_km.ToString());
                        AddOptionalAttribute(xmlDoc, node, "commsRange_km", ((BlueBaseConfig)siteConfig).commsRange_km);
                        break;
                    case SiteConfig.ConfigType.RED_BASE:
                        node = xmlDoc.CreateElement("RedBase");
                        sitesNode.AppendChild(node);
                        AddAttribute(xmlDoc, node, "name", siteConfig.name);
                        AddAttribute(xmlDoc, node, "x_km", siteConfig.x_km.ToString());
                        AddAttribute(xmlDoc, node, "z_km", siteConfig.z_km.ToString());
                        break;
                    case SiteConfig.ConfigType.NGO_SITE:
                        node = xmlDoc.CreateElement("NGOSite");
                        sitesNode.AppendChild(node);
                        AddAttribute(xmlDoc, node, "name", siteConfig.name);
                        AddAttribute(xmlDoc, node, "x_km", siteConfig.x_km.ToString());
                        AddAttribute(xmlDoc, node, "z_km", siteConfig.z_km.ToString());
                        break;
                    case SiteConfig.ConfigType.VILLAGE:
                        node = xmlDoc.CreateElement("Village");
                        sitesNode.AppendChild(node);
                        AddAttribute(xmlDoc, node, "name", siteConfig.name);
                        AddAttribute(xmlDoc, node, "x_km", siteConfig.x_km.ToString());
                        AddAttribute(xmlDoc, node, "z_km", siteConfig.z_km.ToString());
                        break;
                    default:
                        Log.error("SoaConfigXMLWriter::PopulateSites(): Unrecognized config type " + siteConfig.GetConfigType() + " in soaConfig.sites");
                        break;
                }
            }
        }

        private static void PopulateLocal(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "Local" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("Local");
            configNode.AppendChild(node);

            // Go through each local platform and add children to "Local" node
            foreach (PlatformConfig p in soaConfig.localPlatforms)
            {
                switch(p.GetConfigType())
                {
                    case PlatformConfig.ConfigType.RED_DISMOUNT:
                        PopulateRedDismount(xmlDoc, node, (RedDismountConfig) p);
                        break;
                    case PlatformConfig.ConfigType.RED_TRUCK:
                        PopulateRedTruck(xmlDoc, node, (RedTruckConfig)p);
                        break;
                    case PlatformConfig.ConfigType.NEUTRAL_DISMOUNT:
                        PopulateNeutralDismount(xmlDoc, node, (NeutralDismountConfig)p);
                        break;
                    case PlatformConfig.ConfigType.NEUTRAL_TRUCK:
                        PopulateNeutralTruck(xmlDoc, node, (NeutralTruckConfig)p);
                        break;
                    case PlatformConfig.ConfigType.BLUE_POLICE:
                        PopulateBluePolice(xmlDoc, node, (BluePoliceConfig)p);
                        break;
                    default:
                        Log.error("SoaConfigXMLWriter::PopulateLocal(): Unrecognized config type " + p.GetConfigType() + " in soaConfig.localPlatforms");
                        break;
                }
            }
        }

        private static void PopulateRemote(XmlDocument xmlDoc, XmlNode configNode, SoaConfig soaConfig)
        {
            // Create "Remote" node and append to configNode
            XmlNode node = xmlDoc.CreateElement("Remote");
            configNode.AppendChild(node);

            // Go through each remote platform and add children to "Remote" node
            foreach (PlatformConfig p in soaConfig.remotePlatforms)
            {
                switch (p.GetConfigType())
                {
                    case PlatformConfig.ConfigType.HEAVY_UAV:
                        PopulateHeavyUAV(xmlDoc, node, (HeavyUAVConfig)p);
                        break;
                    case PlatformConfig.ConfigType.SMALL_UAV:
                        PopulateSmallUAV(xmlDoc, node, (SmallUAVConfig)p);
                        break;
                    case PlatformConfig.ConfigType.BLUE_BALLOON:
                        PopulateBlueBalloon(xmlDoc, node, (BlueBalloonConfig)p);
                        break;
                    default:
                        Log.error("SoaConfigXMLWriter::PopulateRemote(): Unrecognized config type " + p.GetConfigType() + " in soaConfig.remotePlatforms");
                        break;
                }
            }
        }

        private static void AddPerceptionOverride(XmlDocument xmlDoc, XmlNode parentNode, PlatformConfig c)
        {
            // Add sensor if overridden
            if (!c.GetUseDefaultSensorModalities())
            {
                XmlNode childNode = xmlDoc.CreateElement("Sensor");
                parentNode.AppendChild(childNode);
                PopulatePerceptionModes(xmlDoc, childNode, c.GetSensorModalities());
            }

            // Add classifier if overridden
            if (!c.GetUseDefaultClassifierModalities())
            {
                XmlNode childNode = xmlDoc.CreateElement("Classifier");
                parentNode.AppendChild(childNode);
                PopulatePerceptionModes(xmlDoc, childNode, c.GetClassifierModalities());
            }
        }

        #region Local Platforms
        private static void PopulateRedDismount(XmlDocument xmlDoc, XmlNode parentNode, RedDismountConfig c)
        {
            // Create "Red Dismount" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("RedDismount");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddOptionalAttribute(xmlDoc, node, "id", c.id);
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            AddOptionalAttribute(xmlDoc, node, "initialWaypoint", c.initialWaypoint);
            AddOptionalAttribute(xmlDoc, node, "hasWeapon", c.hasWeapon);
            AddOptionalAttribute(xmlDoc, node, "commsRange_km", c.commsRange_km);
            AddOptionalAttribute(xmlDoc, node, "numStorageSlots", c.numStorageSlots);

            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }

        private static void PopulateRedTruck(XmlDocument xmlDoc, XmlNode parentNode, RedTruckConfig c)
        {
            // Create "Red Truck" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("RedTruck");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddOptionalAttribute(xmlDoc, node, "id", c.id);
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            AddOptionalAttribute(xmlDoc, node, "initialWaypoint", c.initialWaypoint);
            AddOptionalAttribute(xmlDoc, node, "hasWeapon", c.hasWeapon);
            AddOptionalAttribute(xmlDoc, node, "hasJammer", c.hasJammer);
            AddOptionalAttribute(xmlDoc, node, "commsRange_km", c.commsRange_km);
            AddOptionalAttribute(xmlDoc, node, "jammerRange_km", c.jammerRange_km);
            AddOptionalAttribute(xmlDoc, node, "numStorageSlots", c.numStorageSlots);

            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }

        private static void PopulateNeutralDismount(XmlDocument xmlDoc, XmlNode parentNode, NeutralDismountConfig c)
        {
            // Create "NeutralDismount" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("NeutralDismount");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddOptionalAttribute(xmlDoc, node, "id", c.id);
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            
            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }

        private static void PopulateNeutralTruck(XmlDocument xmlDoc, XmlNode parentNode, NeutralTruckConfig c)
        {
            // Create "NeutralTruck" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("NeutralTruck");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddOptionalAttribute(xmlDoc, node, "id", c.id);
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);

            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }

        private static void PopulateBluePolice(XmlDocument xmlDoc, XmlNode parentNode, BluePoliceConfig c)
        {
            // Create "BluePolice" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("BluePolice");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddOptionalAttribute(xmlDoc, node, "id", c.id);
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            AddOptionalAttribute(xmlDoc, node, "commsRange_km", c.commsRange_km);
    
            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }
        #endregion

        #region Remote Platforms
        private static void PopulateHeavyUAV(XmlDocument xmlDoc, XmlNode parentNode, HeavyUAVConfig c)
        {
            // Create "HeavyUAV" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("HeavyUAV");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "y_km", c.y_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddAttribute(xmlDoc, node, "id", c.id.ToString());
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            AddOptionalAttribute(xmlDoc, node, "commsRange_km", c.commsRange_km);
            AddOptionalAttribute(xmlDoc, node, "fuelTankSize_s", c.fuelTankSize_s);
            AddOptionalAttribute(xmlDoc, node, "numStorageSlots", c.numStorageSlots);

            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }

        private static void PopulateSmallUAV(XmlDocument xmlDoc, XmlNode parentNode, SmallUAVConfig c)
        {
            // Create "SmallUAV" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("SmallUAV");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "x_km", c.x_km.ToString());
            AddAttribute(xmlDoc, node, "y_km", c.y_km.ToString());
            AddAttribute(xmlDoc, node, "z_km", c.z_km.ToString());
            AddAttribute(xmlDoc, node, "id", c.id.ToString());
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            AddOptionalAttribute(xmlDoc, node, "commsRange_km", c.commsRange_km);
            AddOptionalAttribute(xmlDoc, node, "fuelTankSize_s", c.fuelTankSize_s);

            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }

        private static void PopulateBlueBalloon(XmlDocument xmlDoc, XmlNode parentNode, BlueBalloonConfig c)
        {
            // Create "BlueBalloon" node and append to parentNode
            XmlNode node = xmlDoc.CreateElement("BlueBalloon");
            parentNode.AppendChild(node);

            // Add attributes
            AddAttribute(xmlDoc, node, "id", c.id.ToString());
            AddOptionalAttribute(xmlDoc, node, "sensorBeamwidth_deg", c.sensorBeamwidth_deg);
            AddAttribute(xmlDoc, node, "teleportLoop", c.teleportLoop.ToString());

            // Add waypoints
            foreach (PrimitivePair<float, float> waypoint in c.waypoints_km)
            {
                XmlNode waypointNode = xmlDoc.CreateElement("Waypoint");
                node.AppendChild(waypointNode);
                AddAttribute(xmlDoc, waypointNode, "x_km", waypoint.first.ToString());
                AddAttribute(xmlDoc, waypointNode, "z_km", waypoint.second.ToString());
            }

            // Add perception override (if specified)
            AddPerceptionOverride(xmlDoc, node, c);
        }
        #endregion
    }
}
