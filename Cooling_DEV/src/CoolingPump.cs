using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace CoolingSystem
{
    public class CoolingPump : PartModule
    {
        //Variables
        [KSPField(isPersistant = true, guiActive = true, guiName = "Status: ")]
        public string RunningState = "";

        bool Running = true;


        //On off Buttons
        [KSPEvent(guiActive = true, guiName = "Activate")]
        public void ActivateEvent()
        {
            ScreenMessages.PostScreenMessage("Pump activated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            RunningState = "Running";
            Running = true;
            CoolingManager.Pumps.Remove(this.part);
            CoolingManager.Pumps.Add(this.part, Running);

            Events["ActivateEvent"].active = false;
            Events["DeactivateEvent"].active = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate", active = false)]
        public void DeactivateEvent()
        {
            ScreenMessages.PostScreenMessage("Pump deactivated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            RunningState = "Ready";
            Running = false;
            CoolingManager.Pumps.Remove(this.part);
            CoolingManager.Pumps.Add(this.part, Running);
            Events["ActivateEvent"].active = true;
            Events["DeactivateEvent"].active = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            CoolingManager.Pumps.Add(this.part, Running);
        }
    }



    public static class CoolingPumpExtension
    {
        public static bool isPrimary(this Part thisPart, List<Part> partsList, int moduleClassID)
        {
            foreach (var item in partsList)
            {
                if (item.Modules.Contains(moduleClassID))
                {
                    if (thisPart == item)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
    }


}