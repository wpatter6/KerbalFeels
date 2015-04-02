using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFeels
{
    static class KFDeath
    {
        public static void DoSuicide(ProtoCrewMember member, Vessel vessel)
        {
            KFUtil.Log("DoSuicide");
            new KFGUI().ShowGuiDialog(String.Format("{0} committed suicide!",  member.name));
            KillCrewMember(member, vessel);
        }

        public static void DoMurder(ProtoCrewMember attacker, ProtoCrewMember victim, Vessel vessel)
        {
            KFUtil.Log("DoMurder");
            var text = new List<String>();
            var number = 0.0;
            var verdict = KFCalc.CaluclateMurderVerdict(attacker, victim, vessel, ref number);

            if (TimeWarp.CurrentRate > 1) TimeWarp.SetRate(1, true);
            KillCrewMember(victim, vessel);
            
            text.Add(String.Format("{0} murdered {1}!", KFUtil.GetFirstName(attacker), KFUtil.GetFirstName(victim)));
            switch (verdict)
            {
                case VerdictTypes.CapitalPunishment:
                    KillCrewMember(attacker, vessel);
                    text.Add(String.Format("The crew flew into a rage and executed {0}!", KFUtil.GetFirstName(attacker)));
                    break;
                case VerdictTypes.Guilty:
                    attacker.isBadass = true;
                    var duration = KFCalc.GetMurderSentenceDuration(number);
                    KillCrewMember(attacker, vessel, duration);
                    text.Add(String.Format("The crew arrested {0} and found him guilty! His jail sentence is {1} days.", KFUtil.GetFirstName(attacker), Convert.ToInt32(duration / 60 / 60 / 6)));
                    break;
                case VerdictTypes.Innocent:
                    attacker.isBadass = true;
                    text.Add(String.Format("The crew found {0} innocent!", KFUtil.GetFirstName(attacker)));
                    break;
            }
            new KFGUI().ShowGuiDialog(text.ToArray());
        }

        public static void DoDeath(ProtoCrewMember deceased, Vessel vessel = null)
        {
            KFUtil.Log("DoDeath");
            foreach (ProtoCrewMember member in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                if (member == deceased) continue;
                KFCalc.CalculateDeathEffects(deceased, member);
            }
            if (vessel != null)
            {
                var crew = vessel.GetVesselCrew();
                foreach (ProtoCrewMember member in crew)
                {
                    KFCalc.CalculateDeathEffects(deceased, member, true);
                }
            }
        }

        public static void KillCrewMember(ProtoCrewMember crewMember, Vessel vessel, double respawnDelay = double.MinValue)
        {
            if (!vessel.isEVA)
            {
                Part part = vessel.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                if (part != null)
                {
                    part.RemoveCrewmember(crewMember);
                    crewMember.Die();

                    if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn || respawnDelay > double.MinValue)
                    {
                        if (respawnDelay == double.MinValue) respawnDelay = KFConfig.RespawnDelay;
                        crewMember.StartRespawnPeriod(respawnDelay);
                    }
                }
            }
            else
            {
                vessel.rootPart.Die();

                if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn || respawnDelay > double.MinValue)
                {
                    if (respawnDelay == double.MinValue) respawnDelay = KFConfig.RespawnDelay;
                    crewMember.StartRespawnPeriod(respawnDelay);
                }
            }
        }
    }
}
