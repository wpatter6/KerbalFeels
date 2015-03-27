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
    public class KerbalFeelsEvents
    {
        private bool _initialized = false;
        private string _crewDbSaveFileName;
        private string _crewDbSaveFileNameAndPath;
        private string _flightsDbSaveFileName;
        private string _flightsDbSaveFileNameAndPath;

        public KerbalFeelsEvents(string crewFileName, string flightsFileName, string crewFileFullPath, string flightsFileFullPath)
        {
            _crewDbSaveFileName = crewFileName;
            _crewDbSaveFileNameAndPath = crewFileFullPath;
            _flightsDbSaveFileName = flightsFileName;
            _flightsDbSaveFileNameAndPath = flightsFileFullPath;
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
            GameEvents.onCrash.Add(new EventData<EventReport>.OnEvent(OnCrash));
            _initialized = true;
        }

        #region event handlers
        private void OnCrash(EventReport data)
        {
            KFUtil.Log("OnCrash");
            KFUtil.Log(data.eventType.ToString());
            if (data.origin != null && data.origin.vessel != null)
                RemoveVessel(data.origin.vessel.id.ToString(), data.origin.vessel.GetVesselCrew());
        }

        private void OnGameStateLoad(ConfigNode data)
        {
            KFUtil.Log("OnGameStateLoad");

            if (data.HasNode("FEELS"))
            {
                var feelsNode = data.GetNode("FEELS");

                if (feelsNode.HasNode("CREW"))
                {
                    var crewNode = feelsNode.GetNode("CREW");
                    crewNode.Save(_crewDbSaveFileNameAndPath);
                }
                if (feelsNode.HasNode("FLIGHTS"))
                {
                    var flightsNode = feelsNode.GetNode("FLIGHTS");
                    flightsNode.Save(_flightsDbSaveFileNameAndPath);
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

            var crewNode = KFUtil.GetConfigNode(_crewDbSaveFileName, this.GetType());
            crewNode.name = "CREW";
            var flightNode = KFUtil.GetConfigNode(_flightsDbSaveFileName, this.GetType());
            flightNode.name = "FLIGHTS";

            feelNode.AddNode(crewNode);
            feelNode.AddNode(flightNode);
        }

        private void OnVesselGoOffRails(Vessel data)
        {
            KFUtil.Log("OnVesselGoOffRails");

            DetermineVesselCrewInfo(data);
        }

        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewOnEva");

            if (data.from != null && data.from.vessel != null)
            {
                CalculateVesselChangedCrewInfoDelta(data.from.vessel);
            }
        }

        private void OnCrewKilled(EventReport data)
        {//todo lower the courage & level of kerbals who had positive feels towards killed crew member -- need to somehow align with "revert"
            KFUtil.Log("OnCrewKilled");
            KFUtil.Log(data.msg);
        }

        private void OnPartUndock(Part data)
        {
            KFUtil.Log("OnPartUndock");
            CalculateVesselCrewStats(data.vessel);
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            KFUtil.Log("OnCrewBoardVessel");
            DetermineVesselCrewInfo(data.to.vessel);
        }

        private void OnVesselRecoveryProcessing(ProtoVessel data0, MissionRecoveryDialog data1, float data2)
        {
            KFUtil.Log("OnVesselRecoveryProcessing");
            var c = CalculateVesselCrewStats(data0, true);
            c.Sort((x, y) => x.NewFeel.CrewMember.CompareTo(y.NewFeel.CrewMember));

            if(HighLogic.CurrentGame.config.HasNode("FEELS_CHANGE_TEXT"))
                HighLogic.CurrentGame.config.RemoveNode("FEELS_CHANGE_TEXT");

            var node = HighLogic.CurrentGame.config.AddNode("FEELS_CHANGE_TEXT");

            foreach (FeelsChange change in c)
            {
                var subnode = node.AddNode("TEXT");
                subnode.AddValue("value", KFUtil.GetFeelsChangeText(change));                
            }

            KFUtil.Log("RenderingManager.AddToPostDrawQueue");
            RenderingManager.AddToPostDrawQueue(0, KFUtil.OnDrawGUI);
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

        #region reusable methods
        //Sets the kerbal's altered base stats based on who they're in a vessel with
        private void DetermineVesselCrewInfo(string vesselId, List<ProtoCrewMember> crew)
        {
            var isNew = true;
            var flightNode = KFUtil.GetConfigNode(_flightsDbSaveFileName, this.GetType());
            var crewNode = KFUtil.GetConfigNode(_crewDbSaveFileName, this.GetType());

            ConfigNode flightInfo;
            if (flightNode.HasNode(vesselId)) //return;//it is already being tracked -- need better event than "VesselGoOffRails"
                flightInfo = flightNode.GetNode(vesselId);
            else
                flightInfo = flightNode.AddNode(vesselId);

            var crew2 = new List<ProtoCrewMember>(crew);
            foreach (ProtoCrewMember crewMember1 in crew)
            {
                ConfigNode fcnode;
                if (flightInfo.HasNode(KFUtil.GetFirstName(crewMember1)))
                    fcnode = flightInfo.GetNode(KFUtil.GetFirstName(crewMember1));
                else
                    fcnode = flightInfo.AddNode(KFUtil.GetFirstName(crewMember1));

                if (!fcnode.HasValue("startTime"))
                    fcnode.AddValue("startTime", HighLogic.CurrentGame.UniversalTime);

                if (!isNew)//has existing feels
                    foreach (ProtoCrewMember crewMember2 in crew2)
                    {
                        if (crewMember1.name != crewMember2.name && !fcnode.HasNode(KFUtil.GetFirstName(crewMember2)))
                        {//Determine their modifier for the current flight & store so it can be switched back when they are separated again.
                            Feels f = KFUtil.GetFeels(crewNode, crewMember1, crewMember2);
                            var changeNode = fcnode.AddNode(KFUtil.GetFirstName(crewMember2));

                            float stupidityChange = KFUtil.DefaultStupidityChange, courageChange = KFUtil.DefaultCourageChange;
                            int expChange = KFUtil.DefaultExpChange;

                            switch (f.Type)
                            {//TODO
                                case FeelingTypes.Hateful://-1 level,raises stupidity and courage, can trigger a murder (maybe? todo)
                                    expChange *= -1;
                                    KFUtil.CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                    break;
                                case FeelingTypes.Bored://-1 level,raises stupidity
                                    courageChange = 0;
                                    expChange *= -1;
                                    KFUtil.CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                    break;
                                case FeelingTypes.Scared://-1 level, lowers courage
                                    stupidityChange = 0;
                                    courageChange *= -1;
                                    expChange *= -1;
                                    KFUtil.CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                    break;
                                case FeelingTypes.Playful://+1 level, raises stupditiy, reduces experience gained (maybe? todo)
                                    courageChange = 0;
                                    KFUtil.CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                    break;
                                case FeelingTypes.InLove://+1 level,raises stupidity and courage
                                    KFUtil.CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                    break;
                                case FeelingTypes.Inspired://+1 level, lowers stupidity, raises experience gained (maybe? todo)
                                    stupidityChange *= -1;
                                    courageChange = 0;
                                    KFUtil.CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                    break;
                                case FeelingTypes.Indifferent:
                                    courageChange = stupidityChange = expChange = 0;
                                    break;
                            }
                            changeNode.AddValue("courage", courageChange);
                            changeNode.AddValue("stupidity", stupidityChange);
                            changeNode.AddValue("experience", expChange);
                        }
                    }
            }
            KFUtil.Log("flightNode:" + flightNode.ToString());
            KFUtil.Log("_flightsDbSaveFileName:" + _flightsDbSaveFileNameAndPath);
            flightNode.Save(_flightsDbSaveFileNameAndPath);
        }

        private void DetermineVesselCrewInfo(Vessel data)
        {
            DetermineVesselCrewInfo(data.id.ToString(), data.GetVesselCrew());
        }

        //Calculate the values for crew members that were in the vessel but are no longer.
        private void CalculateVesselChangedCrewInfoDelta(String vesselId, List<ProtoCrewMember> crew)
        {
            var crewNode = KFUtil.GetConfigNode(_crewDbSaveFileName, this.GetType());
            var flightNode = KFUtil.GetConfigNode(_flightsDbSaveFileName, this.GetType());

            if (flightNode.HasNode(vesselId))
            {
                var vesselNode = flightNode.GetNode(vesselId);
                var remNodes = new ConfigNode.ConfigNodeList();
                foreach (ConfigNode node in vesselNode.nodes)
                {
                    if (crew.Where(x => KFUtil.GetFirstName(x) == node.name).Count() == 0)
                    {

                        var crewMember = HighLogic.CurrentGame.CrewRoster.Crew.Where(x => KFUtil.GetFirstName(x) == node.name).ToArray()[0];

                        var timeSpent = HighLogic.CurrentGame.UniversalTime - Convert.ToDouble(vesselNode.GetNode(KFUtil.GetFirstName(crewMember)).GetValue("startTime"));
                        var sanity = KFUtil.CalculateSanity(crewMember, crew, crewNode, timeSpent);

                        if (crewMember.rosterStatus != ProtoCrewMember.RosterStatus.Dead)
                        {
                            CalculateCrewStats(crewMember, crew, vesselNode, crewNode, sanity);
                        }

                        foreach (ProtoCrewMember member in crew)
                        {
                            RemoveCrewEffect(vesselId, crewMember, member);
                            RemoveCrewEffect(vesselId, member, crewMember, true);
                        }

                        remNodes.Add(node);
                    }
                }
                foreach (ConfigNode node in remNodes)
                {
                    vesselNode.RemoveNode(node.name);
                }

                flightNode.Save(_flightsDbSaveFileName);
                crewNode.Save(_crewDbSaveFileNameAndPath);
            }
        }

        private void CalculateVesselChangedCrewInfoDelta(Vessel data)
        {
            CalculateVesselChangedCrewInfoDelta(data.id.ToString(), data.GetVesselCrew());
        }

        //Calculate feels values for a single crew member
        private List<FeelsChange> CalculateCrewStats(ProtoCrewMember member, List<ProtoCrewMember> crew, ConfigNode vesselNode, ConfigNode crewNode, double sanity)
        {
            List<FeelsChange> FeelsChanges = new List<FeelsChange>();
            foreach (ProtoCrewMember member2 in crew)
            {
                if (member == member2) continue;

                double start1 = HighLogic.CurrentGame.UniversalTime, start2 = HighLogic.CurrentGame.UniversalTime;
                if (vesselNode.HasNode(KFUtil.GetFirstName(member)))
                    start1 = Convert.ToDouble(vesselNode.GetNode(KFUtil.GetFirstName(member)).GetValue("startTime"));
                if (vesselNode.HasNode(KFUtil.GetFirstName(member2)))
                    start2 = Convert.ToDouble(vesselNode.GetNode(KFUtil.GetFirstName(member)).GetValue("startTime"));

                double timeSpan = HighLogic.CurrentGame.UniversalTime - Math.Max(start1, start2);

                FeelsChanges.Add(KFUtil.CalculateAndSetFeelingChange(crewNode, timeSpan, member, member2, sanity));
            }
            return FeelsChanges;
        }

        //Calculate feels values for crew members in the vessel
        private List<FeelsChange> CalculateVesselCrewStats(String vesselId, List<ProtoCrewMember> crew, bool removeVessel = false)
        {
            var changes = new List<FeelsChange>();
            var crewNode = KFUtil.GetConfigNode(_crewDbSaveFileName, this.GetType());
            var flightNode = KFUtil.GetConfigNode(_flightsDbSaveFileName, this.GetType());

            if (flightNode.HasNode(vesselId))
            {
                var vesselNode = flightNode.GetNode(vesselId);
                var crew2 = new List<ProtoCrewMember>(crew);
                var crew3 = new List<ProtoCrewMember>(crew);
                foreach (ProtoCrewMember member in crew)
                {
                    var timeSpent = HighLogic.CurrentGame.UniversalTime - Convert.ToDouble(vesselNode.GetNode(KFUtil.GetFirstName(member)).GetValue("startTime"));
                    var sanity = KFUtil.CalculateSanity(member, crew3, crewNode, timeSpent);

                    changes.AddRange(CalculateCrewStats(member, crew2, vesselNode, crewNode, sanity));
                }

                crewNode.Save(_crewDbSaveFileNameAndPath);

                if (removeVessel)
                {
                    flightNode.RemoveNode(vesselId);
                    flightNode.Save(_flightsDbSaveFileNameAndPath);
                }
            }
            return changes;
        }

        private List<FeelsChange> CalculateVesselCrewStats(Vessel data, bool removeVessel = false)
        {
            return CalculateVesselCrewStats(data.id.ToString(), data.GetVesselCrew(), removeVessel);
        }

        private List<FeelsChange> CalculateVesselCrewStats(ProtoVessel data, bool removeVessel = false)
        {
            return CalculateVesselCrewStats(data.vesselID.ToString(), data.GetVesselCrew(), removeVessel);
        }

        

        private void RemoveCrewEffect(string vesselId, ProtoCrewMember memberToUpdate, ProtoCrewMember memberToRemove, bool removeMemberFromVessel = false)
        {
            var flightNode = KFUtil.GetConfigNode(_flightsDbSaveFileName, this.GetType());
            if (flightNode.HasNode(vesselId))
            {
                var vesselNode = flightNode.GetNode(vesselId);

                if (vesselNode.HasNode(KFUtil.GetFirstName(memberToUpdate)))
                {
                    var memberNode = vesselNode.GetNode(KFUtil.GetFirstName(memberToUpdate));

                    if (memberNode.HasNode(KFUtil.GetFirstName(memberToRemove)))
                    {
                        var removeNode = memberNode.GetNode(KFUtil.GetFirstName(memberToRemove));
                        var courageChange = Convert.ToSingle(removeNode.GetValue("courage"));
                        var stupidityChange = Convert.ToSingle(removeNode.GetValue("stupidity"));
                        var expChange = Convert.ToInt32(removeNode.GetValue("experience"));

                        memberToUpdate.courage -= courageChange;
                        memberToUpdate.stupidity -= stupidityChange;
                        memberToUpdate.experienceLevel -= expChange;

                        if(removeMemberFromVessel)
                            memberNode.RemoveNode(KFUtil.GetFirstName(memberToRemove));
                    }
                }
            }
            flightNode.Save(_flightsDbSaveFileNameAndPath);
        }

        private void RemoveVessel(string vesselId, List<ProtoCrewMember> crew)
        {
            var flightNode = KFUtil.GetConfigNode(_flightsDbSaveFileName, this.GetType());
            var crew2 = new List<ProtoCrewMember>(crew);

            if (flightNode.HasNode(vesselId))
            {
                foreach (ProtoCrewMember member1 in crew)
                {
                    crew2.Remove(member1);
                    foreach (ProtoCrewMember member2 in crew2)
                    {
                        RemoveCrewEffect(vesselId, member1, member2);
                        RemoveCrewEffect(vesselId, member2, member1);
                    }
                }
                flightNode.RemoveNode(vesselId);
            }
        }
        #endregion
    }
}
