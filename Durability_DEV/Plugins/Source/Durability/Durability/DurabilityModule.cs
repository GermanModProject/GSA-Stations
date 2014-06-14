using System;
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
        //[KSPField(isPersistant = false, guiActive = true, guiName = "Quality ", guiUnits = "%", guiFormat = "0.00")]
        public double displayQuality;

        /// <summary>
        /// Quality Display
        /// </summary>
        //[KSPField(isPersistant = false, guiActive = true, guiName = "Durability ", guiUnits = "%", guiFormat = "0.00")]
        public double displayDurability;

        /// <summary>
        /// Quality from Part 0.0f - 1.0f; Default: 0.5f 
        /// </summary>
        [KSPField(isPersistant = true)]
        public double quality = 0.5f;

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

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 1f)]
        public void RepairDamage()
        {
            setEventLabel();
            if (Events == null || !canRepair)
                return;
            if (part.Resources.Contains("Durability"))
            {
                double difference = part.Resources["Durability"].maxAmount - part.Resources["Durability"].amount;
                Debug.Log("KspDeMod [RepairDamage]: Start Repair (" + difference.ToString("0.0000") + ")");
                if (difference > 0)
                {
                    part.Resources["Durability"].amount = part.Resources["Durability"].maxAmount;
                    double differenceP = difference / part.Resources["Durability"].maxAmount;
                    Debug.Log("KspDeMod [RepairDamage]: Repair differenceP (" + differenceP.ToString("0.0000") + ")");
                    quality -= repairQualityReducer * differenceP;
                    if (quality < 0.01d)
                    {
                        quality = 0.01d;
                    }
                }
            }
            else
            {
                Debug.Log("KspDeMod [RepairDamage]: Resource Durability not Found");
            }
            setEventLabel();
            if (myWindow != null)
                myWindow.displayDirty = true;
        }

        public override void OnAwake()
        {
            Debug.Log("KspDeMod [OnAwake]: " + this.name);
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            Debug.Log("KspDeMod: [OnStart]: " + this.name);
            base.OnStart(state);
            if (state == StartState.Editor)
            {
                return;
            }
            if (quality > 0)
            {
                displayQuality = quality * 100;
            }
            displayDurability = GetDurabilityPercent();
            displayQuality = quality * 100;

            if (part.Resources.Contains("Quality"))
            {
                if (part.Resources["Quality"].amount > 100)
                {
                   // part.Resources["Quality"].amount = 100d;
                }
                if (part.Resources["Quality"].maxAmount > 100)
                {
                   // part.Resources["Quality"].maxAmount = 100d;
                }
                if (quality > 0)
                {
                  //  part.Resources["Quality"].amount = displayQuality;
                }
                else 
                { 
                   // quality = part.Resources["Quality"].amount / 100;
                }
            }

            setEventLabel();

            Debug.Log("KspDeMod: [checkStatus]: quality:" + quality.ToString());
            Debug.Log("KspDeMod: [checkStatus]: displayQuality:" + displayQuality.ToString());
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            checkStatus();
            lastUpdate = vessel.missionTime;
            if (part.Resources.Contains("Durability"))
            {
                if (part.Resources["Durability"].amount != 0)
                { 
                    part.Resources["Durability"].amount -= (1.001d - quality) * damageRate * (Planetarium.TimeScale * Time.deltaTime);
                }
                if (part.Resources["Durability"].amount < 0)
                {
                    part.Resources["Durability"].amount = 0;
                }
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            checkFixedStatus();
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
            displayQuality = quality * 100;

            part.Resources["Quality"].amount = displayQuality;
            
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

        private void setEventLabel()
        {
            if (Events == null)
                return;
            try
            {
                if (displayDurability < 100 && canRepair)
                {
                    Events["RepairDamage"].guiName = "Repair";
                    Events["RepairDamage"].guiActive = true;
                }
                else if (!canRepair)
                {
                    Events["RepairDamage"].guiName = "Can not Repair";
                    Events["RepairDamage"].guiActive = false;
                    //Events["RepairDamage"].active = false;
                }
                else if (quality <= 0.01d)
                {
                    quality = 0.01d;
                    canRepair = false;
                    Events["RepairDamage"].guiName = "Quality to low! Can not Repair";
                    //Events["RepairDamage"].active = false;
                    Events["RepairDamage"].guiActive = false;
                }
                else
                {
                    Events["RepairDamage"].guiName = "No Damage";
                    Events["RepairDamage"].guiActive = false;
                }
            }
            catch(Exception ex)
            {
                Debug.LogError("KspDeMod: [setEventLabel]: " + ex.Message);
            }
        }

        /// <summary>
        /// Check durability status (Physic)
        /// </summary>
        private void checkFixedStatus()
        {
           
        }
        
        
        /// <summary>
        /// Get the current Durability as percent
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
                Debug.Log("KspDeMod [GetDurabilityPercent]: Resource Durability not Found");
            }
            return percent * 100;
        }
    }
}
