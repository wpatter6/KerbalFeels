using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalFeels
{
    //Class for performing calcluations
    static class KFCalc
    {
        #region tweakable constants
        private const float _defaultStupidityChange = .3F;
        private const float _defaultCourageChange = .3F;
        private const int _defaultExpChange = 1;

        private const double _sanityNumerator = 18;//the bigger this number, the more sanity affects the RNG portion of feels change
        private const double _baseSanity = 20;//if sanity drops too low and the kerbal has a feel status of "Hatred" towards another, they may try to murder them
        private const double _minSanity = 1;
        private const double _goodSanityModifier = .2;
        private const double _badSanityModifier = .2;

        private const float _stupidityDivisor = 2;//used to change the effect of stupidity difference on feelings
        private const float _courageDivisor = 1;//used to change the effect of courage difference on feelings

        private const int _precision = 4;
        private const float _randomMultiplier = 2;//How much RNG affects feels change
        private const float _personalityMultiplier = .5F;//How much personality affects feels change
        private const float _badassAddition = .5F;
        private const double _durationDivisor = 60 * 60 * 6;//How much each second together affects feels & sanity change(1/x; 60 * 60 * 6 = 1 day)
        private const double _feelThreshold = 25;//number that when gone past will trigger a new feeling type of either good or bad
        #endregion

        public static void CrewMemberStatChange(ProtoCrewMember member, ref float courageChange, ref float stupidityChange, ref int expChange)
        {
            if (member.stupidity + stupidityChange > 1)
            {
                stupidityChange = 1 - member.stupidity;
                member.stupidity = 1;
            }
            else member.stupidity += stupidityChange;

            if (member.courage + courageChange > 1)
            {
                courageChange = 1 - member.courage;
                member.courage = 1;
            }
            else member.courage += courageChange;

            if (member.experienceLevel + expChange < 0)
            {
                expChange = member.experienceLevel;
                member.experienceLevel = 0;
            }
            else member.experienceLevel += expChange;

        }

        //returns a number between -x and x to indicate impact, may need tweaking
        public static double CalculateCrewImpact(ProtoCrewMember crewMember1, ProtoCrewMember crewMember2, double sanity)
        {
            KFUtil.Log("CalculateCrewImpact");

            Single intDiff = Math.Abs(crewMember1.stupidity - crewMember2.stupidity) / _stupidityDivisor * _personalityMultiplier,//if number is positive, crewMember1 is smarter.  The bigger the difference the more negative impact
                courageDiff = (crewMember1.courage - crewMember2.courage) / _courageDivisor * _personalityMultiplier;//larger number more negative impact; smaller, positive

            KFUtil.Log("intDiff: " + intDiff.ToString());
            KFUtil.Log("courageDiff: " + courageDiff.ToString());

            Double random = (Single)new Random().NextDouble();

            var rnd = _randomMultiplier * _sanityNumerator / sanity;

            return random * (rnd * 2) - (rnd + courageDiff + intDiff) + (crewMember2.isBadass ? _badassAddition : 0);
        }

        public static Feels GetFeels(ConfigNode node, ProtoCrewMember crewMember1, ProtoCrewMember crewMember2)
        {
            KFUtil.Log("GetFeels");

            if (!node.HasNode(KFUtil.GetFirstName(crewMember1))) return new Feels(KFUtil.GetFirstName(crewMember1), KFUtil.GetFirstName(crewMember2));

            var crewNode = node.GetNode(KFUtil.GetFirstName(crewMember1));

            if (!crewNode.HasNode(KFUtil.GetFirstName(crewMember2))) return new Feels(KFUtil.GetFirstName(crewMember1), KFUtil.GetFirstName(crewMember2));

            var feelNode = crewNode.GetNode(KFUtil.GetFirstName(crewMember2));

            if (!feelNode.HasValue("num") || !feelNode.HasValue("type")) return new Feels(KFUtil.GetFirstName(crewMember1), KFUtil.GetFirstName(crewMember2));

            return new Feels(KFUtil.GetFirstName(crewMember1), KFUtil.GetFirstName(crewMember2), Convert.ToDouble(feelNode.GetValue("num")), (FeelingTypes)Convert.ToInt32(feelNode.GetValue("type")));
        }

        public static FeelsChange CalculateAndSetFeelingChange(ConfigNode node, double flightDuration, ProtoCrewMember crewMember1, ProtoCrewMember crewMember2, double sanity)
        {
            KFUtil.Log("CalculateAndSetFeelingChange");
            KFUtil.Log("flightDuration: " + flightDuration.ToString());

            var feel = GetFeels(node, crewMember1, crewMember2);
            var oldFeel = feel;
            var impact = CalculateCrewImpact(crewMember1, crewMember2, sanity);

            KFUtil.Log("impact: " + impact.ToString());

            var change = Math.Round(impact * (flightDuration / _durationDivisor), _precision);//impact * days

            KFUtil.Log("change: " + change.ToString("G"));

            feel.Number += change;

            if (feel.Type == FeelingTypes.Indifferent)
            {//if passed feeling threshold assign a random type
                if (feel.Number > _feelThreshold)
                {
                    feel.Type = (FeelingTypes)(new Random().Next(2) + 1);
                }
                else if (feel.Number < _feelThreshold * -1)
                {
                    feel.Type = (FeelingTypes)((new Random().Next(2) + 1) * -1);
                }
            }
            else
            {
                if (feel.Number <= _feelThreshold && feel.Number >= _feelThreshold * -1)
                    feel.Type = FeelingTypes.Indifferent;
            }

            ConfigNode crewNode = null, feelNode = null;

            if (node.HasNode(KFUtil.GetFirstName(crewMember1)))
                crewNode = node.GetNode(KFUtil.GetFirstName(crewMember1));
            else
                crewNode = node.AddNode(KFUtil.GetFirstName(crewMember1));

            if (crewNode.HasNode(KFUtil.GetFirstName(crewMember2)))
                feelNode = crewNode.GetNode(KFUtil.GetFirstName(crewMember2));
            else 
                feelNode = crewNode.AddNode(KFUtil.GetFirstName(crewMember2));


            KFUtil.Log("num: " + feel.Number.ToString("G"));
            KFUtil.Log("type: " + feel.Type.ToString());


            if (feelNode.HasValue("num"))
                feelNode.RemoveValue("num");
            feelNode.AddValue("num", feel.Number.ToString("G"));

            if (feelNode.HasValue("type"))
                feelNode.RemoveValue("type");
            feelNode.AddValue("type", (int)feel.Type);


            return new FeelsChange(oldFeel, feel);
        }

        //Sets the kerbal's altered base stats based on who they're in a vessel with
        public static void DetermineVesselCrewInfo(string vesselId, List<ProtoCrewMember> crew)
        {
            KFUtil.Log("DetermineVesselCrewInfo");
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            var crewNode = KFUtil.GetConfigNode("CREW");//_crewDbSaveFileName, this.GetType());

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

                foreach (ProtoCrewMember crewMember2 in crew2)
                {
                    if (crewNode.HasNode(KFUtil.GetFirstName(crewMember1)) && crewMember1.name != crewMember2.name && !fcnode.HasNode(KFUtil.GetFirstName(crewMember2)))
                    {//Determine their modifier for the current flight & store so it can be switched back when they are separated again.
                        Feels f = KFCalc.GetFeels(crewNode, crewMember1, crewMember2);
                        var changeNode = fcnode.AddNode(KFUtil.GetFirstName(crewMember2));

                        float stupidityChange = _defaultStupidityChange, courageChange = _defaultCourageChange;
                        int expChange = _defaultExpChange;

                        switch (f.Type)
                        {
                            case FeelingTypes.Hateful://-1 level,raises stupidity and courage, can trigger a murder (maybe? todo)
                                expChange *= -1;
                                CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                break;
                            case FeelingTypes.Annoyed://-1 level,raises stupidity
                                courageChange = 0;
                                expChange *= -1;
                                CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                break;
                            case FeelingTypes.Scared://-1 level, lowers courage
                                stupidityChange = 0;
                                courageChange *= -1;
                                expChange *= -1;
                                CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                break;
                            case FeelingTypes.Playful://+1 level, raises stupditiy, reduces experience gained (maybe? todo)
                                courageChange = 0;
                                CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                break;
                            case FeelingTypes.InLove://+1 level,raises stupidity and courage
                                CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
                                break;
                            case FeelingTypes.Inspired://+1 level, lowers stupidity, raises experience gained (maybe? todo)
                                stupidityChange *= -1;
                                courageChange = 0;
                                CrewMemberStatChange(crewMember1, ref courageChange, ref stupidityChange, ref expChange);
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
            //KFUtil.Log("flightNode:" + flightNode.ToString());
            //KFUtil.Log("_flightsDbSaveFileName:" + _flightsDbSaveFileNameAndPath);
            //flightNode.Save(_flightsDbSaveFileNameAndPath);
        }

        public static void DetermineVesselCrewInfo(Vessel data)
        {
            DetermineVesselCrewInfo(data.id.ToString(), data.GetVesselCrew());
        }

        //Calculate the values for crew members that were in the vessel but are no longer.
        public static void CalculateVesselChangedCrewInfoDelta(String vesselId, List<ProtoCrewMember> crew)
        {
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            var crewNode = KFUtil.GetConfigNode("CREW");//_crewDbSaveFileName, this.GetType());

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
                        var sanity = CalculateSanity(crewMember, crew, crewNode, timeSpent);

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

                //flightNode.Save(_flightsDbSaveFileName);
                //crewNode.Save(_crewDbSaveFileNameAndPath);
            }
        }

        public static void CalculateVesselChangedCrewInfoDelta(Vessel data)
        {
            CalculateVesselChangedCrewInfoDelta(data.id.ToString(), data.GetVesselCrew());
        }

        //Calculate feels values for a single crew member
        public static List<FeelsChange> CalculateCrewStats(ProtoCrewMember member, List<ProtoCrewMember> crew, ConfigNode vesselNode, ConfigNode crewNode, double sanity)
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

                FeelsChanges.Add(CalculateAndSetFeelingChange(crewNode, timeSpan, member, member2, sanity));
            }
            return FeelsChanges;
        }

        //Calculate feels values for crew members in the vessel
        public static List<FeelsChange> CalculateVesselCrewStats(String vesselId, List<ProtoCrewMember> crew, bool removeVessel = false)
        {
            KFUtil.Log("CalculateVesselCrewStats");
            var changes = new List<FeelsChange>();
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            var crewNode = KFUtil.GetConfigNode("CREW");//_crewDbSaveFileName, this.GetType());


            KFUtil.Log(flightNode);
            KFUtil.Log(crewNode);

            if (flightNode.HasNode(vesselId))
            {
                var vesselNode = flightNode.GetNode(vesselId);
                var crew2 = new List<ProtoCrewMember>(crew);
                var crew3 = new List<ProtoCrewMember>(crew);
                foreach (ProtoCrewMember member in crew)
                {
                    KFUtil.Log("Calculating stats for " + KFUtil.GetFirstName(member));
                    var timeSpent = HighLogic.CurrentGame.UniversalTime - Convert.ToDouble(vesselNode.GetNode(KFUtil.GetFirstName(member)).GetValue("startTime"));

                    var sanity = CalculateSanity(member, crew3, crewNode, timeSpent);

                    changes.AddRange(CalculateCrewStats(member, crew2, vesselNode, crewNode, sanity));
                }

                //crewNode.Save(_crewDbSaveFileNameAndPath);

                if (removeVessel)
                {
                    flightNode.RemoveNode(vesselId);
                    //flightNode.Save(_flightsDbSaveFileNameAndPath);
                }
            }
            return changes;
        }

        public static List<FeelsChange> CalculateVesselCrewStats(Vessel data, bool removeVessel = false)
        {
            return CalculateVesselCrewStats(data.id.ToString(), data.GetVesselCrew(), removeVessel);
        }

        public static List<FeelsChange> CalculateVesselCrewStats(ProtoVessel data, bool removeVessel = false)
        {
            return CalculateVesselCrewStats(data.vesselID.ToString(), data.GetVesselCrew(), removeVessel);
        }

        public static double CalculateSanity(ProtoCrewMember member, List<ProtoCrewMember> crew, ConfigNode crewNode, double timeSpent)
        {
            int goodCount = 0, badCount = 0;

            var memberNode = new ConfigNode();
            if (crewNode.HasNode(KFUtil.GetFirstName(member)))
                memberNode = crewNode.GetNode(KFUtil.GetFirstName(member));

            foreach (ProtoCrewMember checkMember in crew)
            {
                if (memberNode.HasNode(KFUtil.GetFirstName(checkMember)))
                {
                    var feel = Convert.ToInt32(memberNode.GetNode(KFUtil.GetFirstName(checkMember)).GetValue("type"));

                    if (feel > 0) goodCount++;
                    else if (feel < 0) badCount++;
                }
            }

            var t = timeSpent / _durationDivisor;

            return Math.Max(_baseSanity - ((t + (badCount * _badSanityModifier) - (goodCount * _goodSanityModifier)) * (1 / member.courage)), _minSanity);
        }

        public static void RemoveCrewEffect(string vesselId, ProtoCrewMember memberToUpdate, ProtoCrewMember memberToRemove, bool removeMemberFromVessel = false)
        {
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
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

                        if (removeMemberFromVessel)
                            memberNode.RemoveNode(KFUtil.GetFirstName(memberToRemove));
                    }
                }
            }
            //flightNode.Save(_flightsDbSaveFileNameAndPath);
        }

        public static void RemoveVessel(string vesselId, List<ProtoCrewMember> crew)
        {
            var flightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
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
    }
}
