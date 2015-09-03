﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace soa
{
    public class SoaConfig
    {
        // Network configuration
        public string networkRedRoom;
        public string networkBlueRoom;

        // Red platform weapon probability
        public float probRedDismountWeaponized;
        public float probRedTruckWeaponized;

        // Local platforms
        public List<PlatformConfig> localPlatforms;

        // Remote platforms
        public List<PlatformConfig> remotePlatforms;

        public SoaConfig()
        {
            localPlatforms = new List<PlatformConfig>();
            remotePlatforms = new List<PlatformConfig>();
        }
    }

    // Generalized platform
    public abstract class PlatformConfig
    {
        public enum ConfigType {RED_DISMOUNT, RED_TRUCK, NEUTRAL_DISMOUNT, NEUTRAL_TRUCK, BLUE_POLICE, HEAVY_UAV, SMALL_UAV, BALLOON};
        public Vector3 pos;
        public int id;
        public PlatformConfig(float x, float y, float z, int id) {
            this.pos = new Vector3(x, y, z);
            this.id = id;
        }
        public abstract ConfigType GetConfigType();
    }

    // Red dismount config
    public class RedDismountConfig : PlatformConfig
    {
        public bool hasWeapon;
        public string initialWaypoint;
        public RedDismountConfig(float x, float y, float z, int id, string initialWaypoint, bool hasWeapon) 
            : base(x, y, z, id)
        {
            this.initialWaypoint = initialWaypoint;
            this.hasWeapon = hasWeapon;
        }
        public override ConfigType GetConfigType() { return ConfigType.RED_DISMOUNT; }
    }

    // Red truck config
    public class RedTruckConfig : PlatformConfig
    {
        public bool hasWeapon;
        public string initialWaypoint;
        public RedTruckConfig(float x, float y, float z, int id, string initialWaypoint, bool hasWeapon) 
            : base(x, y, z, id)
        {
            this.initialWaypoint = initialWaypoint;
            this.hasWeapon = hasWeapon;
        }
        public override ConfigType GetConfigType() { return ConfigType.RED_TRUCK; }
    }

    // Neutral dismount config
    public class NeutralDismountConfig : PlatformConfig
    {
        public NeutralDismountConfig(float x, float y, float z, int id)
            : base(x, y, z, id) {}
        public override ConfigType GetConfigType() { return ConfigType.NEUTRAL_DISMOUNT; }
    }
    
    // Neutral truck config
    public class NeutralTruckConfig : PlatformConfig
    {
        public NeutralTruckConfig(float x, float y, float z, int id)
            : base(x, y, z, id) { }
        public override ConfigType GetConfigType() { return ConfigType.NEUTRAL_TRUCK; }
    }

    // Blue police config
    public class BluePoliceConfig : PlatformConfig
    {
        public BluePoliceConfig(float x, float y, float z, int id)
            : base(x, y, z, id) { }
        public override ConfigType GetConfigType() { return ConfigType.BLUE_POLICE; }
    }

    // Heavy UAV config
    public class HeavyUAVConfig : PlatformConfig
    {
        public HeavyUAVConfig(float x, float y, float z, int id)
            : base(x, y, z, id) { }
        public override ConfigType GetConfigType() { return ConfigType.HEAVY_UAV; }
    }

    // Small UAV config
    public class SmallUAVConfig : PlatformConfig
    {
        public SmallUAVConfig(float x, float y, float z, int id)
            : base(x, y, z, id) { }
        public override ConfigType GetConfigType() { return ConfigType.SMALL_UAV; }
    }

    // Balloon config
    public class BalloonConfig : PlatformConfig
    {
        public BalloonConfig(float x, float y, float z, int id)
            : base(x, y, z, id) { }
        public override ConfigType GetConfigType() { return ConfigType.BALLOON; }
    }
}