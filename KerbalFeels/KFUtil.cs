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
        #region tweakable constants
        /*
         * 
         * 
         * 
         * */
        public const float DefaultStupidityChange = .3F;
        public const float DefaultCourageChange = .3F;
        public const int DefaultExpChange = 1;

        private const double _sanityNumerator = 18;//the bigger this number, the more sanity affects the RNG portion of feels change
        private const double _baseSanity = 20;//if sanity drops too low and the kerbal has a feel status of "Hatred" towards another, they may try to murder them
        private const double _minSanity = 1;
        private const double _goodSanityModifier = .2;
        private const double _badSanityModifier = .2;


        private const int _precision = 4;
        private const float _randomMultiplier = 2;//How much RNG affects feels change
        private const float _personalityMultiplier = .5F;//How much personality affects feels change
        private const float _badassAddition = .5F;
        private const double _durationDivisor = 60 * 60 * 6;//How much each second together affects feels & sanity change(1/x; 60 * 60 * 6 = 1 day)
        private const double _feelThreshold = 25;//number that when gone past will trigger a new feeling type of either good or bad
        #endregion

        #region logging
        private const string _logAppender = "FEELS: ";

        public static void Log(string message)
        {
            Debug.Log(_logAppender + message);
        }

        public static void Log(object message)
        {
            Log(message.ToString());
        }

        public static void LogError(string message)
        {
            Debug.LogError(_logAppender + message);
        }
        #endregion

        #region general util
        public static string GetFirstName(String name)
        {
            return name.Substring(0, name.IndexOf(' '));
        }

        public static string GetFirstName(ProtoCrewMember crewMember)
        {
            return GetFirstName(crewMember.name);
        }

        public static string GetFeelsChangeText(FeelsChange change)
        {
            return String.Format("{0} likes {1} {2}", change.NewFeel.CrewMember, change.NewFeel.ToCrewMember, GetFeelsChangeText(change.NewFeel.Number - change.OldFeel.Number));
        }

        public static string GetFeelsChangeText(double change)
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

        public static ConfigNode GetConfigNode(String fileName, Type type)
        {
            var node = new ConfigNode();
            if (File.Exists<KerbalFeelsEvents>(fileName))
            {
                KFUtil.Log("crewDbSaveFile exists");
                try
                {
                    node = ConfigNode.Load(IOUtils.GetFilePathFor(type, fileName));
                }
                catch (Exception e)
                {
                    KFUtil.LogError("Error loading KerbalFeels crew database: " + e.ToString());
                }
            }
            return node;
        }
        #endregion

        #region stats & feels
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
        public static double CalculateCrewImpact(Feels feel, ProtoCrewMember crewMember1, ProtoCrewMember crewMember2, double sanity)
        {
            Log("CalculateCrewImpact");

            Single intDiff = Math.Abs(crewMember1.stupidity - crewMember2.stupidity) * _personalityMultiplier,//if number is positive, crewMember1 is smarter.  The bigger the difference the more negative impact
                courageDiff = (crewMember1.courage - crewMember2.courage) * _personalityMultiplier;//larger number more negative impact; smaller, positive

            Log("intDiff: " + intDiff.ToString());
            Log("courageDiff: " + courageDiff.ToString());

            Double random = (Single)new Random().NextDouble();

            var rnd = _randomMultiplier * _sanityNumerator / sanity;

            return random * (rnd * 2) - (rnd + courageDiff + intDiff) + (crewMember2.isBadass ? _badassAddition : 0);
        }

        public static Feels GetFeels(ConfigNode node, ProtoCrewMember crewMember1, ProtoCrewMember crewMember2)
        {
            Log("GetFeels");

            if (!node.HasNode(GetFirstName(crewMember1))) return new Feels();

            var crewNode = node.GetNode(GetFirstName(crewMember1));

            if (!crewNode.HasNode(GetFirstName(crewMember2))) return new Feels();

            var feelNode = crewNode.GetNode(GetFirstName(crewMember2));

            if (!feelNode.HasValue("num") || !feelNode.HasValue("type")) return new Feels();

            return new Feels(GetFirstName(crewMember1), GetFirstName(crewMember2), Convert.ToDouble(feelNode.GetValue("num")), (FeelingTypes)Convert.ToInt32(feelNode.GetValue("type")));
        }

        public static FeelsChange CalculateAndSetFeelingChange(ConfigNode node, double flightDuration, ProtoCrewMember crewMember1, ProtoCrewMember crewMember2, double sanity)
        {
            Log("CalculateAndSetFeelingChange");
            Log("flightDuration: " + flightDuration.ToString());

            var feel = GetFeels(node, crewMember1, crewMember2);
            var oldFeel = feel;
            var impact = CalculateCrewImpact(feel, crewMember1, crewMember2, sanity);

            Log("impact: " + impact.ToString());

            var change = Math.Round(impact * (flightDuration / _durationDivisor), _precision);//impact * days

            Log("change: " + change.ToString("G"));

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

            if (node.HasNode(GetFirstName(crewMember1)))
                crewNode = node.GetNode(GetFirstName(crewMember1));

            if (crewNode != null)
            {
                if(crewNode.HasNode(GetFirstName(crewMember2)))
                    feelNode = crewNode.GetNode(GetFirstName(crewMember2));
            }
            else
            {
                crewNode = node.AddNode(GetFirstName(crewMember1));
            }

            if (feelNode == null)
            {
                feelNode = crewNode.AddNode(GetFirstName(crewMember2));
            }


            Log("num: " + feel.Number.ToString("G"));
            Log("type: " + ((int)feel.Type).ToString());


            if(feelNode.HasValue("num"))
                feelNode.RemoveValue("num");
            feelNode.AddValue("num", feel.Number.ToString("G"));

            if (feelNode.HasValue("type"))
                feelNode.RemoveValue("type");
            feelNode.AddValue("type", (int)feel.Type);


            return new FeelsChange(oldFeel, feel);
        }
        #endregion

        #region gui
        private static Rect windowPosition = new Rect(150, 150, 220, 240);
        private static GUIStyle guiStyle = null;

        public static void OnDrawGUI()
        {
            Log("OnDrawGUI");
            guiStyle = new GUIStyle(HighLogic.Skin.window);
            windowPosition = GUI.Window(1, windowPosition, FeelsChangedWindowGUI, "Kerbal Feels", guiStyle);
        }

        public static void FeelsChangedWindowGUI(int WindowID)
        {
            Log("FeelsChangedWindowGUI");
            Log(HighLogic.CurrentGame.config.HasNode("FEELS_CHANGE_TEXT").ToString());
            if (HighLogic.CurrentGame.config.HasNode("FEELS_CHANGE_TEXT"))
            {
                var text = HighLogic.CurrentGame.config.GetNode("FEELS_CHANGE_TEXT");

                if (text.HasNode("TEXT"))
                {

                    Log("text.nodes.count: " + text.nodes.Count.ToString());

                    GUILayout.BeginVertical();

                    foreach (ConfigNode node in text.nodes)
                    {
                        string str = node.GetValue("value");
                        Log("str: " + str);
                        GUILayout.Label(str);
                    }

                    if (GUILayout.Button("Ok", guiStyle, GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
                    {
                        RenderingManager.RemoveFromPostDrawQueue(0, OnDrawGUI);
                    }

                    GUILayout.EndVertical();

                    GUI.DragWindow(new Rect(0, 0, 10000, 20));
                }
            }
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

            return Math.Max(_baseSanity - (t + (badCount * _badSanityModifier) - (goodCount * _goodSanityModifier)), _minSanity);
        }
        #endregion 
        
    }

    public struct FeelsChange
    {
        public Feels OldFeel;
        public Feels NewFeel;

        public FeelsChange(Feels oldFeel = new Feels(), Feels newFeel = new Feels())
        {
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
    public enum FeelingTypes
    {
        Hateful = -3,//raises stupidity and courage, can trigger a murder (maybe?)
        Bored = -2,//raises stupidity
        Scared = -1,//lowers courage
        Indifferent = 0,//does nothing
        Playful = 1,//reduces experience gained (maybe?)
        InLove = 2,//raises stupidity and courage
        Inspired = 3//raises experience gained (maybe?)
    } 
}
