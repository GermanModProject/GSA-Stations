using System;
using UnityEngine;

namespace KspDeMod.Durability
{
    public class DurabilityModule : PartModule
    {
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
        /// Display Temperature
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature", guiUnits = "° C", guiFormat = "F0")]
        public string displayTemperature;

        /// <summary>
        /// Display Geeforce
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Geeforce", guiUnits = "G", guiFormat = "F0")]
        public double displayGeeforce;

        /// <summary>
        /// TempMutli Display Debugger
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Temp Mutli ", guiUnits = "", guiFormat = "F0")]
        public double displayTempMutli;

        /// <summary>
        /// GForece Display Debugger
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "GeeForce Mutli ", guiUnits = "", guiFormat = "F0")]
        public double displayGeeForceMutli;

        /// <summary>
        /// GForece Display Debugger
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "Expiry ", guiUnits = "", guiFormat = "F0")]
        public string displayTime;

        /// <summary>
        /// Quality Display
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Quality ", guiUnits = "%", guiFormat = "0.00")]
        public double displayQuality;

        /// <summary>
        /// Quality Display
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Durability ", guiUnits = "%", guiFormat = "0.00")]
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

        private PartModule deadlyReentry = null;

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = false, externalToEVAOnly = true, guiActive = false, unfocusedRange = 4f)]
        public void RepairDamage()
        {
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

        [KSPEvent(guiName = "Add Temp (Debug)", guiActiveUnfocused = false, externalToEVAOnly = false, guiActive = false, unfocusedRange = 4f, active = false)]
        public void AddTemp()
        {
            if (Events == null)
                return;
            part.temperature += 10;
        }
        [KSPEvent(guiName = "Sub Temp (Debug)", guiActiveUnfocused = false, externalToEVAOnly = false, guiActive = false, unfocusedRange = 4f, active = false)]
        public void SubTemp()
        {
            if (Events == null)
                return;
            part.temperature -= 10;
        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (part && part.Modules != null)
            {
                if (part.Modules.Contains("ModuleAeroReentry"))
                { 
                    deadlyReentry = part.Modules["ModuleAeroReentry"];
                    Fields["displayTemperature"].guiActive = false;
                }
            }
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
            quality = part.Resources["Quality"].amount / 100;

            checkStatus();

            setEventLabel();

            Debug.Log("KspDeMod: [checkStatus]: quality:" + quality.ToString());
            Debug.Log("KspDeMod: [checkStatus]: displayQuality:" + displayQuality.ToString());
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            checkStatus();
            lastUpdate = vessel.missionTime;
            reduceDurability();
            setEventLabel();
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
            displayTemperature = part.temperature.ToString("0.00");
            displayGeeforce = vessel.geeForce;
            if (quality > 0 && quality < 1)
            {
                part.Resources["Quality"].amount = quality * 100;
            } 
            else
            {
                quality = 0.5d;
                part.Resources["Quality"].amount = 50d;
            }
            if (part.Resources.Contains("Durability"))
            {
                if (part.Resources["Durability"].amount <= 0 && canExplode)
                {
                    part.explode();
                }
                else
                {
                    if (part.Resources["Durability"].amount <= minDurability && !part.frozen)
                    {
                        part.freeze();
                        Debug.Log("KspDeMod: [checkStatus]: freeze Part " + part.name);
                    }
                    else if (part.Resources["Durability"].amount > minDurability && part.frozen)
                    {
                        part.unfreeze();
                        Debug.Log("KspDeMod: [checkStatus]: unfreeze Part " + part.name);
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
                if (displayDurability < 100 && canRepair && quality > 0.01)
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
                else if (quality <= 0.01)
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

        private void reduceDurability()
        {
            double reduce = 0;

            if (part.Resources.Contains("Durability"))
            {
                if (part.Resources["Durability"].amount > 0)
                {
                    //Temperature
                    double TempMutli = 0;
                    TempMutli = idealTemp.Evaluate(part.temperature) * (damageRate * damageRate) + 1;
                    displayTempMutli = TempMutli;

                    //GeeForce
                    double GeeForceMutli = 1;
                    if (vessel.geeForce > 1 && vessel.geeForce < 9)
                    {
                        GeeForceMutli = (vessel.geeForce * vessel.geeForce * damageRate) + 1;
                    }
                    else if (vessel.geeForce >= 9)
                    {
                        GeeForceMutli = (vessel.geeForce * vessel.geeForce) + 1;
                    }
                    displayGeeForceMutli = GeeForceMutli;
                    
                    // Default
                    //reduce = (1.001d - quality) * damageRate * (Planetarium.TimeScale * Time.deltaTime);
                    reduce += (1.001d - quality) * damageRate * displayTempMutli * GeeForceMutli * TimeWarp.fixedDeltaTime;

                    displayGeeforce = vessel.geeForce;

                    double secondsToZero = part.Resources["Durability"].amount / (reduce * (1 / TimeWarp.fixedDeltaTime));
                    
                    double KerbalYearSec = (426.08 * 6 * 60 * 60);
                    double KerbalDaySec = (6 * 60 * 60);
                    double KerbalHourSec = (60 * 60);

                    int years = (int)Math.Floor(secondsToZero / KerbalYearSec);
                    secondsToZero -= years * KerbalYearSec;

                    int days = (int)Math.Floor(secondsToZero / KerbalDaySec);
                    secondsToZero -= days * KerbalDaySec;

                    int hours = (int)Math.Floor(secondsToZero / KerbalHourSec);
                    secondsToZero -= hours * KerbalHourSec;

                    int minutes = (int)Math.Floor(secondsToZero / 60);
                    secondsToZero -= minutes * 60;

                    int seconds = (int)Math.Floor(secondsToZero);

                    displayTime = "T+ " + ((years > 0) ? "y" + years.ToString("D2") + ", " : "") +
                        ((days > 0) ? "d" + days.ToString("D2") + ", " : "") +
                        hours.ToString("D2") + ":" + 
                        minutes.ToString("D2") + ":" + 
                        seconds.ToString("D2");
                    
                }
                else if (part.Resources["Durability"].amount < 0)
                {
                    part.Resources["Durability"].amount = 0;
                }
                    
                part.Resources["Durability"].amount -= reduce;
            }
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
