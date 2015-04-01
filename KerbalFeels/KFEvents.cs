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
            
            _initialized = true;
        }

        #region event handlers
        private void OnGameStateLoad(ConfigNode data)
        {
            KFUtil.Log("OnGameStateLoad");

            if (data.HasNode("FEELS"))
            {
                var feelsNode = data.GetNode("FEELS");

                if (feelsNode.HasNode("CREW"))
                {
                    var crewNode = feelsNode.GetNode("CREW");
                    KFUtil.SetConfigNode("CREW", crewNode);
                    //crewNode.Save(_crewDbSaveFileNameAndPath);
                }
                if (feelsNode.HasNode("FLIGHTS"))
                {
                    var flightsNode = feelsNode.GetNode("FLIGHTS");
                    KFUtil.SetConfigNode("FLIGHTS", flightsNode);

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

            var crewNode = KFUtil.GetConfigNode("CREW");//_crewDbSaveFileName, this.GetType());
            crewNode.name = "CREW";
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            flightNode.name = "FLIGHTS";

            feelNode.AddNode(crewNode);
            feelNode.AddNode(flightNode);
        }

        private void OnVesselGoOffRails(Vessel data)
        {
            KFUtil.Log("OnVesselGoOffRails");
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());

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
            
        }

        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewOnEva");

            //data.to.addChild(new KFEvaModule());

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
            //c.Sort((x, y) => x.NewFeel.CrewMember.CompareTo(y.NewFeel.CrewMember));

            List<string> strs = new List<string>();

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
