using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace Terminus_Life_Support
{
    class ModuleTerminusLS : PartModule
    {
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            Debug.Log("Terminus OnStart");
        }

     /*   public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            //get the total number of kerbals and request life support
            int kerbals = part.protoModuleCrew.Count;

            double requiredLS = (TimeWarp.deltaTime * kerbals) / KSPUtil.KerbinDay; //1 unit per Kerbin day (4 units per Earth day)
        }*/
    }
}
