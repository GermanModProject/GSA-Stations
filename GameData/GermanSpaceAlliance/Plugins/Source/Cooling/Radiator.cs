using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace CoolingSystem
{
    class Radiator : PartModule
    {
        float A;
        float EmissionGrade = 0.9342f;

        float lastUpdate = Time.time;

        public override void OnStart(StartState state)
        {
            
            base.OnStart(state);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            CoolingManager.addCoolingEnergy(this.vessel,A * 0.0000000567f * CtoK(this.part.temperature) * EmissionGrade * (Time.time - lastUpdate / 60f));
            lastUpdate = Time.time;
        }


        public float CtoK(float C)
        {
            float K = C + 273.15f;

            return K;
        }

    }

    
}
