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
            GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.onGameStateSave.Add(new EventData<ConfigNode>.OnEvent(OnGameStateSave));
            GameEvents.onGameStateLoad.Add(new EventData<ConfigNode>.OnEvent(OnGameStateLoad));
            //GameEvents.onCrash.Add(new EventData<EventReport>.OnEvent(OnCrash));
            _initialized = true;
        }

        #region event handlers
        //private void OnCrash(EventReport data)
        //{
        //    KFUtil.Log("OnCrash");
        //    KFUtil.Log(data.eventType.ToString());
        //    if (data.origin != null && data.origin.vessel != null)
        //        KFCalc.RemoveVessel(data.origin.vessel.id.ToString(), data.origin.vessel.GetVesselCrew());
        //}

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

            KFCalc.DetermineVesselCrewInfo(data);
        }

        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewOnEva");

            if (data.from != null && data.from.vessel != null)
                KFCalc.CalculateVesselChangedCrewInfoDelta(data.from.vessel);
        }

        private void OnCrewKilled(EventReport data)
        {//todo lower the courage & level of kerbals who had positive feels towards killed crew member -- need to somehow align with "revert"
            KFUtil.Log("OnCrewKilled");
            KFUtil.Log(data.msg);
        }

        private void OnPartUndock(Part data)
        {
            KFUtil.Log("OnPartUndock");
            KFCalc.CalculateVesselCrewStats(data.vessel);
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewBoardVessel");
            KFCalc.DetermineVesselCrewInfo(data.to.vessel);
        }

        private void OnVesselRecoveryProcessing(ProtoVessel data0, MissionRecoveryDialog data1, float data2)
        {
            KFUtil.Log("OnVesselRecoveryProcessing");
            var c = KFCalc.CalculateVesselCrewStats(data0, true);
            //c.Sort((x, y) => x.NewFeel.CrewMember.CompareTo(y.NewFeel.CrewMember));

            if(HighLogic.CurrentGame.config.HasNode("FEELS_CHANGE_TEXT"))
                HighLogic.CurrentGame.config.RemoveNode("FEELS_CHANGE_TEXT");

            var node = HighLogic.CurrentGame.config.AddNode("FEELS_CHANGE_TEXT");

            foreach (FeelsChange change in c)
            {
                var subnode = node.AddNode("TEXT");
                var text = KFUtil.GetFeelsChangeText(change);

                KFUtil.Log(text);
                subnode.AddValue("value", text);                
            }

            if (c.Count > 0)
            {
                KFUtil.Log("RenderingManager.AddToPostDrawQueue");
                RenderingManager.AddToPostDrawQueue(0, KFUtil.OnDrawGUI);
            }
        }

        private void OnKerbalStatusChange(ProtoCrewMember data0, ProtoCrewMember.RosterStatus data1, ProtoCrewMember.RosterStatus data2)
        {
            KFUtil.Log("OnKerbalStatusChange");
            KFUtil.Log("data0: " + data0.ToString());
            KFUtil.Log("data1: " + data1.ToString());
            KFUtil.Log("data2: " + data2.ToString());
        }

        private void OnGUIAstronautComplexSpawn()
        {//todo some kind of gui?
            KFUtil.Log("OnGUIAstronautComplexSpawn");
        }
        #endregion
    }
}
