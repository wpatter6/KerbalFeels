using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;

namespace KerbalFeels
{
    //Handles the events we're attaching to
    class KFEvents
    {
        private bool _initialized = false;

        public KFEvents()
        {
        }
        public void InitializeEvents()
        {
            KFUtil.Log("InitializeEvents");
            if (_initialized) return;
            GameEvents.onGUIAstronautComplexSpawn.Add(new EventVoid.OnEvent(OnGUIAstronautComplexSpawn));
            GameEvents.onKerbalStatusChange.Add(new EventData<ProtoCrewMember, ProtoCrewMember.RosterStatus, ProtoCrewMember.RosterStatus>.OnEvent(OnKerbalStatusChange));
            GameEvents.onVesselRecoveryProcessing.Add(new EventData<ProtoVessel, MissionRecoveryDialog, float>.OnEvent(OnVesselRecoveryProcessing));
            GameEvents.onCrewBoardVessel.Add(new EventData<GameEvents.FromToAction<Part, Part>>.OnEvent(OnCrewBoardVessel));
            GameEvents.onPartUndock.Add(new EventData<Part>.OnEvent(OnPartUndock));
            GameEvents.onVesselGoOffRails.Add(new EventData<Vessel>.OnEvent(OnVesselGoOffRails));
            GameEvents.onCrewOnEva.Add(new EventData<GameEvents.FromToAction<Part, Part>>.OnEvent(OnCrewOnEva));
            GameEvents.onGameStateSave.Add(new EventData<ConfigNode>.OnEvent(OnGameStateSave));
            GameEvents.onGameStateLoad.Add(new EventData<ConfigNode>.OnEvent(OnGameStateLoad));
            GameEvents.OnProgressComplete.Add(new EventData<ProgressNode>.OnEvent(OnProgressComplete));
            GameEvents.VesselSituation.onLand.Add(new EventData<Vessel, CelestialBody>.OnEvent(VesselOnLand));
            GameEvents.VesselSituation.onOrbit.Add(new EventData<Vessel, CelestialBody>.OnEvent(VesselOnOrbit));
            GameEvents.VesselSituation.onEscape.Add(new EventData<Vessel, CelestialBody>.OnEvent(VesselOnEscape));
            GameEvents.VesselSituation.onFlyBy.Add(new EventData<Vessel, CelestialBody>.OnEvent(VesselOnFlyBy));
            _initialized = true;
        }


        #region event handlers
        #region Game state
        private void OnGameStateLoad(ConfigNode data)
        {
            KFUtil.Log("OnGameStateLoad");

            if (data.HasNode("FEELS"))
            {
                var feelsNode = data.GetNode("FEELS");

                if (feelsNode.HasNode("CREW"))
                {
                    var crewNode = feelsNode.GetNode("CREW");
                    KFConfig.SetCurrentGameConfigNode("CREW", crewNode);
                    //crewNode.Save(_crewDbSaveFileNameAndPath);
                }
                if (feelsNode.HasNode("FLIGHTS"))
                {
                    var flightsNode = feelsNode.GetNode("FLIGHTS");
                    KFConfig.SetCurrentGameConfigNode("FLIGHTS", flightsNode);

                    //flightsNode.Save(_flightsDbSaveFileNameAndPath);
                }
            }
        }

        private void OnGameStateSave(ConfigNode data)
        {
            KFUtil.Log("OnGameStateSave");

            ConfigNode feelNode;
            if (data.HasNode("FEELS"))
                data.RemoveNode("FEELS");

            feelNode = data.AddNode("FEELS");

            var crewNode = KFConfig.GetCurrentGameConfigNode("CREW");//_crewDbSaveFileName, this.GetType());
            crewNode.name = "CREW";
            var flightNode = KFConfig.GetCurrentGameConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            flightNode.name = "FLIGHTS";

            feelNode.AddNode(crewNode);
            feelNode.AddNode(flightNode);
        }
        #endregion

        #region vessel situations
        private void VesselOnLand(Vessel data0, CelestialBody data1)
        {
            KFUtil.Log("VesselOnLand");
            foreach (ProtoCrewMember member in data0.GetVesselCrew())
            {
                KFCalc.AddSanity(member, KFConfig.VesselLandSanityBonus);
            }
        }

