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
        /*
         * THIS IS AN VERY EARLY TEST VERSION
         * 
         * 
         * 
         * 
         * All oft the CODe would be changed
         * 
         * 
         * 
         * 
         * 
         * 
         * /
        /*List of Pumpobject in the ship*/
        int pumpCount;

        /*lock  variables*/
        bool locked = false;
        

        float lastUpdate = 0.0f;

        /*gamesetting  variables*/
        [KSPField(isPersistant = true, guiActive = true,guiName="Cool to ",guiUnits = " °C")]
        public float coolTemp = 0.0f;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Status ")]
        public string state = "Ready";
        float coolAmount = 10.0f;

        /*Called on Creating*/
        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor)
            {
                
                locked = true;
            }
            else
            {
                locked = false;
                coolTemp = CoolingPumpExtension.coolTemp;
            }
            print("Cooling loaded " + locked.ToString());

        }
        /*Called every fixed Frame*/
        //FixedUpdate not working
        public override void  OnUpdate()
        {
 	        if (locked == false && state == "Running")
            {
               
               
                
                pumpCount = this.part.getPumps(this.vessel.parts, this.ClassID);
                if (Time.time - lastUpdate > 1)
                {
                    if (this.part.RequestResource("ElectricCharge", 0.5 * (Time.time - lastUpdate)) > 0.2 && this.part.RequestResource("CoolWater", 15.0  * (Time.time - lastUpdate)) > 14.9)
                    {
                        this.part.RequestResource("CoolWater", -14.99 * (Time.time - lastUpdate));
                        lastUpdate = Time.time;
                        CoolingPumpExtension.toAddAmount += coolAmount;
                        if (this.part.isPrimary(this.vessel.parts, this.ClassID))
                        {
                            this.part.coolDown(Time.time - lastUpdate);
                            foreach (var item in this.part.vessel.parts)
                            {
                                print("temp: " + item.temperature);
                            }
                        }
                    }
                    else
                    {
                        state = "No Energy";
                    }
                   
                }
            }

            
        }

        [KSPEvent(guiActive = true, guiName = "Activate")]
        public void ActivateEvent()
        {
            ScreenMessages.PostScreenMessage("Pump activated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            state = "Running";
            lastUpdate = Time.time;
            Events["ActivateEvent"].active = false;
            Events["DeactivateEvent"].active = true;
        }

        /*
         * This event is also active when controlling the vessel with the part. It starts disabled.
         */
        [KSPEvent(guiActive = true, guiName = "Deactivate", active = false)]
        public void DeactivateEvent()
        {
            ScreenMessages.PostScreenMessage("Pump deactivated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            state = "Ready";
            lastUpdate = Time.time;
            Events["ActivateEvent"].active = true;
            Events["DeactivateEvent"].active = false;
        }

    }

    public static class CoolingPumpExtension
    {
        public static float toAddAmount = 0.0f;

        public static float coolTemp = 10.0f;

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

        public static int getPumps(this Part thisPart,List <Part> partsList,int moduleClassID)
        {
            int count = 0;
            foreach (var item in partsList)
            {
                if (item.Modules.Contains(moduleClassID))
                {
                    count++;
                }

            }
            return count;
        }

        public static void coolDown(this Part thisPart, float mult)
        {
            float alldif = 0.0f;
            foreach (var item in thisPart.vessel.parts)
            {
                float diference = coolTemp - item.temperature;
                diference = Mathf.Sqrt(diference * diference);
                alldif += diference;
                
            }


            foreach (var item in thisPart.vessel.parts)
            {
                float diference = coolTemp - item.temperature;
                if (coolTemp - item.temperature > 0)
                {
                    diference = Mathf.Sqrt(diference * diference);
                    float pp = diference / alldif;
                    float add = (toAddAmount) * pp;
                    item.temperature = item.temperature + (add*mult);
                    /*Must Add Temp*/
                }
                else
                {
                    diference = Mathf.Sqrt(diference * diference);
                    float pp = diference / alldif;
                    float add = (toAddAmount) * pp;
                    item.temperature = item.temperature - (add*mult);
                    /*Must remove Temp*/
                }
                
            }

            toAddAmount = 0.0f;


        }
    }
}
