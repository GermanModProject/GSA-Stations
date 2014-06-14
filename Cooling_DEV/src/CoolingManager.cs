using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoolingSystem
{
    /*
     * This Class will Manage 
     * 1. Mutiple Parts
     * 2. The Generatet Cooling Energy
     */
    public static class CoolingManager
    {
        public static Dictionary<Vessel, float> aviableCooling = new Dictionary<Vessel, float>();
        public static Dictionary<Part, bool> Pumps = new Dictionary<Part, bool>();
    }



    public static class VesselExtension
    {
        public static float getCoolingEnergy(this Vessel thisVessel){
            float ret = 0.0f;
            CoolingManager.aviableCooling.TryGetValue(thisVessel, out ret);
            return ret;
        }

        public static void addCoolingEnergy(this Vessel thisVessel, float amount)
        {
            float ret = 0.0f;
            CoolingManager.aviableCooling.TryGetValue(thisVessel, out ret);
            ret += amount;
            CoolingManager.aviableCooling.Remove(thisVessel);
            CoolingManager.aviableCooling.Add(thisVessel, ret);
        }

        public static void addIfNotExists(this Vessel thisVessel)
        {
            if (!CoolingManager.aviableCooling.ContainsKey(thisVessel))
            {
                CoolingManager.aviableCooling.Add(thisVessel,0.0f);
            }
        }

        public static void getLostEnergy(this Vessel thisVessel)
        {

        }
    }
}
