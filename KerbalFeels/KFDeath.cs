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
            new KFGUI().ShowGuiDialog(String.Format("{0} committed suicide!",  member.name));
            member.Die();
        }

        public static void DoMurder(ProtoCrewMember attacker, ProtoCrewMember victim, Vessel vessel)
        {
            
            var text = new List<String>();
            var number = 0.0;
            var verdict = KFCalc.CaluclateMurderVerdict(attacker, victim, vessel, ref number);

            victim.Die();
            
            text.Add(String.Format("{0} murdered {1}!", KFUtil.GetFirstName(attacker), KFUtil.GetFirstName(victim)));
            switch (verdict)
            {
                case VerdictTypes.CapitalPunishment:
                    attacker.Die();
                    text.Add(String.Format("The crew flew into a rage and executed {0}!", KFUtil.GetFirstName(attacker)));
                    break;
                case VerdictTypes.Guilty:
                    attacker.isBadass = true;
                    attacker.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
                    attacker.Die();
                    var duration = KFCalc.GetMurderSentenceDuration(number);
                    attacker.StartRespawnPeriod(duration);
                    text.Add(String.Format("The crew arrested {0} and found him guilty! His jail sentence is {1} days.", KFUtil.GetFirstName(attacker), Convert.ToInt32(duration / 60 / 60 / 6)));
                    break;
                case VerdictTypes.Innocent:
                    attacker.isBadass = true;
                    text.Add(String.Format("The crew found {0} innocent!"));
                    break;
            }
            new KFGUI().ShowGuiDialog(text.ToArray());
        }

        public static void DoDeath(ProtoCrewMember deceased, Vessel vessel = null)
        {
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
    }
}
