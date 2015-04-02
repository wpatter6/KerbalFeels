using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;
using Random = System.Random;

namespace KerbalFeels
{
    static class KFUtil
    {

        #region logging
        private const bool _doLogging = true;
        private const string _logAppender = "FEELS: ";

        public static void Log(string message)
        {
            if(_doLogging)
                Debug.Log(_logAppender + message);
        }

        public static void Log(object message)
        {
            if (_doLogging)
                Log(message.ToString());
        }

        public static void LogError(string message)
        {
            if (_doLogging)
                Debug.LogError(_logAppender + message);
        }
        #endregion

        #region string util
        public static string GetFirstName(String name)
        {
            int ix = name.IndexOf(' ');
            if (ix == -1) return name;
            return name.Substring(0, ix);
        }

        public static string GetFirstName(ProtoCrewMember crewMember)
        {
            return GetFirstName(crewMember.name);
        }

        public static string GetFeelsString(Feels feel)
        {
            switch (feel.Type)
            {
                case FeelingTypes.InLove:
                    return "in love with";
                case FeelingTypes.Indifferent:
                    var pstr = new String(feel.Number < 0 ? '-' : '+', Convert.ToInt32(Math.Ceiling(Math.Abs(feel.Number) / 4)));
                    return feel.Type.ToString().ToLower() + pstr + " towards";
                case FeelingTypes.Playful:
                case FeelingTypes.Hateful:
                    return feel.Type.ToString().ToLower() + " towards";
                case FeelingTypes.Inspired:
                case FeelingTypes.Annoyed:
                    return feel.Type.ToString().ToLower() + " by";
                case FeelingTypes.Scared:
                    return "scared of";
                default:
                    return "indifferent towards";
            }
        }


        public static string GetFeelsChangeText(FeelsChange change)
        {
            String str = String.Format("{0} likes {1} {2}", change.NewFeel.CrewMember, change.NewFeel.ToCrewMember, GetFeelsChangeBasicText(change.TotalChange));;

            if (Math.Abs(change.NewFeel.Number - change.TotalChange) < KFConfig.FeelThreshold && change.NewFeel.Number > KFConfig.FeelThreshold)
            {
                str += String.Format(" and is now {0} them.", GetFeelsString(change.NewFeel));
            }
            else str += ".";
            return str;
        }

        public static string GetFeelsChangeBasicText(double change)
        {
            string text = "about the same";

            if (change > .1)
            {
                text = "more";
            }
            else if (change < -.1)
            {
                text = "less";
            }
            var abs = Math.Abs(change);
            if (abs > 5)
                text = "a lot " + text;
            else if (abs < 1 && abs > .1)
                text = "a little " + text;

            return text;
        }

        #endregion        
    }

    public struct FeelsChange
    {
        public double TotalChange;
        public Feels OldFeel;
        public Feels NewFeel;

        public FeelsChange(double totalChange, Feels oldFeel = new Feels(), Feels newFeel = new Feels())
        {
            TotalChange = totalChange;
            OldFeel = oldFeel;
            NewFeel = newFeel;
        }
    }

    public struct Feels
    {
        public string CrewMember;
        public string ToCrewMember;
        public double Number;
        public FeelingTypes Type;

        public Feels(string crewMember, string toCrewMember, double num = 0, FeelingTypes type = FeelingTypes.Indifferent)
        {
            Number = num;
            Type = type;
            CrewMember = crewMember;
            ToCrewMember = toCrewMember;
        }
    }

    //negative values will lower the kerbal's level by one, positive will raise by one.
    //Larger absolute values mean more impact on death
    public enum FeelingTypes
    {
        Hateful = -3,//raises stupidity and courage, can trigger a murder (maybe?)
        Scared = -2,//lowers courage
        Annoyed = -1,//raises stupidity
        Indifferent = 0,//does nothing
        Playful = 1,//reduces experience gained (maybe?)
        Inspired = 2,//raises experience gained (maybe?)
        InLove = 3//raises stupidity and courage
    }

    public enum VerdictTypes
    {
        Innocent,
        Guilty,
        CapitalPunishment
    }
}
