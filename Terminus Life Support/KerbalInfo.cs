using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminus_Life_Support
{
    public class KerbalInfo
    {
        public double LastUpdated = -1;
        public double LastSated = -1;

        public string Name = "";

        private ProtoCrewMember _CrewMember = null;
        public ProtoCrewMember CrewMember
        {
            get
            {
                if (_CrewMember == null)
                {
                    if (HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(pcm => pcm.name == Name) != null)
                        _CrewMember = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(pcm => pcm.name == Name);
                    else if (HighLogic.CurrentGame.CrewRoster.Tourist.FirstOrDefault(pcm => pcm.name == Name) != null)
                        _CrewMember = HighLogic.CurrentGame.CrewRoster.Tourist.FirstOrDefault(pcm => pcm.name == Name);
                    else if (HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(pcm => pcm.name == Name) != null)
                        _CrewMember = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(pcm => pcm.name == Name);
                }

                if (_CrewMember == null)
                    UnityEngine.Debug.Log("Terminus LS: Cannot find crewmember " + Name);

                return _CrewMember;
            }
        }

        private Vessel _CrewedVessel = null;
        public Vessel CrewedVessel
        {
            get
            {
                
                if (CrewMember != null && (_CrewedVessel == null || !_CrewedVessel.GetVesselCrew().Contains(CrewMember)))
                    _CrewedVessel = FlightGlobals.Vessels.FirstOrDefault(v => v.GetVesselCrew().Contains(CrewMember));

                if (_CrewedVessel == null)
                    UnityEngine.Debug.Log("Terminus LS: Cannot find vessel for crewmember " + Name);

                return _CrewedVessel;
            }
        }

        public KerbalInfo() { }

        public KerbalInfo(ProtoCrewMember pcm)
        {
            Name = pcm.name;
        }


        public ConfigNode AsConfigNode()
        {
            ConfigNode retNode = new ConfigNode("KerbalInfo");
            retNode.AddValue("Name", Name);
            retNode.AddValue("LastUpdated", LastUpdated);
            retNode.AddValue("LastSated", LastSated);

            return retNode;
        }

        public void FromConfigNode(ConfigNode node)
        {
            if (node.HasValue("Name"))
                Name = node.GetValue("Name");

            if (node.HasValue("LastUpdated"))
                double.TryParse(node.GetValue("LastUpdated"), out LastUpdated);

            if (node.HasValue("LastSated"))
                double.TryParse(node.GetValue("LastSated"), out LastSated);
        }
    }
}
