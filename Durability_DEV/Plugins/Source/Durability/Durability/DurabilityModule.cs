using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KspDeMod.Durability
{
    public class DurabilityModule : PartModule
    {
        public DurabilityModule()
        {
            Debug.Log("KspDeMod [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: Constructor");
        }

        UIPartActionWindow _myWindow = null;
        UIPartActionWindow myWindow
        {
            get
            {
                if (_myWindow == null)
                {
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part) _myWindow = window;
                    }
                }
                return _myWindow;
            }
        }

        /// <summary>
        /// Quality Display
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "Quality", guiUnits = "%", guiFormat = "P")]
        public double displayQuality;

        /// <summary>
        /// Quality Display
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "Durability", guiUnits = "%", guiFormat = "P")]
        public double displayDurability;

        /// <summary>
        /// Quality from Part 0.0f - 1.0f; Default: 0.5f 
        /// </summary>
        [KSPField(isPersistant = true)]
        public double quality;

        /// <summary>
        /// Durability from Part
        /// </summary>
        [KSPField(isPersistant = true)]
        public string durability;

        /// <summary>
        /// Minimum Durability
        /// </summary>
        [KSPField(isPersistant = true)]
        public double minDurability = 150f;

        /// <summary>
        /// Part explode, if durability = 0
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool canExplode = false;

        /// <summary>
        /// Can repair the Part on EVA
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool canRepair = true;

        /// <summary>
        /// Reduce Quality Multiplicator
        /// </summary>
        [KSPField(isPersistant = true)]
        public float repairQualityReducer;

        /// <summary>
        /// Max count of remairs (-1 endless)
        /// </summary>
        [KSPField(isPersistant = true)]
        public int maxRepair;

        /// <summary>
        /// Damagerate Multiplicator from Part
        /// </summary>
        [KSPField(isPersistant = true)]
        public float damageRate;

        /// <summary>
        /// Ideal Temperature
        /// </summary>
        [KSPField(isPersistant = true)]
        public FloatCurve idealTemp = new FloatCurve();

        /// <summary>
        /// LAst Update (MissionTime)
        /// </summary>
        [KSPField(isPersistant = true)]
        public double lastUpdate;

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 4f)]
        public void RepairDamage()
        {
            if (Events == null)
                return;
            if (part.Resources.Contains("Durability"))
            {
                double difference = part.Resources["Durability"].maxAmount - part.Resources["Durability"].amount;
                if (difference > 0)
                {
                    part.Resources["Durability"].amount = part.Resources["Durability"].maxAmount;
                    double differenceP = difference / part.Resources["Durability"].maxAmount;
                    quality -= repairQualityReducer * differenceP;
                    if (quality < 1)
                    {
                        quality = 1d;
                        canRepair = false;
                        Events["RepairDamage"].guiName = "Quality to low!";
                    }
                }
            }
            else
            {
                Debug.Log("KspDeMod [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: Resource Durability not Found");
            }

            
            if (myWindow != null)
                myWindow.displayDirty = true;
        }

        public override void OnAwake()
        {
            Debug.Log("KspDeMod [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: OnAwake: " + this.name);
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            Debug.Log("KspDeMod: [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: OnStart: " + this.name);
            base.OnStart(state);
            if (state == StartState.Editor)
            {
                return;
            }
            if (quality > 0)
            {
                displayQuality = quality;
            }
            else 
            {
                displayQuality = 50f;
            }
            displayDurability = GetDurabilityPercent();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            checkStatus();
            lastUpdate = vessel.missionTime;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            checkFixedStatus();
            if (part.Resources.Contains("Durability"))
            {
                part.Resources["Durability"].amount -= (1.001d-quality) * damageRate;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("KspDeMod [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: OnLoad: " + node);
        }

        public override string GetInfo()
        {
            return "Durability";
        }

        /// <summary>
        /// Check durability status
        /// </summary>
        private void checkStatus()
        {
            displayDurability = GetDurabilityPercent();
            if (displayDurability < 1 && canRepair)
            {            
                Events["RepairDamage"].guiName = "Repair Damage";
                Events["RepairDamage"].guiActive = true;
            }
            else
            {
                Events["RepairDamage"].guiName = "No Damage";
                Events["RepairDamage"].guiActive = false;
            }
        }

        /// <summary>
        /// Check durability status (Physic)
        /// </summary>
        private void checkFixedStatus()
        {
            if (part.Resources.Contains("Durability"))
            {
                if (part.Resources["Durability"].amount <= 0 && canExplode)
                {
                    part.explode();
                }
                else
                {
                    if (part.Resources["Durability"].amount <= minDurability)
                    {
                        part.freeze();
                    }
                    else if (part.Resources["Durability"].amount > minDurability && part.frozen)
                    {
                        part.unfreeze();
                    }
                }
            }
        }
        
        
        /// <summary>
        /// Get the current Durability as percent (0.0 - 1.0)
        /// </summary>
        /// <returns></returns>
        public double GetDurabilityPercent()
        {
            double percent = 1.0d;
            if (part.Resources.Contains("Durability"))
            {
                percent = part.Resources["Durability"].amount / part.Resources["Durability"].maxAmount;
            }
            else
            {
                Debug.Log("KspDeMod [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: Resource Durability not Found");
            }
            return percent;
        }
    }
}
