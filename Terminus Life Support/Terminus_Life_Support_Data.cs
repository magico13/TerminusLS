using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminus_Life_Support
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    class Terminus_Life_Support_Data : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            ConfigNode DataNode = new ConfigNode("TerminusData");
            ConfigNode TLS_Kerbals = new ConfigNode("TLS_Kerbals");
            foreach (KerbalInfo ki in Terminus_Life_Support.TrackedKerbals)
            {
                TLS_Kerbals.AddNode(ki.AsConfigNode());
            }
            DataNode.AddNode(TLS_Kerbals);
            node.AddNode(DataNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            //get settings node and kerbal node
            try
            {
                ConfigNode DataNode = node.GetNode("TerminusData");
                if (DataNode != null)
                {
                    ConfigNode TLS_Kerbals = DataNode.GetNode("TLS_Kerbals");
                    if (TLS_Kerbals != null)
                    {
                        Terminus_Life_Support.TrackedKerbals.Clear();
                        foreach (ConfigNode kiNode in TLS_Kerbals.GetNodes("KerbalInfo"))
                        {
                            KerbalInfo ki = new KerbalInfo();
                            ki.FromConfigNode(kiNode);
                            if (ki.Name != "")
                                Terminus_Life_Support.TrackedKerbals.Add(ki);
                        }
                        Terminus_Life_Support.UpdateKerbalList(true);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
