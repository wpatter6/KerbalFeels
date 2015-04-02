using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalFeels
{
    //Class for performing calcluations
    static class KFCalc
    {

        #region utilites
        public static void CrewMemberStatChange(ProtoCrewMember member, ref float courageChange, ref float stupidityChange, ref int expChange)
        {
            KFUtil.Log("CrewMemberStatChange");
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

        public static double GetSanity(ProtoCrewMember crewMember)
        {
            KFUtil.Log("GetSanity");
            //var KFConfig.CrewNode = KFUtil.GetConfigNode("CREW");
            double sanity = KFConfig.BaseSanity;

            if (KFConfig.CrewNode.HasNode(KFUtil.GetFirstName(crewMember)))
            {
                if(KFConfig.CrewNode.GetNode(KFUtil.GetFirstName(crewMember)).HasValue("sanity"))
                    sanity = Convert.ToDouble(KFConfig.CrewNode.GetNode(KFUtil.GetFirstName(crewMember)).GetValue("sanity"));
            }

            return sanity;
        }

        public static void RemoveCrewEffect(string vesselId, ProtoCrewMember memberToUpdate, ProtoCrewMember memberToRemove, bool removeMemberFromVessel = false)
        {
            KFUtil.Log("RemoveCrewEffect");
            //var KFConfig.FlightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            if (KFConfig.FlightNode.HasNode(vesselId))
            {
                var vesselNode = KFConfig.FlightNode.GetNode(vesselId);

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

                        //if (removeMemberFromVessel)
                        //    memberNode.RemoveNode(KFUtil.GetFirstName(memberToRemove));
                    }
                }
            }
            //KFConfig.FlightNode.Save(_flightsDbSaveFileNameAndPath);
        }

        public static void RemoveVessel(string vesselId, List<ProtoCrewMember> crew)
        {
            KFUtil.Log("RemoveVessel");
            //var KFConfig.CrewNode = KFUtil.GetConfigNode("CREW");
            //var KFConfig.FlightNode = KFUtil.GetConfigNode("FLIGHTS");//_flightsDbSaveFileName, this.GetType());
            var crew2 = new List<ProtoCrewMember>(crew);

            if (KFConfig.FlightNode.HasNode(vesselId))
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
                KFConfig.FlightNode.RemoveNode(vesselId);
            }
        }

        public static Feels GetFeels(string crewMember1, string crewMember2)
        {
            crewMember1 = KFUtil.GetFirstName(crewMember1);
            crewMember2 = KFUtil.GetFirstName(crewMember2);

            KFUtil.Log("GetFeels");

            if (!KFConfig.CrewNode.HasNode(crewMember1)) return new Feels(crewMember1, crewMember2);

            var memberNode = KFConfig.CrewNode.GetNode(crewMember1);

            if (!memberNode.HasNode(crewMember2)) return new Feels(crewMember1, crewMember2);

            var feelNode = memberNode.GetNode(crewMember2);

            if (!feelNode.HasValue("num") || !feelNode.HasValue("type")) return new Feels(crewMember1, crewMember2);

            return new Feels(crewMember1, crewMember2, Convert.ToDouble(feelNode.GetValue("num")), (FeelingTypes)Convert.ToInt32(feelNode.GetValue("type")));
        }

        public static Feels GetFeels(ProtoCrewMember crewMember1, ProtoCrewMember crewMember2)
        {
            return GetFeels(crewMember1.name, crewMember2.name);
        }

        public static List<KeyValuePair<string, Feels>> GetAllFeels(ProtoCrewMember member)
        {
            KFUtil.Log("GetAllFeels");
            var dict = new List<KeyValuePair<string, Feels>>();
            var memberNode = KFConfig.GetConfigNode(KFConfig.CrewNode, KFUtil.GetFirstName(member));

            foreach (ConfigNode node in memberNode.nodes)
            {
                if(node.name != "DEATH")
                    dict.Add(new KeyValuePair<string, Feels>(node.name, GetFeels(member.name, node.name)));
            }
            return dict;
        }

        //public static bool HasFeels(ProtoCrewMember member)
        //{
        //    return KFConfig.CrewNode.HasNode(KFUtil.GetFirstName(member)) && KFConfig.CrewNode.GetNode(KFUtil.GetFirstName(member)).HasNode();
        //}
        #endregion

        #region calculations
        #region sanity
        //checks if sanity will trigger murder or suicide on a vessel
        public static bool DoSanityCheck(Vessel vessel)
        {
            KFUtil.Log("DoSanityCheck");
            var crew = new List<ProtoCrewMember>(vessel.GetVesselCrew());
            var crew2 = new List<ProtoCrewMember>(crew);
            var burning = !FlightGlobals.ActiveVessel.acceleration.IsZero();

            foreach (ProtoCrewMember crewMember1 in crew)
            {//do sanity check
                var sanity = KFCalc.GetSanity(crewMember1);
                if (sanity < KFConfig.SanityThreshold)
                {
                    var rnd = new Random().NextDouble();
                    if (rnd < KFConfig.SuicideChance)//bad dice roll, suicide
                    {
                        KFDeath.DoSuicide(crewMember1, vessel);
                        return true;
                    }

                    foreach (ProtoCrewMember crewMember2 in crew2)
                    {
                        var feel = KFCalc.GetFeels(crewMember1, crewMember2);

                        if ((rnd * KFConfig.MurderRandomMultiplier * (KFConfig.SanityThreshold - sanity)) + ((int)feel.Type * (Math.Abs(feel.Number) / 10) * -1) > 20)
                        {
                            KFDeath.DoMurder(crewMember1, crewMember2, vessel);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //calculates and returns the current sanity of the crew member
        public static double CalculateSanity(ProtoCrewMember member, List<ProtoCrewMember> crew, List<Part> vesselParts, ConfigNode memberNode, double timeSpent)
        {
            KFUtil.Log("CalculateSanity");
            int goodCount = 0, badCount = 0, totalCount = crew.Count;
            double currentSanity = KFConfig.BaseSanity;

            if (memberNode.HasValue("lastSanityCheck"))
            {
                timeSpent = Math.Min(timeSpent, HighLogic.CurrentGame.UniversalTime - Convert.ToDouble(memberNode.GetValue("lastSanityCheck")));
                memberNode.SetValue("lastSanityCheck", HighLogic.CurrentGame.UniversalTime.ToString());
            }
            else
                memberNode.AddValue("lastSanityCheck", HighLogic.CurrentGame.UniversalTime.ToString());

            foreach (ProtoCrewMember checkMember in crew)
            {
                if (memberNode.HasNode(KFUtil.GetFirstName(checkMember)))
                {
                    var feel = Convert.ToInt32(memberNode.GetNode(KFUtil.GetFirstName(checkMember)).GetValue("type"));

                    if (feel > 0) goodCount += feel;
                    else if (feel < 0) badCount += 1;
                }
            }

            var sanityDivisorBase = (totalCount * (member.courage + 1) + (goodCount * KFConfig.GoodSanityModifier) - (badCount * KFConfig.BadSanityModifier));
            double sanityDivisor;
            
            if (sanityDivisorBase < -1)
                sanityDivisor = 1 / Math.Abs(sanityDivisorBase);//this will cause sanity to drop very quickly if they get below -1...
            else if (sanityDivisorBase > -1 && sanityDivisorBase < 1)
                sanityDivisor = 1;
            else
                sanityDivisor = sanityDivisorBase;

            if (memberNode.HasValue("sanity"))
                currentSanity = Convert.ToDouble(memberNode.GetValue("sanity"));

            var t = timeSpent / KFConfig.DurationDivisor;//this value equals one if divisor

            return Math.Min(Math.Max(currentSanity - t / sanityDivisor, KFConfig.MinSanity), KFConfig.BaseSanity);
        }

        public static double AddSanity(ProtoCrewMember member, double amount)
        {
            var memberNode = KFConfig.GetConfigNode(KFConfig.CrewNode, KFUtil.GetFirstName(member));
            double currentSanity = KFConfig.BaseSanity;

            if (memberNode.HasValue("sanity"))
                currentSanity = Convert.ToDouble(memberNode.GetValue("sanity"));

            currentSanity = Math.Min(currentSanity + amount, KFConfig.BaseSanity);

            if (memberNode.HasValue("sanity"))
                memberNode.SetValue("sanity", currentSanity.ToString());
            else
                memberNode.AddValue("sanity", currentSanity);

            return currentSanity;
        }
        #endregion

        #region feels effects
        //Sets the kerbal's altered courage/stupidity/xp based on who they're in a vessel with
        public static void DetermineVesselCrewInfo(string vesselId, List<ProtoCrewMember> crew)
        {
            KFUtil.Log("DetermineVesselCrewInfo");
            ConfigNode flightInfo;
            if (KFConfig.FlightNode.HasNode(vesselId))
                flightInfo = KFConfig.FlightNode.GetNode(vesselId);
            else
                flightInfo = KFConfig.FlightNode.AddNode(vesselId);

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

                if (KFConfig.CrewNode.HasNode(KFUtil.GetFirstName(crewMember1)) && !KFConfig.CrewNode.GetNode(KFUtil.GetFirstName(crewMember1)).HasValue("sanity"))
                    KFConfig.CrewNode.GetNode(KFUtil.GetFirstName(crewMember1)).AddValue("sanity", KFConfig.BaseSanity);

                foreach (ProtoCrewMember crewMember2 in crew2)
                {
                    if (KFConfig.CrewNode.HasNode(KFUtil.GetFirstName(crewMember1)) && crewMember1.name != crewMember2.name)
                    {//Determine their modifier for the current flight & store so it can be switched back when they are separated again.
                        Feels f = KFCalc.GetFeels(crewMember1, crewMember2);
                        ConfigNode changeNode;

                        if (fcnode.HasNode(KFUtil.GetFirstName(crewMember2)))
                            changeNode = fcnode.GetNode(KFUtil.GetFirstName(crewMember2));
                        else
                            changeNode = fcnode.AddNode(KFUtil.GetFirstName(crewMember2));

                        float stupidityChange = KFConfig.StupidityChange, courageChange = KFConfig.CourageChange;
                        int expChange = KFConfig.ExpChange;

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
                        if (changeNode.HasValue("courage"))
                            changeNode.SetValue("courage", courageChange.ToString());
                        else
                            changeNode.AddValue("courage", courageChange);

                        if (changeNode.HasValue("stupidity"))
                            changeNode.SetValue("stupidity", stupidityChange.ToString());
                        else
                            changeNode.AddValue("stupidity", stupidityChange);

                        if (changeNode.HasValue("experience"))
                            changeNode.SetValue("experience", expChange.ToString());
                        else
                            changeNode.AddValue("experience", expChange);
                    }
                }
            }
            //KFUtil.Log("KFConfig.FlightNode:" + KFConfig.FlightNode.ToString());
            //KFUtil.Log("_flightsDbSaveFileName:" + _flightsDbSaveFileNameAndPath);
            //KFConfig.FlightNode.Save(_flightsDbSaveFileNameAndPath);
        }

        public static void DetermineVesselCrewInfo(Vessel data)
        {
            DetermineVesselCrewInfo(data.id.ToString(), data.GetVesselCrew());
        }
        #endregion

        #region feels calculation
        //calculates and saves the feeling change on crewMember1 towards crewMember2
        public static FeelsChange DoFeelingChange(ProtoCrewMember crewMember1, ProtoCrewMember crewMember2, ConfigNode _crewNode, ConfigNode _memberNode, double sanity = double.MinValue, double flightDuration = double.MinValue, double staticValue = double.MinValue)
        {
            KFUtil.Log("DoFeelingChange");
            var feel = GetFeels(crewMember1, crewMember2);
            var oldFeel = feel;
            var change = staticValue;
            if (staticValue == double.MinValue)
            {
                var impact = CalculateCrewMemberImpact(crewMember1, crewMember2, sanity);

                change = Math.Round(impact * (flightDuration / KFConfig.DurationDivisor), KFConfig.Precision);//impact * days

                feel.Number += change;
            }
            else feel.Number = staticValue;

            if (feel.Type == FeelingTypes.Indifferent)
            {//if passed feeling threshold assign a random type
                if (feel.Number > KFConfig.FeelThreshold)
                {
                    feel.Type = (FeelingTypes)(new Random().Next(2) + 1);
                }
                else if (feel.Number < KFConfig.FeelThreshold * -1)
                {
                    feel.Type = (FeelingTypes)((new Random().Next(2) + 1) * -1);
                }
            }
            else
            {
                if (feel.Number <= KFConfig.FeelThreshold && feel.Number >= KFConfig.FeelThreshold * -1)
                    feel.Type = FeelingTypes.Indifferent;
            }

            ConfigNode crewMemberNode = null, feelNode = null;

            if (_crewNode.HasNode(KFUtil.GetFirstName(crewMember1)))
                crewMemberNode = _crewNode.GetNode(KFUtil.GetFirstName(crewMember1));
            else
                crewMemberNode = _crewNode.AddNode(KFUtil.GetFirstName(crewMember1));

            if (crewMemberNode.HasNode(KFUtil.GetFirstName(crewMember2)))
                feelNode = crewMemberNode.GetNode(KFUtil.GetFirstName(crewMember2));
            else
                feelNode = crewMemberNode.AddNode(KFUtil.GetFirstName(crewMember2));

            if (feelNode.HasValue("num"))
                feelNode.RemoveValue("num");
            feelNode.AddValue("num", feel.Number.ToString("G"));

            if (feelNode.HasValue("type"))
                feelNode.RemoveValue("type");
            feelNode.AddValue("type", (int)feel.Type);

            if (feelNode.HasValue("lastCheckTime"))
                feelNode.RemoveValue("lastCheckTime");
            feelNode.AddValue("lastCheckTime", HighLogic.CurrentGame.UniversalTime);

            if (_memberNode.HasValue(KFUtil.GetFirstName(crewMember2)))
            {
                var num = Convert.ToDouble(_memberNode.GetValue(KFUtil.GetFirstName(crewMember2)));
                change += num;
                _memberNode.SetValue(KFUtil.GetFirstName(crewMember2), change.ToString());
            }
            else _memberNode.AddValue(KFUtil.GetFirstName(crewMember2), change);

            return new FeelsChange(change, oldFeel, feel);
        }
        
        //Calculate feels values for a single crew member towards members of the crew -- determines time span and return DoFeelingChange results
        public static List<FeelsChange> CalculateCrewStats(ProtoCrewMember member, List<ProtoCrewMember> crew, ConfigNode _vesselNode, ConfigNode _crewNode, double sanity)
        {
            KFUtil.Log("CalculateCrewStats");
            var memberNode = _vesselNode.HasNode(KFUtil.GetFirstName(member)) ? _vesselNode.GetNode(KFUtil.GetFirstName(member)) : _vesselNode.AddNode(KFUtil.GetFirstName(member));
            var crewMemberNode = _crewNode.HasNode(KFUtil.GetFirstName(member)) ? _crewNode.GetNode(KFUtil.GetFirstName(member)) : _crewNode.AddNode(KFUtil.GetFirstName(member));

            List<FeelsChange> FeelsChanges = new List<FeelsChange>();
            foreach (ProtoCrewMember member2 in crew)
            {
                if (member == member2) continue;
                var member2Node = _vesselNode.HasNode(KFUtil.GetFirstName(member2)) ? _vesselNode.GetNode(KFUtil.GetFirstName(member2)) : _vesselNode.AddNode(KFUtil.GetFirstName(member2));

                double start1 = HighLogic.CurrentGame.UniversalTime, start2 = HighLogic.CurrentGame.UniversalTime, lastCheck = 0;
                if (memberNode.HasValue("startTime"))
                    start1 = Convert.ToDouble(memberNode.GetValue("startTime"));
                if (member2Node.HasValue("startTime"))
                    start2 = Convert.ToDouble(member2Node.GetValue("startTime"));

                if (crewMemberNode.HasNode(KFUtil.GetFirstName(member2))
                    && crewMemberNode.GetNode(KFUtil.GetFirstName(member2)).HasValue("lastCheckTime"))
                    lastCheck = Convert.ToDouble(crewMemberNode.GetNode(KFUtil.GetFirstName(member2)).GetValue("lastCheckTime"));

                double timeSpan = HighLogic.CurrentGame.UniversalTime - Math.Max(Math.Max(start1, start2), lastCheck);

                FeelsChanges.Add(DoFeelingChange(member, member2, _crewNode, memberNode, sanity, timeSpan));
            }
            return FeelsChanges;
        }

        //Calculate feels values for all crew members in the vessel towards each other
        public static List<FeelsChange> CalculateVesselCrewStats(String vesselId, List<ProtoCrewMember> crew, List<Part> vesselParts, bool removeVessel = false)
        {
            KFUtil.Log("CalculateVesselCrewStats");
            var changes = new List<FeelsChange>();

            if (KFConfig.FlightNode.HasNode(vesselId))
            {
                var vesselNode = KFConfig.FlightNode.GetNode(vesselId);
                foreach (ProtoCrewMember member in crew)
                {
                    var vesselMemberNode = KFConfig.GetConfigNode(vesselNode, KFUtil.GetFirstName(member));
                    var memberNode = KFConfig.GetConfigNode(KFConfig.CrewNode, KFUtil.GetFirstName(member));

                    var timeSpent = HighLogic.CurrentGame.UniversalTime - Convert.ToDouble(vesselMemberNode.GetValue("startTime"));
                    
                    var sanity = CalculateSanity(member, new List<ProtoCrewMember>(crew), vesselParts, memberNode, timeSpent);

                    changes.AddRange(CalculateCrewStats(member, new List<ProtoCrewMember>(crew), vesselNode, KFConfig.CrewNode, sanity));

                    if (removeVessel)
                    {
                        memberNode.RemoveValue("sanity");
                        memberNode.RemoveValue("lastSanityCheck");
                    }
                }

                //KFConfig.CrewNode.Save(_crewDbSaveFileNameAndPath);

                if (removeVessel)
                {
                    KFConfig.FlightNode.RemoveNode(vesselId);
                    //KFConfig.FlightNode.Save(_flightsDbSaveFileNameAndPath);
                }
            }
            return changes;
        }

        //Overload
        public static List<FeelsChange> CalculateVesselCrewStats(Vessel data, bool removeVessel = false)
        {
            return CalculateVesselCrewStats(data.id.ToString(), data.GetVesselCrew(), data.parts, removeVessel);
        }

        //Overload
        public static List<FeelsChange> CalculateVesselCrewStats(ProtoVessel data, bool removeVessel = false)
        {
            KFUtil.Log("CalculateVesselCrewStats (o2)");
            List<Part> parts = new List<Part>();
            foreach (ProtoPartSnapshot part in data.protoPartSnapshots)
            {
                parts.Add(part.partRef);
            }

            return CalculateVesselCrewStats(data.vesselID.ToString(), data.GetVesselCrew(), parts, removeVessel);
        }
        
        //returns a number between -x and x to indicate impact of feelings from crewMember1 towards crewMember2, may need tweaking
        public static double CalculateCrewMemberImpact(ProtoCrewMember crewMember1, ProtoCrewMember crewMember2, double sanity)
        {
            KFUtil.Log("CalculateCrewImpact");

            Single intDiff = (Math.Abs(crewMember1.stupidity - crewMember2.stupidity) / KFConfig.StupidityDivisor * KFConfig.PersonalityMultiplier) - KFConfig.StupidityBalancer,//if number is positive, crewMember1 is smarter.  The bigger the difference the more negative impact
                courageDiff = (crewMember1.courage - crewMember2.courage) / KFConfig.CourageDivisor * KFConfig.PersonalityMultiplier;//larger number more negative impact; smaller, positive

            Double random = (Single)new Random().NextDouble();

            var rnd = KFConfig.SanityNumerator / sanity;

            return random * (rnd * 2) - (rnd + courageDiff + intDiff) + (crewMember2.isBadass ? KFConfig.BadassAddition : 0);
        }
        
        //Calculate the feels values for crew members that were in vessel
        public static void CalculateVesselChangedCrewInfo(Vessel vessel)
        {
            KFUtil.Log("CalculateVesselChangedCrewInfo");
            String vesselId = vessel.id.ToString();
            List<ProtoCrewMember> crew = vessel.GetVesselCrew();
            if (KFConfig.FlightNode.HasNode(vesselId))
            {
                var vesselNode = KFConfig.FlightNode.GetNode(vesselId);
                var remNodes = new ConfigNode.ConfigNodeList();
                foreach (ConfigNode node in vesselNode.nodes)
                {
                    var crewMember = HighLogic.CurrentGame.CrewRoster.Crew.First(x => KFUtil.GetFirstName(x) == node.name);
                    var memberNode = KFConfig.GetConfigNode(KFConfig.CrewNode, KFUtil.GetFirstName(crewMember));
                    var start = Convert.ToDouble(vesselNode.GetNode(KFUtil.GetFirstName(crewMember)).GetValue("startTime"));
                    Double sanity;

                    var timeSpent = HighLogic.CurrentGame.UniversalTime - start;

                    if (memberNode.HasValue("sanity"))
                        memberNode.SetValue("sanity", (sanity = CalculateSanity(crewMember, crew, vessel.parts, memberNode, timeSpent)).ToString());
                    else
                        memberNode.AddValue("sanity", (sanity = CalculateSanity(crewMember, crew, vessel.parts, memberNode, timeSpent)));


                    if (crewMember.rosterStatus != ProtoCrewMember.RosterStatus.Dead)
                    {
                        CalculateCrewStats(crewMember, crew, vesselNode, KFConfig.CrewNode, sanity);
                    }


                    if (crew.Where(x => KFUtil.GetFirstName(x) == node.name).Count() == 0)
                    {//the kerbal left the vessel for some reason, remove their effect on other crew and the effects on them
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
            }
        }

        public static void CalculateProgressFeelsBoost(Vessel vessel)
        {
            //TODO
            KFUtil.Log("CalculateProgressFeelsBoost");
        }
        #endregion

        #region death
        //How much one crew member's death affects one crew member's stats
        public static void CalculateDeathEffects(ProtoCrewMember deceased, ProtoCrewMember living, bool isInVessel = false)
        {
            KFUtil.Log("CalculateDeathEffects");
            bool isMissing = deceased.rosterStatus == ProtoCrewMember.RosterStatus.Missing;

            var memberNode = new ConfigNode();
            if(KFConfig.CrewNode.HasNode(KFUtil.GetFirstName(living)))
                memberNode = KFConfig.CrewNode.GetNode(KFUtil.GetFirstName(living));

            var deceasedNode = new ConfigNode();
            if (memberNode.HasNode(KFUtil.GetFirstName(deceased)))
                deceasedNode = memberNode.GetNode(KFUtil.GetFirstName(deceased));

            var feelType = 0;
            if (deceasedNode.HasValue("type"))
                feelType = Convert.ToInt32(deceasedNode.GetValue("type"));

            var sub = feelType / KFConfig.DeathImpactDivisor;//negative feelings will boost courage but not xp

            if (isInVessel) sub /= 2;//we've already had impact so lower it.
            if (isMissing) sub /= 2;//missing or jail sentence, lower impact.

            var deathNode = memberNode.AddNode("DEATH");

            deathNode.AddValue("kerb", KFUtil.GetFirstName(deceased));
            deathNode.AddValue("missing", isMissing);

            var x = living.courage;


            living.courage = Math.Max(living.courage - sub, 0.0F);

            x = x - living.courage;

            deathNode.AddValue("courage", x);

            if (living.courage == 0 && isInVessel && !isMissing && living.experienceLevel > 0)
            {//lose exp if they lose all courage and they witnessed the death
                deathNode.AddValue("xp", 1);
                living.experienceLevel -= 1;
            }
            else deathNode.AddValue("xp", 0);
            var until = HighLogic.CurrentGame.UniversalTime + new Random().Next(7, 60) * 6 * 60 * 60 * Math.Abs(feelType);

            if(isMissing) until /= 2;
            deathNode.AddValue("until", until);//for between 1 week and 2 months, multiplied if feel type bigger
        }

        //based on feelings towards defendant & victim
        public static VerdictTypes CaluclateMurderVerdict(ProtoCrewMember defendent, ProtoCrewMember victim, Vessel vessel, ref double verdict)
        {
            KFUtil.Log("CaluclateMurderVerdict");
            var jury = vessel.GetVesselCrew();
            var vesselNode = KFConfig.FlightNode.HasNode(vessel.id.ToString()) ? KFConfig.FlightNode.GetNode(vessel.id.ToString()) : KFConfig.FlightNode.AddNode(vessel.id.ToString());
            var num = 0.0;
            var ct = 0;

            foreach (ProtoCrewMember member in jury)
            {
                if (member == defendent || member == victim) continue;
                var memberNode = vesselNode.HasNode(KFUtil.GetFirstName(member)) ? vesselNode.GetNode(KFUtil.GetFirstName(member)) : vesselNode.AddNode(KFUtil.GetFirstName(member));
                var df = KFCalc.GetFeels(member, defendent);
                var vf = KFCalc.GetFeels(member, victim);

                df.Number -= KFConfig.MurderImpact + vf.Number;//impact increased or decreased by how much they liked the victim

                num += df.Number;

                DoFeelingChange(member, defendent, KFConfig.CrewNode, memberNode, 0, 0, df.Number);
                ct++;
            }
            if (ct == 0) return VerdictTypes.Innocent;//there was no one else in the vessel, they got away with it

            verdict = num / ct;
            var threshold = KFConfig.FeelThreshold / 2;

            if (verdict > threshold) return VerdictTypes.Innocent;//should be pretty rare unless they are well loved
            else if (verdict < threshold * -1) return VerdictTypes.CapitalPunishment;
            return VerdictTypes.Guilty;
        }

        //Applies a little RNG as well as how strongly the verdict was one way or the other
        public static double GetMurderSentenceDuration(double number)
        {
            KFUtil.Log("GetMurderSentenceDuration");
            return ((new Random().NextDouble() + 1) * 60 * 60 * 6 * number) + KFConfig.AverageMurderSentence;
        }

        //Check if effects of death have expired
        public static void CheckDeathEffects(ProtoCrewMember member)
        {
            var memberNode = KFConfig.GetConfigNode(KFConfig.CrewNode, KFUtil.GetFirstName(member));

            if (memberNode.HasNode("DEATH"))
            {
                KFUtil.Log("CheckDeathEffects");
                var changed = false;
                var keepNodes = new List<ConfigNode>();
                foreach (ConfigNode node in memberNode.GetNodes("DEATH"))
                {
                    if (Convert.ToDouble(node.GetValue("until")) >= HighLogic.CurrentGame.UniversalTime)
                    {
                        changed = true;
                        member.courage += Convert.ToSingle(node.GetValue("courage"));
                        member.experienceLevel += Convert.ToInt32(node.GetValue("xp"));;
                    } else keepNodes.Add(node);
                }
                if (changed)
                {
                    memberNode.RemoveNodes("DEATH");
                    foreach (ConfigNode node in keepNodes)
                    {
                        memberNode.AddNode(node);
                    }
                }
            }
        }
        #endregion
        #endregion

    }
}
