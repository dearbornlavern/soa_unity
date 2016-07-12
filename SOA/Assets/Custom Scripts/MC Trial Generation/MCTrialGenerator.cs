using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace soa
{
    public class MCTrialGenerator
    {
        /********************* GENERATOR *********************/
        // Random seed for MC trial generation (not runtime computations)
        const int generatorRandomSeed = 2;

        /******************** MONTE CARLO ********************/
        const int numMCTrials = 10;
        const string soaConfigOutputPath = "../../../../GeneratedFiles";
        const string soaConfigFileHeader = "MCConfig_";

        /******************** ENVIRONMENT ********************/
        const string envConfigFile = "../../../../../SOA/Assets/Custom Scripts/MC Trial Generation/SoaEnvConfig.xml";

        /********************** NETWORK **********************/
        const string networkRedRoomHeader = "soa-mc-red_";
        const string networkBlueRoomHeader = "soa-mc-blue_";

        /********************** LOGGER ***********************/
        const string soaLoggerFileHeader = "MCOutput_";
        const bool logEvents = true;
        const bool logToUnityConsole = true;

        /******************** SIMULATION *********************/
        const int simulationRandomSeed = 1;
        const float gameDurationHr = 15.0f;
        const float probRedDismountHasWeapon = 0.5f;
        const float probRedTruckHasWeapon = 0.5f;
        const float probRedTruckHasJammer = 0.5f;

        /*********************** FUEL ************************/
        const float defaultHeavyUAVFuelTankSize_s = 10000;
        const float defaultSmallUAVFuelTankSize_s = 40000;

        /*********************** STORAGE ************************/
        const int defaultHeavyUAVStorageSlots = 1;
        const int defaultRedDismountStorageSlots = 1;
        const int defaultRedTruckStorageSlots = 1;

        /******************** REMOTE ID **********************/
        const int remoteStartID = 100;

        /****************** LOCAL PLATFORMS ******************/
        // Red dismounts 
        private Platform2DLaydown redDismountLaydown;

        // Red trucks
        private Platform2DLaydown redTruckLaydown;
   
        // Neutral dismounts
        private Platform2DLaydown neutralDismountLaydown;
       
        // Neutral trucks
        private Platform2DLaydown neutralTruckLaydown;
    
        // Blue police
        private Platform2DLaydown bluePoliceLaydown;

        /***************** REMOTE PLATFORMS ******************/
        // ID
        private int availableRemoteID;

        // Heavy UAVs
        private Platform3DLaydown heavyUAVLaydown;

        // Small UAVs
        private Platform3DLaydown smallUAVLaydown;

        // No balloon laydown

        /***************** SITE LAYDOWN **********************/
        private List<SiteConfig> blueBases;
        private List<SiteConfig> redBases;
        private List<SiteConfig> ngoSites;
        private List<SiteConfig> villages;

        /***************** CLASS DEFINITION ******************/
        private GridMath gridMath;
        private HashSet<PrimitivePair<int, int>> landCells;
        private HashSet<PrimitivePair<int, int>> landAndWaterCells;
        private EnvConfig envConfig;
        private Random rand;
        public MCTrialGenerator()
        {
            // Read in envConfig
            envConfig = EnvConfigXMLReader.Parse(envConfigFile);
            if (envConfig == null)
            {
                Log.debug("MCTrialGenerator::Main(): Parsed envConfig is null, exiting");
                return;
            }

            // Initialize GridMath object
            gridMath = new GridMath(envConfig.gridOrigin_x, envConfig.gridOrigin_z, envConfig.gridToWorldScale);

            // Populate hashsets of cells for easy lookup
            landCells = new HashSet<PrimitivePair<int, int>>(envConfig.landCells);
            landAndWaterCells = new HashSet<PrimitivePair<int, int>>(envConfig.landCells);
            landAndWaterCells.UnionWith(envConfig.waterCells);
            
            // Random generator for determining whether a unit has weapons or jammers etc.
            rand = new Random(generatorRandomSeed);
        }

        #region Perception Defaults
        private void SetSensorDefaults(SoaConfig soaConfig)
        {
            // Pointer
            List<PerceptionModality> modes;
            
            // Red Dismount
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("BluePolice", 1.0f, 5.0f));
            modes.Add(new PerceptionModality("HeavyUAV",   1.0f, 5.0f));
            modes.Add(new PerceptionModality("SmallUAV",   1.0f, 4.5f));
            soaConfig.defaultSensorModalities.Add("RedDismount", modes);

            // Red Truck
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("BluePolice", 1.0f, 5.0f));
            modes.Add(new PerceptionModality("HeavyUAV",   1.0f, 5.0f));
            modes.Add(new PerceptionModality("SmallUAV",   1.0f, 4.5f));
            soaConfig.defaultSensorModalities.Add("RedTruck", modes);

            // Neutral Dismount
            modes = new List<PerceptionModality>();
            soaConfig.defaultSensorModalities.Add("NeutralDismount", modes);

            // Neutral Truck
            modes = new List<PerceptionModality>();
            soaConfig.defaultSensorModalities.Add("NeutralTruck", modes);

            // Blue Police
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     0.5f, 0.5f));
            modes.Add(new PerceptionModality("RedTruck",        0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralDismount", 0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralTruck",    0.5f, 0.5f));
            soaConfig.defaultSensorModalities.Add("BluePolice", modes);

            // Heavy UAV
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     0.5f, 0.5f));
            modes.Add(new PerceptionModality("RedTruck",        0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralDismount", 0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralTruck",    0.5f, 0.5f));
            soaConfig.defaultSensorModalities.Add("HeavyUAV", modes);
            soaConfig.defaultSensorBeamwidths.Add("HeavyUAV", 360);

            // Small UAV
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     1.0f, 5.0f));
            modes.Add(new PerceptionModality("RedTruck",        2.0f, 7.0f));
            modes.Add(new PerceptionModality("NeutralDismount", 1.0f, 5.0f));
            modes.Add(new PerceptionModality("NeutralTruck",    2.0f, 7.0f));
            soaConfig.defaultSensorModalities.Add("SmallUAV", modes);
            soaConfig.defaultSensorBeamwidths.Add("SmallUAV", 20);

            // Blue Balloon
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     1e20f,1e20f));
            modes.Add(new PerceptionModality("RedTruck",        1e20f,1e20f));
            modes.Add(new PerceptionModality("NeutralDismount", 1e20f,1e20f));
            modes.Add(new PerceptionModality("NeutralTruck",    1e20f,1e20f));
            soaConfig.defaultSensorModalities.Add("BlueBalloon", modes);
            soaConfig.defaultSensorBeamwidths.Add("BlueBalloon", 4);
        }

        private void SetClassifierDefaults(SoaConfig soaConfig)
        {
            // Pointer
            List<PerceptionModality> modes;

            // Red Dismount
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("BluePolice",      5.0f, 5.0f));
            modes.Add(new PerceptionModality("HeavyUAV",        5.0f, 5.0f));
            modes.Add(new PerceptionModality("SmallUAV",        4.5f, 4.5f));
            soaConfig.defaultClassifierModalities.Add("RedDismount", modes);

            // Red Truck
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("BluePolice",      5.0f, 5.0f));
            modes.Add(new PerceptionModality("HeavyUAV",        5.0f, 5.0f));
            modes.Add(new PerceptionModality("SmallUAV",        4.5f, 4.5f));
            soaConfig.defaultClassifierModalities.Add("RedTruck", modes);

            // Neutral Dismount
            modes = new List<PerceptionModality>();
            soaConfig.defaultClassifierModalities.Add("NeutralDismount", modes);

            // Neutral Truck
            modes = new List<PerceptionModality>();
            soaConfig.defaultClassifierModalities.Add("NeutralTruck", modes);

            // Blue Police
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     0.5f, 0.5f));
            modes.Add(new PerceptionModality("RedTruck",        0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralDismount", 0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralTruck",    0.5f, 0.5f));
            soaConfig.defaultClassifierModalities.Add("BluePolice", modes);

            // Heavy UAV
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     0.5f, 0.5f));
            modes.Add(new PerceptionModality("RedTruck",        0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralDismount", 0.5f, 0.5f));
            modes.Add(new PerceptionModality("NeutralTruck",    0.5f, 0.5f));
            soaConfig.defaultClassifierModalities.Add("HeavyUAV", modes);

            // Small UAV
            modes = new List<PerceptionModality>();
            modes.Add(new PerceptionModality("RedDismount",     0.5f, 5.0f));
            modes.Add(new PerceptionModality("RedTruck",        0.5f, 7.0f));
            modes.Add(new PerceptionModality("NeutralDismount", 0.5f, 5.0f));
            modes.Add(new PerceptionModality("NeutralTruck",    0.5f, 7.0f));
            soaConfig.defaultClassifierModalities.Add("SmallUAV", modes);

            // Blue Balloon
            modes = new List<PerceptionModality>();
            soaConfig.defaultClassifierModalities.Add("BlueBalloon", modes);
        }
        #endregion

        #region Comms Defaults
        public void SetCommsDefaults(SoaConfig soaConfig)
        {
            soaConfig.defaultCommsRanges["BlueBase"] = 10.0f;
            soaConfig.defaultCommsRanges["RedDismount"] = 5.0f;
            soaConfig.defaultCommsRanges["RedTruck"] = 5.0f;
            soaConfig.defaultCommsRanges["BluePolice"] = 10.0f;
            soaConfig.defaultCommsRanges["HeavyUAV"] = 10.0f;
            soaConfig.defaultCommsRanges["SmallUAV"] = 10.0f;
        }

        public void SetJammerDefaults(SoaConfig soaConfig)
        {
            soaConfig.defaultJammerRanges["RedTruck"] = 2.0f;
        }
        #endregion

        #region Platform Laydown
        public void InitializeLocalLaydown()
        {
            // Red dismounts
            redDismountLaydown = new Platform2DLaydown();
            redDismountLaydown.numMean = 3;
            redDismountLaydown.numStdDev = 2;
            redDismountLaydown.numMin = 1;
            redDismountLaydown.numMax = 10;
            redDismountLaydown.fromAnchorStdDev_km = 1;
            redDismountLaydown.fromAnchorMax_km = 2;
            redDismountLaydown.anchors = extractSiteLocations(redBases);
            redDismountLaydown.allowedCells = landCells;
            
            // Red trucks
            redTruckLaydown = new Platform2DLaydown();
            redTruckLaydown.numMean = 3;
            redTruckLaydown.numStdDev = 2;
            redTruckLaydown.numMin = 1;
            redTruckLaydown.numMax = 10;
            redTruckLaydown.fromAnchorStdDev_km = 1;
            redTruckLaydown.fromAnchorMax_km = 2;
            redTruckLaydown.anchors = extractSiteLocations(redBases);
            redTruckLaydown.allowedCells = landCells;

            // Merge list of NGO and villages
            List<SiteConfig> neutralSites = new List<SiteConfig>();
            neutralSites.AddRange(ngoSites);
            neutralSites.AddRange(villages);

            // Neutral dismounts
            neutralDismountLaydown = new Platform2DLaydown();
            neutralDismountLaydown.numMean = 2;
            neutralDismountLaydown.numStdDev = 2;
            neutralDismountLaydown.numMin = 0;
            neutralDismountLaydown.numMax = 4;
            neutralDismountLaydown.fromAnchorStdDev_km = 2;
            neutralDismountLaydown.fromAnchorMax_km = 5;
            neutralDismountLaydown.anchors = extractSiteLocations(neutralSites);
            neutralDismountLaydown.allowedCells = landCells;

            // Neutral trucks
            neutralTruckLaydown = new Platform2DLaydown();
            neutralTruckLaydown.numMean = 2;
            neutralTruckLaydown.numStdDev = 2;
            neutralTruckLaydown.numMin = 0;
            neutralTruckLaydown.numMax = 4;
            neutralTruckLaydown.fromAnchorStdDev_km = 2;
            neutralTruckLaydown.fromAnchorMax_km = 5;
            neutralTruckLaydown.anchors = extractSiteLocations(neutralSites);
            neutralTruckLaydown.allowedCells = landCells;

            // Blue police
            bluePoliceLaydown = new Platform2DLaydown();
            bluePoliceLaydown.numMean = 1;
            bluePoliceLaydown.numStdDev = 0f;
            bluePoliceLaydown.numMin = 1;
            bluePoliceLaydown.numMax = 1;
            bluePoliceLaydown.fromAnchorStdDev_km = 2;
            bluePoliceLaydown.fromAnchorMax_km = 5;
            bluePoliceLaydown.anchors = extractSiteLocations(blueBases);
            bluePoliceLaydown.allowedCells = landCells;
        }

        public void InitializeRemoteLaydown()
        {
            // Heavy UAV
            heavyUAVLaydown = new Platform3DLaydown();
            heavyUAVLaydown.numMean = 3.5f;
            heavyUAVLaydown.numStdDev = 1.0f;
            heavyUAVLaydown.numMin = 2;
            heavyUAVLaydown.numMax = 5;
            heavyUAVLaydown.fromAnchorStdDev_km = 5;
            heavyUAVLaydown.fromAnchorMax_km = 15;
            heavyUAVLaydown.altitudeMean_km = 0.25f;
            heavyUAVLaydown.altitudeStdDev_km = 0.25f;
            heavyUAVLaydown.altitudeMin_km = 0.0f;
            heavyUAVLaydown.altitudeMax_km = 0.5f;
            heavyUAVLaydown.anchors = extractSiteLocations(blueBases);
            heavyUAVLaydown.allowedCells = landAndWaterCells;

            // Small UAV
            smallUAVLaydown = new Platform3DLaydown();
            smallUAVLaydown.numMean = 3.5f;
            smallUAVLaydown.numStdDev = 1.0f;
            smallUAVLaydown.numMin = 2;
            smallUAVLaydown.numMax = 5;
            smallUAVLaydown.fromAnchorStdDev_km = 5;
            smallUAVLaydown.fromAnchorMax_km = 15;
            smallUAVLaydown.altitudeMean_km = 2.5f;
            smallUAVLaydown.altitudeStdDev_km = 2.5f;
            smallUAVLaydown.altitudeMin_km = 0.0f;
            smallUAVLaydown.altitudeMax_km = 5.0f;
            smallUAVLaydown.anchors = extractSiteLocations(blueBases);
            smallUAVLaydown.allowedCells = landAndWaterCells;
        }
        #endregion

        #region Site Locations
        public void SetSiteLocations(SoaConfig soaConfig)
        {
            // Blue Base
            blueBases = new List<SiteConfig>();
            blueBases.Add(new BlueBaseConfig(-3.02830208f, -9.7492255f, "Blue Base", new Optional<float>(soaConfig.defaultCommsRanges["BlueBase"])));

            // Red Base
            redBases = new List<SiteConfig>();
            redBases.Add(new RedBaseConfig(-17.7515077f, 13.7463599f, "Red Base 0"));
            redBases.Add(new RedBaseConfig(-0.439941213f, 12.7518844f, "Red Base 1"));
            redBases.Add(new RedBaseConfig(22.0787984f,   10.7493104f, "Red Base 2"));

            // NGO Site
            ngoSites = new List<SiteConfig>();
            ngoSites.Add(new NGOSiteConfig(-21.2181483f, -1.25010618f, "NGO Site 0"));
            ngoSites.Add(new NGOSiteConfig(-4.76803318f, -5.74888572f, "NGO Site 1"));
            ngoSites.Add(new NGOSiteConfig(11.6916982f,   4.76402643f, "NGO Site 2"));
            ngoSites.Add(new NGOSiteConfig(24.6807822f,  -6.75057337f, "NGO Site 3"));

            // Village
            villages = new List<SiteConfig>();
            villages.Add(new VillageConfig(-16.0197901f, 5.74808437f, "Village 0"));
            villages.Add(new VillageConfig(-14.296086f,  -3.22944096f, "Village 1"));
            villages.Add(new VillageConfig(-5.63349131f,  4.74559538f, "Village 2"));
            villages.Add(new VillageConfig(7.34998325f, -0.743652906f, "Village 3"));
            villages.Add(new VillageConfig(-13.4306279f, -11.7638197f, "Village 4"));

            // Add all to soaConfig
            soaConfig.sites.AddRange(blueBases);
            soaConfig.sites.AddRange(redBases);
            soaConfig.sites.AddRange(ngoSites);
            soaConfig.sites.AddRange(villages);
        }

        #endregion

        #region MC Trial Generation
        public void GenerateConfigFiles()
        {
            // Find out trial number formatting
            int numTrialDigits = numMCTrials.ToString().Length;
            string toStringFormat = "D" + numTrialDigits.ToString();

            // Generate each trial
            SoaConfig soaConfig;
            List<PrimitiveTriple<float, float, float>> randomizedPositions;
            for (int trial = 1; trial <= numMCTrials; trial++)
            {
                // Status message
                Log.debug("Generating config file " + soaConfigFileHeader + trial.ToString(toStringFormat) + ".xml");

                // Initialize remote platform ID assignment
                availableRemoteID = remoteStartID;

                // Create a new SoaConfig object
                soaConfig = new SoaConfig();

                // Populate network
                soaConfig.networkRedRoom = networkRedRoomHeader + trial.ToString(toStringFormat);
                soaConfig.networkBlueRoom = networkBlueRoomHeader + trial.ToString(toStringFormat);

                // Logger configuration
                soaConfig.loggerOutputFile = soaLoggerFileHeader + trial.ToString(toStringFormat) + ".xml";
                soaConfig.enableLogToFile = true;
                soaConfig.enableLogEventsToFile = logEvents;
                soaConfig.enableLogToUnityConsole = logToUnityConsole;

                // Simulation configuration
                soaConfig.simulationRandomSeed = simulationRandomSeed;
                soaConfig.gameDurationHr = gameDurationHr;
                soaConfig.probRedDismountHasWeapon = probRedDismountHasWeapon;
                soaConfig.probRedTruckHasWeapon = probRedTruckHasWeapon;
                soaConfig.probRedTruckHasJammer = probRedTruckHasJammer;

                // Populate fuel defaults
                soaConfig.defaultSmallUAVFuelTankSize_s = defaultSmallUAVFuelTankSize_s;
                soaConfig.defaultHeavyUAVFuelTankSize_s = defaultHeavyUAVFuelTankSize_s;

                // Populate storage defaults
                soaConfig.defaultHeavyUAVNumStorageSlots = defaultHeavyUAVStorageSlots;
                soaConfig.defaultRedDismountNumStorageSlots = defaultRedDismountStorageSlots;
                soaConfig.defaultRedTruckNumStorageSlots = defaultRedTruckStorageSlots;

                // Set sensor and classifier defaults
                SetSensorDefaults(soaConfig);
                SetClassifierDefaults(soaConfig);

                // Set comms and jammer defaults
                SetCommsDefaults(soaConfig);
                SetJammerDefaults(soaConfig);

                // Initialize and set site locations
                SetSiteLocations(soaConfig);

                // Initialize platform laydown
                InitializeLocalLaydown();
                InitializeRemoteLaydown();

                // Local units: Red Dismount
                randomizedPositions = redDismountLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.localPlatforms.Add(new RedDismountConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        new Optional<int>(), // id
                        new Optional<float>(), // sensorBeamwidth_deg
                        new Optional<string>(), // initialWaypoint
                        new Optional<bool>(), // hasWeapon
                        new Optional<float>(), // commsRange_km
                        new Optional<int>() // numStorageSlots
                        ));
                }

                // Local units: Red Truck
                randomizedPositions = redTruckLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.localPlatforms.Add(new RedTruckConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        new Optional<int>(), // id
                        new Optional<float>(), // sensorBeamwidth_deg
                        new Optional<string>(), // initialWaypoint
                        new Optional<bool>(), // hasWeapon
                        new Optional<bool>(), // hasJammer
                        new Optional<float>(), // commsRange_km
                        new Optional<float>(), // jammerRange_km
                        new Optional<int>() // numStorageSlots
                        ));
                }

                // Local units: Neutral Dismount
                randomizedPositions = neutralDismountLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.localPlatforms.Add(new NeutralDismountConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        new Optional<int>(), // id
                        new Optional<float>() // sensorBeamwidth_deg
                        ));
                }

                // Local units: Neutral Truck
                randomizedPositions = neutralTruckLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.localPlatforms.Add(new NeutralTruckConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        new Optional<int>(), // id
                        new Optional<float>() // sensorBeamwidth_deg
                        ));
                }

                // Local units: Blue Police
                randomizedPositions = bluePoliceLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.localPlatforms.Add(new BluePoliceConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        new Optional<int>(), // id
                        new Optional<float>(), // sensorBeamwidth_deg
                        new Optional<float>() // commsRange_km
                        ));
                }

                // Remote units: Heavy UAV
                randomizedPositions = heavyUAVLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.remotePlatforms.Add(new HeavyUAVConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        availableRemoteID++, // id
                        new Optional<float>(), // sensorBeamwidth_deg
                        new Optional<float>(), // commsRange_km
                        new Optional<float>(), // fuelTankSize_s
                        new Optional<int>() // numStorageSlots
                        ));
                }

                // Remote units: Small UAV
                randomizedPositions = smallUAVLaydown.Generate(gridMath, rand);
                for (int i = 0; i < randomizedPositions.Count; i++)
                {
                    soaConfig.remotePlatforms.Add(new SmallUAVConfig(
                        randomizedPositions[i].first, // x
                        randomizedPositions[i].second, // y
                        randomizedPositions[i].third,  // z
                        availableRemoteID++, // id
                        new Optional<float>(), // sensorBeamwidth_deg
                        new Optional<float>(), // commsRange_km
                        new Optional<float>() // fuelTankSize_s
                        ));
                }

                // Remote units: Blue Balloon
                List<PrimitivePair<float, float>> balloonWaypoints = new List<PrimitivePair<float, float>>();
                balloonWaypoints.Add(new PrimitivePair<float, float>(-28, 0));
                balloonWaypoints.Add(new PrimitivePair<float, float>(29, 0));
                soaConfig.remotePlatforms.Add(new BlueBalloonConfig(
                    availableRemoteID++, // id
                    new Optional<float>(), // sensorBeamwidth_deg
                    balloonWaypoints,
                    true
                    ));

                // Create output directory if it does not already exist and Write SoaConfig contents to a config file
                Directory.CreateDirectory(soaConfigOutputPath);
                SoaConfigXMLWriter.Write(soaConfig, Path.Combine(soaConfigOutputPath,soaConfigFileHeader + trial.ToString(toStringFormat) + ".xml"));
            } // End current trial
        }
        #endregion

        #region Helper Functions
        /****************** MAIN FUNCTION ********************/
        private List<PrimitivePair<float,float>> extractSiteLocations(List<SiteConfig> sites)
        {
            List<PrimitivePair<float,float>> locations = new List<PrimitivePair<float, float>>();
            foreach (SiteConfig site in sites){
                locations.Add(new PrimitivePair<float, float>(site.x_km, site.z_km));
            }
            return locations;
        }
        #endregion

        #region Main Function
        /****************** MAIN FUNCTION ********************/
        public static void Main()
        {
            MCTrialGenerator mcTrialGenerator = new MCTrialGenerator();

            mcTrialGenerator.GenerateConfigFiles();
        }
        #endregion
    }
}
