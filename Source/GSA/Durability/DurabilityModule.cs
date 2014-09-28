using System;
using System.Collections.Generic;
using UnityEngine;

namespace GSA.Durability
{
    public class DurabilityModule : PartModule, IPartCostModifier
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
        /// Display Damage
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "[D]Damage")]
        public string displayDamage;

        /// <summary>
        /// Display Pressure
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "[D]Pressure")]
        public string displayPressure;

        /// <summary>
        /// Display Temperature
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "[D]Engine")]
        public string displayEngine;

        /// <summary>
        /// Display GeeForce
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "[D]G")]
        public string displayGeeForce;

        /// <summary>
        /// Display GeeForce
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "[D]Temp")]
        public string displayTempM;

        /// <summary>
        /// Display ReactionWheel
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "[D]RWheel")]
        public string displayReactionWheel;

        /// <summary>
        /// Display Experiment
        /// </summary>
        [KSPField(isPersistant = false, guiActive = true, guiName = "[D]Expe.")]
        public string displayExp;

        /// <summary>
        /// Display Sun
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiName = "[D]Rad")]
        public string displaySun;

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
        public double quality = 0.5f;

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
        public FloatCurve basicWear = new FloatCurve();

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
        /// Radiatio absorption
        /// </summary>
        [KSPField(isPersistant = false)]
        public float radiationAbsorption = 0;

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
        private StartState _state;
        private float _initCost;
        private float _currentWear;
        private ModuleCommand _command = null;
        private ModuleReactionWheel _reactionWheel = null;
        private ModuleEngines _engine = null;
        private CelestialBody _sun = null;
        private ModuleScienceExperiment _scienceExperiment = null;
        private double _finalReduce = 0;

        #endregion //Private Fields

        #region KSPEvent

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 4f)]
        public void RepairDamage()
        {
            if (Events == null || !canRepair || maxRepair == 0)
                return;
            if (part.Resources.Contains("Durability"))
            {
                double difference = part.Resources["Durability"].maxAmount - part.Resources["Durability"].amount;
                GSA.Durability.Debug.Log("KspDeMod [RepairDamage]: Start Repair (" + difference.ToString("0.0000") + ")");
                if (difference > 0)
                {
                    part.Resources["Durability"].amount = part.Resources["Durability"].maxAmount;
                    double differenceP = difference / part.Resources["Durability"].maxAmount;
                    GSA.Durability.Debug.Log("KspDeMod [RepairDamage]: Repair differenceP (" + differenceP.ToString("0.0000") + ")");
                    quality -= repairQualityReducer * differenceP;
                    if (maxRepair > 0)
                    {
                        maxRepair--;
                    }
                    if (quality < 0.01d)
                    {
                        quality = 0.01d;
                    }
                    if (part.Resources.Contains("Quality"))
                    {
                        part.Resources["Quality"].amount = quality * 100;
                    }
                    _currentWear = basicWear.Evaluate((float)quality);
                }
            }
            else
            {
                GSA.Durability.Debug.Log("KspDeMod [RepairDamage]: Resource Durability not Found");
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
                GSA.Durability.Debug.LogError("GSA Durability: [OnAwake]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [OnAwake]: StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            GSA.Durability.Debug.Log("GSA Durability: [OnStart][" + state.ToString() + "]: " + this.name);
            base.OnStart(state);
            try
            {
                _state = state;
                AvailablePart currentPartInfo = PartLoader.getPartInfoByName(part.name.Replace("(Clone)", ""));
                _initCost = currentPartInfo.cost;
                if (state == StartState.Editor)
                {
                    return;
                }
                if (part.Resources.Contains("Quality"))
                {
                    quality = (part.Resources["Quality"].amount / (part.Resources["Quality"].maxAmount / 100)) / 100;
                }

                if (basicWear.findCurveMinMaxInterations == 0)
                {
                    basicWear = new FloatCurve();
                    basicWear.Add(0.1f, 0.69f);
                    basicWear.Add(0.5f, 0.000181f);
                    basicWear.Add(1f, 0.00001f);
                }
                _currentWear = basicWear.Evaluate((float)quality);
                _lastUpdateTime = vessel.missionTime;
                _sun = Planetarium.fetch.Sun;
                //gameObject.AddComponent(typeof(LineRenderer));

                if (part.Modules.Contains("ModuleCommand"))
                {
                    _command = (ModuleCommand)part.Modules["ModuleCommand"];
                    GSA.Durability.Debug.Log("GSA Durability: [OnStart]: _command = " + _command.name);
                }

                if (part.Modules.Contains("ModuleReactionWheel"))
                {
                    _reactionWheel = (ModuleReactionWheel)part.Modules["ModuleReactionWheel"];
                    GSA.Durability.Debug.Log("GSA Durability: [OnStart]: _reactionWheel = " + _reactionWheel.name);
                }

                if (part.Modules.Contains("ModuleEngines"))
                {
                    _engine = (ModuleEngines)part.Modules["ModuleEngines"];
                    GSA.Durability.Debug.Log("GSA Durability: [OnStart]: _engine = " + _engine.name);
                }
                if (part.Modules.Contains("ModuleScienceExperiment"))
                {
                    _scienceExperiment = (ModuleScienceExperiment)part.Modules["ModuleScienceExperiment"];
                    GSA.Durability.Debug.Log("GSA Durability: [OnStart]: _scienceExperiment = " + _scienceExperiment.name);
                }

                checkStatus();
                setEventLabel();
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [OnStart]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [OnStart]: StackTrace: " + ex.StackTrace);
            }

            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: quality:" + quality.ToString());
            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: damageRate: " + _currentWear.ToString("0.000000"));
            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: vessel.missionTime: " + vessel.missionTime.ToString());
            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: lastReduceRange: " + lastReduceRange.ToString());
            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: lastUpdateTime: " + _lastUpdateTime.ToString());
            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: maxRepair: " + maxRepair.ToString());
            GSA.Durability.Debug.Log("GSA Durability: [OnStart]: canRepair: " + canRepair.ToString());
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (_isFirst)
            {
                _isFirst = false;
                GSA.Durability.Debug.Log("GSA Durability: [OnUpdate]: Start First Update");
                GSA.Durability.Debug.Log("GSA Durability: [OnUpdate]: vessel.missionTime: " + vessel.missionTime.ToString());
                //Recalculate Durability
                try
                {
                    if (vessel.missionTime > 0 && _lastUpdateTime > 0)
                    {
                        double reReduce = (vessel.missionTime - _lastUpdateTime) * lastReduceRange;
                        GSA.Durability.Debug.Log("GSA Durability: [OnUpdate]: recalculate Duability: " + reReduce.ToString("F0"));
                        if (part.Resources.Contains("Durability") && reReduce > 0)
                        {
                            part.Resources["Durability"].amount -= reReduce;
                        }
                    }
                    _isInit = true;
                }
                catch (Exception ex)
                {
                    GSA.Durability.Debug.LogError("GSA Durability: [OnUpdate] Recalculate: Message: " + ex.Message);
                    GSA.Durability.Debug.LogError("GSA Durability: [OnUpdate] Recalculate: Source: " + ex.Source);
                    GSA.Durability.Debug.LogError("GSA Durability: [OnUpdate] Recalculate: StackTrace: " + ex.StackTrace);
                }
            }

            if (_state != StartState.Editor)
            {
                checkStatus();
                setEventLabel();
            }
            _countUpdates++;
        }

        public void FixedUpdate()
        {
            if (_state != StartState.Editor)
            {
                reduceDurability();
            }
            else
            {
                if (part.Resources.Contains("Quality"))
                {
                    quality = (part.Resources["Quality"].amount / (part.Resources["Quality"].maxAmount / 100)) / 100;
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                GSA.Durability.Debug.Log(node);
                if (node.HasValue("lastUpdateTime"))
                {
                    double.TryParse(node.GetValue("lastUpdateTime"), out this._lastUpdateTime);
                }
                if (node.HasValue("lastReduceRange"))
                {
                    double.TryParse(node.GetValue("lastReduceRange"), out this.lastReduceRange);
                }

                GSA.Durability.Debug.Log("GSA Durability: [OnLoad]: vessel.missionTime: " + vessel.missionTime.ToString());
                GSA.Durability.Debug.Log("GSA Durability: [OnLoad]: lastReduceRange: " + lastReduceRange.ToString());
                GSA.Durability.Debug.Log("GSA Durability: [OnLoad]: lastUpdateTime: " + _lastUpdateTime.ToString());
            }
            catch { }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (part.Resources.Contains("Durability"))
            {
                float baseValue = part.crashTolerance / 100;
                if (collision.relativeVelocity.magnitude > baseValue * 25 && collision.relativeVelocity.magnitude < part.crashTolerance)
                {
                    double baseDamage = part.Resources["Durability"].maxAmount / 1000;
                    double reduce = 0;
                    GSA.Durability.Debug.Log("GSA Durability: [OnCollisionEnter]: baseDamage: " + baseDamage.ToString("0.0000"));
                    if (part.Modules.Contains("ModuleLandingGear"))
                    {
                        baseDamage /= 3;
                        GSA.Durability.Debug.Log("GSA Durability: [OnCollisionEnter]: ModuleLandingGear baseDamage: " + baseDamage.ToString("0.0000"));
                    }
                    if (part.Modules.Contains("ModuleDockingNode"))
                    {
                        baseDamage /= 2;
                        GSA.Durability.Debug.Log("GSA Durability: [ModuleDockingNode]: ModuleLandingGear baseDamage: " + baseDamage.ToString("0.0000"));
                    }

                    if (collision.relativeVelocity.magnitude > baseValue * 90)
                    {
                        reduce = (collision.relativeVelocity.magnitude / baseValue) * baseDamage;
                        GSA.Durability.Debug.Log("GSA Durability: [ModuleDockingNode]: reduce over 90: " + reduce.ToString("0.0000"));
                    }
                    else
                    {
                        reduce = ((collision.relativeVelocity.magnitude / 2) / baseValue) * baseDamage;
                        GSA.Durability.Debug.Log("GSA Durability: [ModuleDockingNode]: reduce under 90: " + reduce.ToString("0.0000"));
                    }
                    part.Resources["Durability"].amount -= reduce;
                }
            }
        }

        public float GetModuleCost()
        {
            return calculateCost();
        }

        #endregion //Public override methods

        #region Private methods

        private float calculateCost()
        {
            double newCost = 0;
            if (quality == 0.5)
            {
                newCost = (double)_initCost;
            }
            else if (quality > 0.5)
            {
                newCost = (double)_initCost * Math.Pow((double)_initCost, (quality - .5d));
            }
            else if (quality < 0.5)
            {
                newCost = (double)_initCost / Math.Pow((double)_initCost, (.5d - quality));
            }
            newCost -= (double)_initCost;
            //GSA.Durability.Debug.Log("GSA Durability: [calculateCost]: newCost: " + newCost.ToString());
            //GSA.Durability.Debug.Log("GSA Durability: [calculateCost]: _initCost: " + _initCost.ToString());
            return (float)newCost;
        }

        /// <summary>
        /// Check durability status
        /// </summary>
        private void checkStatus()
        {
            _displayDurability = GetDurabilityPercent();
            displayTemperature = part.temperature.ToString("0.00");
            try
            {
                if (part.Resources.Contains("Durability"))
                {
                    if (part.Resources["Durability"].amount <= 0 && canExplode)
                    {
                        part.explode();
                        GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: explode[canExplode] " + part.name);
                    }
                    else
                    {
                        if (part.Resources["Durability"].amount <= minDurability && !part.frozen)
                        {
                            if (_reactionWheel != null)
                            {
                                _reactionWheel.wheelState = ModuleReactionWheel.WheelState.Broken;
                            }

                            if (_engine != null)
                            {
                                GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: _engine is not Null:");
                                if (!_engine.engineShutdown)
                                {
                                    _engine.Events.Send(_engine.Events["Shutdown"].id);
                                    GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: engineShutdown = True");
                                }
                            }
                            GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: _engine:" + _engine.ToString());
                            GSA.Durability.Debug.Log(_engine);
                            foreach (PartModule dModules in part.Modules)
                            {
                                if (dModules.moduleName != "DurabilityModule")
                                {
                                    dModules.isEnabled = false;
                                    GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: isEnabled = False:  " + dModules.moduleName);
                                }
                            }
                            part.freeze();
                            GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: freeze Part " + part.name);
                        }
                        else if (part.Resources["Durability"].amount > minDurability && part.frozen)
                        {
                            part.unfreeze();
                            if (_reactionWheel != null)
                            {
                                _reactionWheel.wheelState = ModuleReactionWheel.WheelState.Active;
                            }
                            foreach (PartModule aModules in part.Modules)
                            {
                                if (aModules.moduleName != "DurabilityModule")
                                {
                                    aModules.isEnabled = true;
                                    GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: isEnabled = True:  " + aModules.moduleName);
                                }
                            }
                            GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: unfreeze Part " + part.name);
                        }
                        if (part.Resources["Durability"].amount <= 0)
                        {
                            if (vessel.staticPressure >= maxPressure)
                            {
                                part.explode();
                                GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: explode[Pressure] " + part.name);
                            }
                            if (_engine != null && quality == 0.01d)
                            {
                                part.explode();
                                GSA.Durability.Debug.Log("GSA Durability: [checkStatus]: explode[engine] " + part.name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [checkStatus]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [checkStatus]: Source :" + ex.Source);
                GSA.Durability.Debug.LogError("GSA Durability: [checkStatus]: StackTrace: " + ex.StackTrace);
            }
        }

        private void setEventLabel()
        {
            if (Events == null)
                return;
            try
            {
                if (_displayDurability < 99 && canRepair && quality > 0.01 && (maxRepair > 0 || maxRepair == -1))
                {
                    Events["RepairDamage"].guiName = "Repair";
                }
                else if ((!canRepair || maxRepair == 0) && quality > 0.01)
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
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [setEventLabel]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [setEventLabel]: Source: " + ex.Source);
                GSA.Durability.Debug.LogError("GSA Durability: [setEventLabel]: StackTrace: " + ex.StackTrace);
                GSA.Durability.Debug.LogError(ex.Data);
            }
        }

        /// <summary>
        /// Reduce durability
        /// </summary>
        private void reduceDurability()
        {
            _finalReduce = 0;
            try
            {
                if (part.Resources.Contains("Durability"))
                {
                    if (part.Resources["Durability"].amount > 0)
                    {
                        // Default
                        _finalReduce = _currentWear;
                        double additionalReduce = 0;

                        //Temperature
                        additionalReduce += getReduceTemperature();

                        //GeeForce
                        additionalReduce += getReduceGeeForce();

                        //Pressure
                        additionalReduce += getReducePressure();

                        //Engine
                        additionalReduce += getReduceEngine();

                        //ScienceExperiment ModuleScienceExperiment (Not Multiply, fixed Damage)
                        getReduceExperiment();

                        //Reaction Wheels
                        /*if (_reactionWheel != null)
                        {
                            displayReactionWheel = "YT" + _reactionWheel.YawTorque.ToString("0.000")
                                + "RT" + _reactionWheel.RollTorque.ToString("0.000")
                                + "PT" + _reactionWheel.PitchTorque.ToString("0.000");
                        }*/

                        //radiation
                        //additionalReduce += getReduceRadiation(reduce);

                        _finalReduce += additionalReduce;
                        displayDamage = _finalReduce.ToString("0.000000");

                        //DeltaTime
                        _finalReduce *= TimeWarp.fixedDeltaTime;
                        displayDamage += " B: " + _currentWear.ToString("0.000000");

                        displayTime = getFormatedExpirationDate();

                        if (_finalReduce > 0.0)
                        {
                            if (_isInit && vessel.missionTime > 0.01)
                            {
                                _lastUpdateTime = vessel.missionTime;
                                lastReduceRange = _finalReduce * (1 / TimeWarp.fixedDeltaTime);
                            }
                        }
                    }
                    else if (part.Resources["Durability"].amount < 0)
                    {
                        part.Resources["Durability"].amount = 0;
                    }

                    part.Resources["Durability"].amount -= _finalReduce;
                }
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [reduceDurability]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [reduceDurability]: StackTrace: " + ex.StackTrace);
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
                GSA.Durability.Debug.Log("KspDeMod [GetDurabilityPercent]: Resource Durability not Found");
            }
            return percent * 100;
        }

        /// <summary>
        /// Get the xxpiration date
        /// </summary>
        /// <returns>Seconds</returns>
        public double getExpirationDate()
        {
            try
            {
                if (part.Resources.Contains("Durability"))
                {
                    return part.Resources["Durability"].amount / (_finalReduce * (1 / TimeWarp.fixedDeltaTime));
                }
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getExpirationDate]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getExpirationDate]: StackTrace: " + ex.StackTrace);
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Get the expiration date Formated
        /// </summary>
        /// <returns>String</returns>
        public string getFormatedExpirationDate()
        {
            try
            {
                double secondsToZero = getExpirationDate();
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

                return "T+ " + ((years > 0) ? "y" + years.ToString("D2") + ", " : "") +
                    ((days > 0) ? "d" + days.ToString("D2") + ", " : "") +
                    hours.ToString("D2") + ":" +
                    minutes.ToString("D2") + ":" +
                    seconds.ToString("D2");
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getFormatedExpirationDate]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getFormatedExpirationDate]: StackTrace: " + ex.StackTrace);
                return "ERROR";
            }
        }

        #endregion //Private methods

        #region Reduce methods

        /// <summary>
        /// Temperatur durability reduction
        /// </summary>
        /// <returns>additional reduction</returns>
        private double getReduceTemperature()
        {
            double additionalReduce = 0;
            try
            {
                double TempMutli = 0;
                TempMutli = idealTemp.Evaluate(part.temperature);
                TempMutli = (TempMutli > 1) ? TempMutli : 1;
                //reduce *= TempMutli;
                additionalReduce += (_currentWear * TempMutli) - _currentWear;
                displayTempM = TempMutli.ToString("0.0000");
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceTemperature]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceTemperature]: StackTrace: " + ex.StackTrace);
            }
            return additionalReduce;
        }

        /// <summary>
        /// GForce durability reduction
        /// </summary>
        /// <returns>additional reduction</returns>
        private double getReduceGeeForce()
        {
            double additionalReduce = 0;
            try
            {
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
                //reduce *= GeeForceMutli;
                additionalReduce += (_currentWear * GeeForceMutli) - _currentWear;
                displayGeeForce = GeeForceMutli.ToString("0.0000");
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceGeeForce]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceGeeForce]: StackTrace: " + ex.StackTrace);
            }
            return additionalReduce;
        }

        /// <summary>
        /// Pressure durability reduction
        /// </summary>
        /// <param name="reduce">basic reduction</param>
        /// <returns>additional reduction</returns>
        private double getReducePressure()
        {
            double additionalReduce = 0;
            try
            {
                double pressureMulti = 0;
                float pressure = 1f;
                pressure = Convert.ToSingle(vessel.staticPressure);
                pressureMulti = idealPressure.Evaluate(pressure);
                pressureMulti = (pressureMulti > 1) ? pressureMulti : 1;
                //reduce *= pressureMulti;
                additionalReduce += (_currentWear * pressureMulti) - _currentWear;
                displayPressure = pressureMulti.ToString("0.0000");
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getReducePressure]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getReducePressure]: StackTrace: " + ex.StackTrace);
            }
            return additionalReduce;
        }

        /// <summary>
        /// Engine durability reduction
        /// </summary>
        /// <param name="reduce">basic reduction</param>
        /// <returns>additional reduction</returns>
        private double getReduceEngine()
        {
            double additionalReduce = 0;
            try
            {
                double EngineMutli = 1;
                if (_engine != null)
                {
                    if (!_engine.flameout && !_engine.engineShutdown)
                    {
                        EngineMutli = (engineWear * engineWear) * (_engine.requestedThrust / _engine.maxThrust + 1);
                        EngineMutli = (EngineMutli > 1) ? EngineMutli : 1;
                        //reduce *= EngineMutli;
                        additionalReduce += (_currentWear * EngineMutli) - _currentWear;
                    }
                }
                displayEngine = EngineMutli.ToString("0.0000");
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceEngine]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceEngine]: StackTrace: " + ex.StackTrace);
            }
            return additionalReduce;
        }

        /// <summary>
        /// Experiment durability reduction
        /// </summary>
        private void getReduceExperiment()
        {
            try
            {
                //ScienceExperiment ModuleScienceExperiment (Not Multiply, fixed Damage)
                if (_scienceExperiment != null)
                {
                    if (_scienceExperiment.Deployed && !_expDeployed)
                    {
                        _expDeployed = true;
                        GSA.Durability.Debug.Log("GSA Durability: [getReduceExperiment]");

                        double experimentDamage = (part.Resources["Durability"].maxAmount / 100) * UnityEngine.Random.Range(5, 35);
                        part.Resources["Durability"].amount -= experimentDamage;
                        displayExp = experimentDamage.ToString("0.0000");
                    }
                }
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceExperiment]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceExperiment]: StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Engine durability reduction
        /// </summary>
        /// <param name="reduce">basic reduction</param>
        /// <returns>additional reduction</returns>
        private double getReduceRadiation()
        {
            double additionalReduce = 0;
            try
            {
                double radiationMutli = 1;
                Transform target = _sun.transform;
                RaycastHit hit;
                displaySun = "";

                //LineRenderer line = (LineRenderer)gameObject.GetComponent(typeof(LineRenderer));
                //line.SetVertexCount(2);
                //line.SetWidth(0.1f, 0.1f);

                float sunDis = Convert.ToSingle(Vector3d.Distance(part.gameObject.transform.position, _sun.transform.position));
                //if (Physics.Raycast(part.gameObject.transform.position, target.position, out hit, sunDis))
                if (Physics.Linecast(part.gameObject.transform.position, target.position, out hit))
                {
                    displaySun = "c: " + (hit.collider != null ? hit.collider.name : "N/A") + "; dis: " + hit.distance.ToString("0.0000");

                    //line.SetColors(Color.red, Color.red);
                    //line.SetPosition(0, part.gameObject.transform.position);
                    //line.SetPosition(1, hit.point);
                }
                else
                {
                    try
                    {
                        double atmoAbsob = (vessel.staticPressure > 1.05 ? 0 : 1.05 - vessel.staticPressure);
                        radiationMutli = sunDis * 1.4995217994990961392430031278165e-9;
                        radiationMutli *= atmoAbsob;
                        //displaySun += ";" + radiationMutli.ToString("0.0000"); 
                        radiationMutli -= radiationAbsorption;
                        displaySun += radiationMutli.ToString("0.0000");
                        //reduce *= (radiationMutli > 0 ? radiationMutli : 1);
                        additionalReduce += (_currentWear * radiationMutli) - _currentWear;

                        //line.SetColors(Color.green, Color.green);
                        //line.SetPosition(0, part.gameObject.transform.position);
                        //line.SetPosition(1, _sun.transform.position);
                        //line.SetWidth(0.01f, 0.01f);
                    }
                    catch (Exception ex)
                    {
                        //displaySun = "Error drow line";
                        GSA.Durability.Debug.LogError("GSA Durability: [getReduceRadiation]: Message: " + ex.Message);
                    }
                }
                displaySun = radiationMutli.ToString("0.0000");
            }
            catch (Exception ex)
            {
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceRadiation]: Message: " + ex.Message);
                GSA.Durability.Debug.LogError("GSA Durability: [getReduceRadiation]: StackTrace: " + ex.StackTrace);
            }
            return additionalReduce;
        }

        #endregion //Reduce methods
    }
}