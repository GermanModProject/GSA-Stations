using System;
using System.Collections.Generic;
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

        #region KSPFields

        /// <summary>
        /// Display Pressure
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(damage) ")]
        public string displayDamage;

        /// <summary>
        /// Display Pressure
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(pressure) ")]
        public string displayPressure;

        /// <summary>
        /// Display Temperature
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(engine) ")]
        public string displayEngine;

        /// <summary>
        /// Display GeeForce
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(G) ")]
        public string displayGeeForce;

        /// <summary>
        /// Display GeeForce
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(Temp) ")]
        public string displayTempM;

        /// <summary>
        /// Display ReactionWheel
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(RWheel) ")]
        public string displayReactionWheel;

        /// <summary>
        /// Display Experiment
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "Deb(Expe.) ")]
        public string displayExp;

        /// <summary>
        /// Display Temperature
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature", guiUnits = "° C", guiFormat = "F0")]
        public string displayTemperature;

        /// <summary>
        /// GForece Display Debug
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "Expiry ", guiUnits = "", guiFormat = "F0")]
        public string displayTime;

        /// <summary>
        /// Quality from Part 0.0f - 1.0f; Default: 0.5f 
        /// </summary>
        [KSPField(isPersistant = true)]
        public double quality;

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
        [KSPField(isPersistant = false)]
        public float repairQualityReducer = 0.012f;

        /// <summary>
        /// Max count of remairs (-1 endless)
        /// </summary>
        [KSPField(isPersistant = true)]
        public int maxRepair = -1;

        /// <summary>
        /// Damagerate Multiplicator from Part
        /// </summary>
        [Obsolete("Use basicWear")]
        [KSPField(isPersistant = false)]
        public float damageRate = 0.018f;

        /// <summary>
        /// Damagerate Multiplicator from Part
        /// </summary>
        [KSPField(isPersistant = false)]
        public float basicWear = 0.018f;

        /// <summary>
        /// Damagerate Multiplicator from Engines
        /// </summary>
        [KSPField(isPersistant = false)]
        public float engineWear = 0;

        /// <summary>
        /// Ideal Temperature
        /// </summary>
        [KSPField(isPersistant = false)]
        public FloatCurve idealTemp = new FloatCurve();

        /// <summary>
        /// Ideal Pressure
        /// </summary>
        [KSPField(isPersistant = false)]
        public FloatCurve idealPressure = new FloatCurve();

        /// <summary>
        /// Part explode is higher and durability is 0 (ignore canExplode)
        /// </summary>
        [KSPField(isPersistant = false)]
        public float maxPressure = 20;

        /// <summary>
        /// Last reduce
        /// </summary>
        [KSPField(isPersistant = true, guiActive = false, guiName = "lastReduceRange ")]
        public double lastReduceRange = 0;

        #endregion //KSPFields

        #region Private Fields

        private bool _isInit = false;
        private bool _isFirst = true;
        private int _countUpdates = 0;
        private PartModule _deadlyReentry = null;
        private double _displayDurability;
        private double _lastUpdateTime = 0;
        private bool _expDeployed = false;

        #endregion //Private Fields

        #region KSPEvent

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 4f)]
        public void RepairDamage()
        {
            if (Events == null || !canRepair)
                return;
            if (part.Resources.Contains("Durability"))
            {
                double difference = part.Resources["Durability"].maxAmount - part.Resources["Durability"].amount;
                KspDeMod.Durability.Debug.Log("KspDeMod [RepairDamage]: Start Repair (" + difference.ToString("0.0000") + ")");
                if (difference > 0)
                {
                    part.Resources["Durability"].amount = part.Resources["Durability"].maxAmount;
                    double differenceP = difference / part.Resources["Durability"].maxAmount;
                    KspDeMod.Durability.Debug.Log("KspDeMod [RepairDamage]: Repair differenceP (" + differenceP.ToString("0.0000") + ")");
                    quality -= repairQualityReducer * differenceP;
                    if (quality < 0.01d)
                    {
                        quality = 0.01d;
                    }
                }
            }
            else
            {
                KspDeMod.Durability.Debug.Log("KspDeMod [RepairDamage]: Resource Durability not Found");
            }
            setEventLabel();
            if (myWindow != null)
                myWindow.displayDirty = true;
        }

        #endregion //KSPEvent

        #region Public override methods

        public override void OnAwake()
        {
            base.OnAwake();
            try
            {
                if (part && part.Modules != null)
                {
                    if (part.Modules.Contains("ModuleAeroReentry"))
                    { 
                        _deadlyReentry = part.Modules["ModuleAeroReentry"];
                    }
                }
            }
            catch (Exception ex)
            {
                KspDeMod.Durability.Debug.LogError("KspDeMod: [OnAwake]: Message: " + ex.Message);
                KspDeMod.Durability.Debug.LogError("KspDeMod: [OnAwake]: Source: " + ex.Source);
                KspDeMod.Durability.Debug.LogError("KspDeMod: [OnAwake]: StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: " + this.name);
            base.OnStart(state);
            if (state == StartState.Editor)
            {
                if (part.Resources.Contains("Quality"))
                {
                    part.Resources["Quality"].amount = quality * 100;
                }
                return;
            }
            if (part.Resources.Contains("Quality"))
            {
                quality = part.Resources["Quality"].amount / 100;
            }
            _lastUpdateTime = vessel.missionTime;
            checkStatus();
            setEventLabel();          

            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: quality:" + quality.ToString());
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: damageRate: " + basicWear.ToString());
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: vessel.missionTime: " + vessel.missionTime.ToString());
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: lastReduceRange: " + lastReduceRange.ToString());
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: lastUpdateTime: " + _lastUpdateTime.ToString());
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: maxRepair: " + maxRepair.ToString());
            KspDeMod.Durability.Debug.Log("KspDeMod: [OnStart]: canRepair: " + canRepair.ToString());
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (_isFirst)
            {
                _isFirst = false;
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnUpdate]: Start First Update");
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnUpdate]: vessel.missionTime: " + vessel.missionTime.ToString());
                //Recalculate Durability
                try
                {
                    if (vessel.missionTime > 0 && _lastUpdateTime > 0)
                    {
                        double reReduce = (vessel.missionTime - _lastUpdateTime) * lastReduceRange;
                        KspDeMod.Durability.Debug.Log("KspDeMod: [OnUpdate]: recalculate Duability: " + reReduce.ToString("F0"));
                        if (part.Resources.Contains("Durability") && reReduce > 0)
                        {
                            part.Resources["Durability"].amount -= reReduce;
                        }
                    }
                    _isInit = true;
                }
                catch (Exception ex)
                {
                    KspDeMod.Durability.Debug.LogError("KspDeMod: [OnUpdate] Recalculate: Message: " + ex.Message);
                    KspDeMod.Durability.Debug.LogError("KspDeMod: [OnUpdate] Recalculate: Source: " + ex.Source);
                    KspDeMod.Durability.Debug.LogError("KspDeMod: [OnUpdate] Recalculate: StackTrace: " + ex.StackTrace);
                }
            }

            checkStatus();
            reduceDurability();
            setEventLabel();
            _countUpdates++;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            try
            {
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnSave]: vessel.missionTime: " + vessel.missionTime.ToString());
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnSave]: lastReduceRange: " + lastReduceRange.ToString());
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnSave]: lastUpdateTime: " + _lastUpdateTime.ToString());
            }
            catch { }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                KspDeMod.Durability.Debug.Log(node);
                if (node.HasValue("lastUpdateTime"))
                {
                    double.TryParse(node.GetValue("lastUpdateTime"), out this._lastUpdateTime);
                }
                if (node.HasValue("lastReduceRange"))
                {
                    double.TryParse(node.GetValue("lastReduceRange"), out this.lastReduceRange);
                }

                KspDeMod.Durability.Debug.Log("KspDeMod: [OnLoad]: vessel.missionTime: " + vessel.missionTime.ToString());
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnLoad]: lastReduceRange: " + lastReduceRange.ToString());
                KspDeMod.Durability.Debug.Log("KspDeMod: [OnLoad]: lastUpdateTime: " + _lastUpdateTime.ToString());
            }
            catch { }
        }

        public override string GetInfo()
        {
            return "Durability";
        }

        #endregion //Public override methods

        #region Private methods

        /// <summary>
        /// Check durability status
        /// </summary>
        private void checkStatus()
        {
            _displayDurability = GetDurabilityPercent();
            displayTemperature = part.temperature.ToString("0.00");
            try
            {
                if (part.Resources.Contains("Quality"))
                {
                    if (quality > 0 && quality < 1)
                    {
                        part.Resources["Quality"].amount = quality * 100;
                    }
                    else
                    {
                        quality = 0.5d;
                        part.Resources["Quality"].amount = 50d;
                    }
                }
                if (part.Resources.Contains("Durability"))
                {
                    if (part.Resources["Durability"].amount <= 0 && canExplode)
                    {
                        part.explode();
                    }
                    else
                    {
                        ModuleCommand comand = null;
                        ModuleReactionWheel ReactionWheel = null;
                        ModuleEngines engine = null;
                        if(part.Modules.Contains("ModuleCommand"))
                        {
                            comand = (ModuleCommand)part.Modules["ModuleCommand"];
                        }

                        if (part.Modules.Contains("ModuleReactionWheel"))
                        {
                            ReactionWheel = (ModuleReactionWheel)part.Modules["ModuleReactionWheel"];
                        }

                        if (part.Modules.Contains("ModuleEngines"))
                        {
                            engine = (ModuleEngines)part.Modules["ModuleEngines"];
                        }

                        if (part.Resources["Durability"].amount <= minDurability && !part.frozen)
                        {
                            part.freeze();
                            if (ReactionWheel != null)
                            {
                                ReactionWheel.wheelState = ModuleReactionWheel.WheelState.Broken;
                            }

                            if (engine != null)
                            {
                                if (!engine.engineShutdown)
                                {
                                    engine.Events.Send(engine.Events["Shutdown"].id);
                                }
                                KspDeMod.Durability.Debug.Log("KspDeMod: [checkStatus]: engineShutdown = True");
                                KspDeMod.Durability.Debug.Log(engine);
                            }
                            foreach (PartModule dModules in part.Modules)
                            {
                                if (dModules.moduleName != "DurabilityModule")
                                {
                                    dModules.isEnabled = false;
                                    KspDeMod.Durability.Debug.Log("KspDeMod: [checkStatus]: isEnabled = False:  " + dModules.moduleName);
                                }
                            }
                            KspDeMod.Durability.Debug.Log("KspDeMod: [checkStatus]: freeze Part " + part.name);
                        }
                        else if (part.Resources["Durability"].amount > minDurability && part.frozen)
                        {
                            part.unfreeze();
                            if (ReactionWheel != null)
                            {
                                ReactionWheel.wheelState = ModuleReactionWheel.WheelState.Active;
                            }
                            foreach (PartModule aModules in part.Modules)
                            {
                                if (aModules.moduleName != "DurabilityModule")
                                {
                                    aModules.isEnabled = true;
                                    KspDeMod.Durability.Debug.Log("KspDeMod: [checkStatus]: isEnabled = True:  " + aModules.moduleName);
                                }
                            }
                            KspDeMod.Durability.Debug.Log("KspDeMod: [checkStatus]: unfreeze Part " + part.name);
                        }
                        else if(part.Resources["Durability"].amount == 0)
                        {
                            if(vessel.staticPressure >= maxPressure)
                            {
                                part.explode();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                KspDeMod.Durability.Debug.LogError("KspDeMod: [checkStatus]: Message: " + ex.Message);
                KspDeMod.Durability.Debug.LogError("KspDeMod: [checkStatus]: Source :" + ex.Source);
                KspDeMod.Durability.Debug.LogError("KspDeMod: [checkStatus]: StackTrace: " + ex.StackTrace);
            }
        }

        private void setEventLabel()
        {
            if (Events == null)
                return;
            try
            {
                if (_displayDurability < 100 && canRepair && quality > 0.01)
                {
                    Events["RepairDamage"].guiName = "Repair";
                }
                else if (!canRepair)
                {
                    Events["RepairDamage"].guiName = "Can not Repair";
                }
                else if (quality <= 0.01)
                {
                    quality = 0.01d;
                    canRepair = false;
                    Events["RepairDamage"].guiName = "Quality to low! Can not Repair";
                }
                else
                {
                    Events["RepairDamage"].guiName = "No Damage";
                }
            }
            catch(Exception ex)
            {
                KspDeMod.Durability.Debug.LogError("KspDeMod: [setEventLabel]: Message: " + ex.Message);
                KspDeMod.Durability.Debug.LogError("KspDeMod: [setEventLabel]: Source: " + ex.Source);
                KspDeMod.Durability.Debug.LogError("KspDeMod: [setEventLabel]: StackTrace: " + ex.StackTrace);
            }
        }

        private void reduceDurability()
        {
            double reduce = 0;
            try
            {                
                if (part.Resources.Contains("Durability"))
                {
                    if (part.Resources["Durability"].amount > 0)
                    {
                        // Default
                        reduce = (1.001d - quality) * basicWear;

                        //Temperature
                        double TempMutli = 0;
                        TempMutli = idealTemp.Evaluate(part.temperature);
                        TempMutli = (TempMutli > 1) ? TempMutli : 1;
                        reduce *= TempMutli;
                        displayTempM = TempMutli.ToString("0.0000");

                        //GeeForce
                        double GeeForceMutli = 1;
                        if (vessel.geeForce > 1 && vessel.geeForce < 9)
                        {
                            GeeForceMutli = (vessel.geeForce * vessel.geeForce);
                        }
                        else if (vessel.geeForce >= 9)
                        {
                            GeeForceMutli = ((vessel.geeForce * vessel.geeForce) * 2);
                        }
                        GeeForceMutli = (GeeForceMutli > 1) ? GeeForceMutli : 1;
                        reduce *= GeeForceMutli;
                        displayGeeForce = GeeForceMutli.ToString("0.0000");

                        //Pressure
                        double pressureMulti = 0;
                        float pressure = 1f;
                        pressure = Convert.ToSingle(vessel.staticPressure);
                        pressureMulti = idealPressure.Evaluate(pressure);
                        pressureMulti = (pressureMulti > 1) ? pressureMulti : 1;
                        reduce *= pressureMulti;
                        displayPressure = pressureMulti.ToString("0.0000");

                        //Engine
                        double EngineMutli = 1;                        
                        if( part.Modules.Contains("ModuleEngines"))
                        { 
                            ModuleEngines engine = (ModuleEngines)part.Modules["ModuleEngines"];
                            if (!engine.flameout && !engine.engineShutdown)
                            {
                                EngineMutli = (engineWear * engineWear) * (engine.requestedThrust / engine.maxThrust + 1);
                                EngineMutli = (EngineMutli > 1) ? EngineMutli : 1;
                                reduce *= EngineMutli;
                            }
                        }
                        displayEngine = EngineMutli.ToString("0.0000");

                        //ScienceExperiment ModuleScienceExperiment (Not Multiply, fixed Damage)
                        if (part.Modules.Contains("ModuleScienceExperiment"))
                        {
                            KspDeMod.Durability.Debug.Log("KspDeMod: [reduceDurability]: scienceExperiment: ");
                            ModuleScienceExperiment scienceExperiment = (ModuleScienceExperiment)part.Modules["ModuleScienceExperiment"];
                            if (scienceExperiment.Deployed && !_expDeployed)
                            {
                                _expDeployed = true;
                                KspDeMod.Durability.Debug.Log("KspDeMod: [reduceDurability]: scienceExperiment: ");
                                
                                double experimentDamage = (part.Resources["Durability"].maxAmount / 100) * UnityEngine.Random.Range(5, 35);
                                part.Resources["Durability"].amount -= experimentDamage;
                                displayExp = EngineMutli.ToString("0.0000");
                            }
                        }
                    
                        //DeltaTime
                        reduce *= TimeWarp.fixedDeltaTime;

                        displayDamage = reduce.ToString("0.0000");
                        displayDamage += " B: " + basicWear.ToString("0.0000");
                        
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
                        if (reduce > 0.0)
                        {
                            if (_isInit && vessel.missionTime > 0.01)
                            {
                                _lastUpdateTime = vessel.missionTime;
                                lastReduceRange = reduce * (1 / TimeWarp.fixedDeltaTime);
                            }
                        }
                    }
                    else if (part.Resources["Durability"].amount < 0)
                    {
                        part.Resources["Durability"].amount = 0;
                    }
                    
                    part.Resources["Durability"].amount -= reduce;
                }
            }
            catch (Exception ex)
            {
                //KspDeMod.Durability.Debug.LogError("KspDeMod: [reduceDurability]: Message: " + ex.Message);
                //KspDeMod.Durability.Debug.LogError("KspDeMod: [reduceDurability]: Source: " + ex.Source);
               // KspDeMod.Durability.Debug.LogError("KspDeMod: [reduceDurability]: StackTrace: " + ex.StackTrace);
            }
        }
        
        
        /// <summary>
        /// Get the current Durability as percent
        /// </summary>
        /// <returns></returns>
        private double GetDurabilityPercent()
        {
            double percent = 1.0d;
            if (part.Resources.Contains("Durability"))
            {
                percent = part.Resources["Durability"].amount / part.Resources["Durability"].maxAmount;
            }
            else
            {
                KspDeMod.Durability.Debug.Log("KspDeMod [GetDurabilityPercent]: Resource Durability not Found");
            }
            return percent * 100;
        }

        #endregion //Private methods
    }
}