using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoolingSystem
{
    /* This Class will Manage 
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

        public static float getLostEnergy(this Vessel thisVessel)
        {
            float ret = 0.0f;
            /*Berechnung des Verlustes mit Hilfe der Pumpen und Schiffsteile
             * 0.97 = kerbilsche Verschwendungskonstante
             *  aviableCooling * Parts * 0.97 / (Pumpen  + Amoniakamount*0.97) / aviableCooling * 100
             */
            CoolingManager.aviableCooling.TryGetValue(thisVessel,out ret);
            ret = ret * thisVessel.parts.Count * 0.97f / (thisVessel.getActivePumpCount() + (thisVessel.getAmoniakAmount() * Mathf.Pow(0.97f,3)))/ ret * 100.0f;
            return ret;
        }

        public static int getActivePumpCount(this Vessel thisVessel)
        {
            int ret = 0;
            foreach (var item in CoolingManager.Pumps)
            {
                if (item.Value)
                {
                    ret++;
                }
            }

            return ret;
        }

        public static float getAmoniakAmount(this Vessel thisVessel)
        {
            float ret = 0;
            foreach (var item in thisVessel.parts)
            {
                ret += (float)item.Resources["Amoniak"].amount;
                
            }

            return ret;
        }
    }
}
