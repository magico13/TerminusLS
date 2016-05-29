using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;


namespace Terminus_Life_Support
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Terminus_Life_Support : MonoBehaviour
    {
        public static double consumptionRate = 1.0 / (6*3600); //in seconds. 1 unit per 6 hours
        public static List<KerbalInfo> TrackedKerbals = new List<KerbalInfo>();
        public static List<KerbalInfo> LoadedKerbals = new List<KerbalInfo>();

        public static List<Vessel> LoadedVessels = new List<Vessel>();

        public void Start()
        {
            GameEvents.onVesselLoaded.Add(VesselLoadedOrUnloaded);
            GameEvents.onVesselGoOnRails.Add(VesselLoadedOrUnloaded);

           /* GameEvents.onKerbalStatusChange.Add((ProtoCrewMember pcm, ProtoCrewMember.RosterStatus oldStatus, ProtoCrewMember.RosterStatus newStatus) =>
            {
                if (newStatus == ProtoCrewMember.RosterStatus.Available)
                {
                    KerbalInfo ki = GetInfoForPCM(pcm);
                    if (ki != null)
                    {
                        ki.LastUpdated = -1;
                        ki.LastSated = -1;
                    }
                    Debug.Log("Terminus LS: Resetting timers for " + pcm.name);
                }
            }
            );*/

            //run through and make sure all the available kerbals have been reset
            UpdateKerbalList();
        }

        public void OnDestroy()
        {
            GameEvents.onVesselLoaded.Remove(VesselLoadedOrUnloaded);
            GameEvents.onVesselGoOnRails.Remove(VesselLoadedOrUnloaded);
        }


        public void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad < 1)
                return;

            double currentTime = Planetarium.GetUniversalTime();
            //loop through all the loaded kerbals and take resources as needed
            /*for (int i = 0; i < LoadedKerbals.Count; i++)
            {
                KerbalInfo ki = LoadedKerbals[i];
                //(KerbalInfo ki in LoadedKerbals)
                if (ki.CrewedVessel != null && ki.CrewedVessel.loaded)
                {
                    //vessel is loaded (and crewmember is valid)
                    //consume resources
                    double delta = currentTime - ki.LastUpdated;
                    double requiredLS = delta * consumptionRate;
                    //if we do it per kerbal then all later kerbals wont have LS if we're low (remember the issues with intake air?)
                        //we should do it per vessel instead, which requires a bit more work
                    
                }
                else
                {
                    LoadedKerbals.RemoveAt(i);
                    i--;
                }
            }*/
            PartResourceDefinition LSRscDef = PartResourceLibrary.Instance.GetDefinition("LifeSupport");

            for (int i = 0; i < LoadedVessels.Count; i++)
            {
                Vessel v = LoadedVessels[i];
                if (v == null || !v.loaded)
                {
                    Debug.Log("Terminus LS: Vessel not loaded: " + v.vesselName);
                    UpdateKerbalList();
                    break;
                }

                int crewCount = v.GetCrewCount();
                //double totalReqLS = 0;

                Vessel.ActiveResource LSRSC = v.GetActiveResources().FirstOrDefault(rs => rs.info.name == "LifeSupport");
                double LSRemaining = (LSRSC != null) ? LSRSC.amount : 0;
                //Debug.Log("Terminus LS: " + LSRemaining + " remaining LS");
                foreach (ProtoCrewMember pcm in new List<ProtoCrewMember>(v.GetVesselCrew()))
                {
                    KerbalInfo ki = GetInfoForPCM(pcm, true);
                    if (ki == null) //not loaded but should be
                    {
                        ki = GetInfoForPCM(pcm, false);
                        if (ki == null) //not tracked but should be
                        {
                            ki = new KerbalInfo(pcm);
                            ki.LastUpdated = currentTime;
                            ki.LastSated = currentTime;
                            TrackedKerbals.Add(ki);
                        }
                        LoadedKerbals.Add(ki);
                    }

                    if (v.situation == Vessel.Situations.PRELAUNCH) //don't run for prelaunched craft
                    {
                        ki.LastUpdated = currentTime;
                        ki.LastSated = currentTime;
                        continue;
                    }

                    if (ki.LastUpdated > currentTime)
                    {
                        ki.LastUpdated = currentTime;
                        ki.LastSated = currentTime;
                    }

                    if (ki.LastUpdated > 0)
                    {
                        double deltaTime = currentTime - ki.LastSated;
                        double desiredLS = consumptionRate * deltaTime;
                        List<PartResource> resourceList = new List<PartResource>();
                        double LSGotten = 0;
                        if (pcm.KerbalRef != null)
                        {
                            //pcm.KerbalRef.InPart.GetConnectedResources(LSRsc.id, LSRsc.resourceFlowMode, resourceList);
                            LSGotten = pcm.KerbalRef.InPart.RequestResource(LSRscDef.id, Math.Min(desiredLS, LSRemaining / crewCount), LSRscDef.resourceFlowMode);
                        }
                        // = pcm.KerbalRef != null ?  //.RequestResource("LifeSupport", Math.Min(desiredLS, LSRemaining / crewCount)) : 0;
                        LSRemaining -= LSGotten;
                        ki.LastSated += deltaTime * (LSGotten / desiredLS); //tries to take all the resources it has missed out on

                        ki.LastUpdated = currentTime;

                        if (currentTime - ki.LastSated > 3600 * 6)
                        {
                            //greater than 6 hours since last fed, kill crewmember
                            KillCrewmember(pcm, v);
                        }
                    }
                    else
                    {
                        ki.LastUpdated = currentTime;
                    }
                }
            }
        }

        public void KillCrewmember(ProtoCrewMember pcm, Vessel vessel)
        {

            //This code adapted from TAC Life Support, written by taraniselsu under the (CC BY-NC-SA 3.0) license. Source available here: https://github.com/taraniselsu/TacLifeSupport/blob/master/Source/LifeSupportController.cs
            TimeWarp.SetRate(0, true);
            if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
            {
                CameraManager.Instance.SetCameraFlight();
            }

            Debug.Log("Terminus LS: " + pcm.name + " has died due to lack of life support at UT:"+Planetarium.GetUniversalTime()+"!");
            ScreenMessages.PostScreenMessage(pcm.name + " has died due to lack of life support!", 4.0f, ScreenMessageStyle.UPPER_CENTER);

            if (!vessel.isEVA)
            {
                Part part = vessel.Parts.Find(p => p.protoModuleCrew.Contains(pcm));
                if (part != null)
                {
                    part.RemoveCrewmember(pcm);
                    pcm.Die();

                    if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn)
                    {
                        pcm.StartRespawnPeriod();
                    }
                }
            }
            else
            {
                vessel.rootPart.Die();

                if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn)
                {
                    pcm.StartRespawnPeriod();
                }
            }

            //remove from the tracked and loaded lists
            KerbalInfo ki = GetInfoForPCM(pcm, false);
            TrackedKerbals.Remove(ki);
            LoadedKerbals.Remove(ki);
        }


        public static void UpdateKerbalList(bool ResetAvailable = false)
        {
            LoadedKerbals.Clear();
            LoadedVessels.Clear();
            //find all loaded vessels, find all kerbals onboard
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v != null && v.loaded)
                {
                    foreach (ProtoCrewMember pcm in v.GetVesselCrew())
                    {
                        if (TrackedKerbals.Exists(k => k.Name == pcm.name))
                            LoadedKerbals.Add(TrackedKerbals.Find(k => k.Name == pcm.name));
                        else
                        {
                            TrackedKerbals.Add(new KerbalInfo(pcm));
                            LoadedKerbals.Add(TrackedKerbals.Find(k => k.Name == pcm.name));
                        }
                    }
                    LoadedVessels.Add(v);
                }
            }

            Debug.Log("Terminus LS: " + LoadedVessels.Count + " vessels with " + LoadedKerbals.Count + " kerbals loaded out of " + TrackedKerbals.Count + " tracked.");
            if (ResetAvailable)
            {
                foreach (ProtoCrewMember pcm in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                    {
                        KerbalInfo ki = GetInfoForPCM(pcm);
                        if (ki != null)
                        {
                            ki.LastSated = -1;
                            ki.LastUpdated = -1;
                        }
                    }
                }
            }
        }


        public void VesselLoadedOrUnloaded(Vessel v)
        {
            UpdateKerbalList();
        }

        public static KerbalInfo GetInfoForPCM(ProtoCrewMember crew, bool loadedOnly = false)
        {
            if (loadedOnly)
            {
                return LoadedKerbals.FirstOrDefault(ki => ki.Name == crew.name);
            }
            else
            {
                return TrackedKerbals.FirstOrDefault(ki => ki.Name == crew.name);
            }
        }

        
    }

    //during fixed update go through all the loaded vessels, then each crewmember, and consume resources for each crewmember based on when they were last updated
    //if not enough LS, "kill" them
    //If they eva, give them 6 hours worth. On return, put it back in the vessel if there's room (hopefully fuel flow rules handle overflow)

    //Like TAC, we should define "VesselInfo" and "CrewInfo" classes that hold our extra data and are linked to a vessel or crewmember (like last update values, for instance)
}
