using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.IO;
using UnityEngine;

namespace KspDeMod.Durability
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class Durability : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("KspDeMod");
        }
    }

    /*[KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class Durability : MonoBehaviour
    {
        void Start()
        {
            //foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DURABILITY_MODULE"))
            //{

            //}
        }
    }*/
}
