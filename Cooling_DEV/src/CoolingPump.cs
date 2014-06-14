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

        bool Running = false;


        //On off Buttons
        [KSPEvent(guiActive = true, guiName = "Activate")]
         public void ActivateEvent()
         {
             ScreenMessages.PostScreenMessage("Pump activated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
             RunningState = "Running";
             Events["ActivateEvent"].active = false;
             Events["DeactivateEvent"].active = true;
         }
         [KSPEvent(guiActive = true, guiName = "Deactivate", active = false)]
         public void DeactivateEvent()
         {
             ScreenMessages.PostScreenMessage("Pump deactivated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
             RunningState = "Ready";
             Events["ActivateEvent"].active = true;
             Events["DeactivateEvent"].active = false;
         }

    }
}