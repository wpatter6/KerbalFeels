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
        private static TimeSpan _spanToCheck = new TimeSpan(0, 0, 1);//1 sec
        private double _lastUT = 0;
        private Thread thread;
        public KFRepeater()
        {
            KFUtil.Log("KFRepeater Constructor");

            thread = new Thread(() => RepeatingCheck());
            //thread = new Thread((HighLogic.CurrentGame) => RepeatingCheck(Game game));
        }

        public void BeginRepeatingCheck()
        {
            KFUtil.Log("BeginRepeatingCheck");
            if (!thread.IsAlive)
                thread.Start();
        }

        private void RepeatingCheck()
        {
            try
            {
                int i = 0;
                while (true)
                {
                    var ut = KFConfig.CurrentTime;
                    if (ut > _lastUT)
                    {
                        _lastUT = ut;
                        //if (TimeWarp.CurrentRate == 1)
                        //{
                        KFUtil.Log("Repeat iteration: " + (++i).ToString());

                        foreach (ProtoCrewMember member in HighLogic.CurrentGame.CrewRoster.Crew.Where(x => x.rosterStatus == ProtoCrewMember.RosterStatus.Assigned))
                        {
                            KFCalc.CheckDeathEffects(member);
                        }
                        //if (FlightGlobals.ActiveVessel != null && !FlightGlobals.warpDriveActive)
                        //{
                        foreach (Vessel vessel in FlightGlobals.Vessels.Where(x => x.GetVesselCrew().Count > 0))
                        {
                            KFUtil.Log("Repeat check " + vessel.vesselName);
                            KFCalc.CalculateVesselChangedCrewInfo(vessel);
                            KFCalc.DetermineVesselCrewInfo(vessel);
                            KFCalc.DoSanityCheck(vessel);
                        }
                        //}
                        //else
                        //{
                        //    KFUtil.Log("Repeater time-warping " + TimeWarp.CurrentRate.ToString() + "x");
                        //    KFUtil.Log(TimeWarp.deltaTime);
                        //}
                        KFUtil.Log("KFConfig.CurrentTime: " + KFConfig.CurrentTime.ToString());
                    }
                    //}
                    Thread.Sleep(_spanToCheck);
                }
            }
            catch (Exception e)
            {
                KFUtil.LogError("RepeatingCheck error: " + e.Message);
                KFUtil.LogError(e.StackTrace);
                throw e;
            }
        }
    }
}
