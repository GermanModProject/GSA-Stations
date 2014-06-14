using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.IO;
using UnityEngine;

namespace KspDeMod.Durability
{
    public static class PartExtensions
    {
        /// <summary>
        /// Get the current Durability as percent (0.0 - 1.0)
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static double GetDurabilityPercent(this Part part)
        {
            double percent = 1;
            if (part.Resources.Contains("Durability"))
            {
                percent = ((part.Resources["Durability"].maxAmount / 100d) / part.Resources["Durability"].amount) / 100;
            }
            return percent;
        }
    }
}
