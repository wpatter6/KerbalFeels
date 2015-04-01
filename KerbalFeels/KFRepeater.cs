using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Random = System.Random;

namespace KerbalFeels
{
    class KFRepeater
    {
        private const int _msToCheck = 10 * 1000;


        public KFRepeater()
        {
            
        }

        public void BeginRepeatingCheck()
        {
            Thread thread = new Thread(() => RepeatingCheck());
            thread.Start();
        }

        private void RepeatingCheck()
        {
            try
            {
                while (true)
                {
                    foreach (ProtoCrewMember member in HighLogic.CurrentGame.CrewRoster.Crew.Where(x => x.rosterStatus == ProtoCrewMember.RosterStatus.Assigned))
                    {
                        KFCalc.CheckDeathEffects(member);
                    }
                    if (FlightGlobals.ActiveVessel != null && !FlightGlobals.warpDriveActive)
                    {
                        KFUtil.Log("RepeatingCheck iteration");

                        KFCalc.CalculateVesselChangedCrewInfo(FlightGlobals.ActiveVessel);
                        KFCalc.DetermineVesselCrewInfo(FlightGlobals.ActiveVessel);

                        KFCalc.DoSanityCheck(FlightGlobals.ActiveVessel);
                        var crew = FlightGlobals.ActiveVessel.GetVesselCrew();
                        
                    }
                    Thread.Sleep(_msToCheck);//sleep for _minsToCheck
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