        private void VesselOnOrbit(Vessel data0, CelestialBody data1)
        {
            KFUtil.Log("VesselOnOrbit");
            //foreach (ProtoCrewMember member in data0.GetVesselCrew())
            //{
            //    KFCalc.AddSanity(member, KFConfig.VesselOrbitSanityBonus);
            //}
        }

        private void VesselOnEscape(Vessel data0, CelestialBody data1)
        {
            KFUtil.Log("VesselOnEscape");
            foreach (ProtoCrewMember member in data0.GetVesselCrew())
            {
                if(data1.name == "Kerbin")
                    KFCalc.AddSanity(member, KFConfig.VesselKerbinEscapeSanityLoss);
            }
        }

        private void VesselOnFlyBy(Vessel data0, CelestialBody data1)
        {
            KFUtil.Log("VesselOnFlyBy");
            foreach (ProtoCrewMember member in data0.GetVesselCrew())
            {
                if (data1.name != "Sun")
                    KFCalc.AddSanity(member, KFConfig.VesselFlyByBonus);
            }
        }
        #endregion

        private void OnVesselGoOffRails(Vessel data)
        {
            KFUtil.Log("OnVesselGoOffRails");
            var flightNode = KFConfig.GetCurrentGameConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());

            foreach (ProtoCrewMember member in data.GetVesselCrew())
            {
                KFCalc.CheckDeathEffects(member);
            }

            if(flightNode.HasNode(data.id.ToString()))
            {
                KFCalc.CalculateVesselChangedCrewInfo(data);
            }

            KFCalc.DetermineVesselCrewInfo(data);
        }

        private void OnProgressComplete(ProgressNode data)
        {
            KFUtil.Log("OnProgressComplete");
            if(FlightGlobals.ActiveVessel != null)
            {
                foreach (ProtoCrewMember member in FlightGlobals.ActiveVessel.GetVesselCrew())
                {
                    KFCalc.AddSanity(member, KFConfig.ProgressNodeBoost);
                }
            }
        }

        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewOnEva");
            if (data.from != null && data.from.vessel != null)
                KFCalc.CalculateVesselChangedCrewInfo(data.from.vessel);
        }

        private void OnPartUndock(Part data)
        {
            KFUtil.Log("OnPartUndock");
            KFCalc.CalculateVesselCrewStats(data.vessel);
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewBoardVessel");
            KFCalc.CalculateVesselChangedCrewInfo(data.to.vessel);
            KFCalc.DetermineVesselCrewInfo(data.to.vessel);
        }

        private void OnVesselRecoveryProcessing(ProtoVessel data0, MissionRecoveryDialog data1, float data2)
        {
            KFUtil.Log("OnVesselRecoveryProcessing");
            var c = KFCalc.CalculateVesselCrewStats(data0, true);
            var strs = new List<string>();

            c.Sort((x, y) => x.NewFeel.CrewMember.CompareTo(y.NewFeel.CrewMember));


            foreach (FeelsChange change in c)
            {
                strs.Add(KFUtil.GetFeelsChangeText(change));              
            }

            if (c.Count > 0)
            {
                new KFGUI().ShowGuiDialog(strs.ToArray());
            }
        }

        private void OnKerbalStatusChange(ProtoCrewMember data0, ProtoCrewMember.RosterStatus data1, ProtoCrewMember.RosterStatus data2)
        {
            KFUtil.Log("OnKerbalStatusChange");
            KFUtil.Log(data1.ToString() + " > " + data2.ToString());
            if (data2 == ProtoCrewMember.RosterStatus.Dead || data2 == ProtoCrewMember.RosterStatus.Missing)
            {
                KFDeath.DoDeath(data0);
            }
        }

        private void OnGUIAstronautComplexSpawn()
        {//todo some kind of gui?
            KFUtil.Log("OnGUIAstronautComplexSpawn");
            //KFGUI.ShowFullCrewDialog();
        }
        #endregion
    }
}
