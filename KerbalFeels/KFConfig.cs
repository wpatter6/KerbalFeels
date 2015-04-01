using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KerbalFeels
{
    static class KFConfig
    {
        #region public properties
        public static int MurderImpact
        {
            get
            {
                return Convert.ToInt32(GetValue("MurderImpact"));
            }
        }
        public static int DeathImpactDivisor
        {
            get
            {
                return Convert.ToInt32(GetValue("DeathImpactDivisor"));
            }
        }
        public static int MurderRandomMultiplier
        {
            get
            {
                return Convert.ToInt32(GetValue("MurderRandomMultiplier"));
            }
        }
        public static int SanityThreshold
        {
            get
            {
                return Convert.ToInt32(GetValue("SanityThreshold"));
            }
        }
        public static int ExpChange
        {
            get
            {
                return Convert.ToInt32(GetValue("ExpChange"));
            }
        }
        public static int Precision
        {
            get
            {
                return Convert.ToInt32(GetValue("Precision"));
            }
        }
        public static double AverageMurderSentence
        {
            get
            {
                return Convert.ToDouble(GetValue("AverageMurderSentence"));
            }
        }
        public static double SuicideChance
        {
            get
            {
                return Convert.ToDouble(GetValue("SuicideChance"));
            }
        }
        public static double SanityNumerator
        {
            get
            {
                return Convert.ToDouble(GetValue("SanityNumerator"));
            }
        }
        public static double BaseSanity
        {
            get
            {
                return Convert.ToDouble(GetValue("BaseSanity"));
            }
        }
        public static double MinSanity
        {
            get
            {
                return Convert.ToDouble(GetValue("MinSanity"));
            }
        }
        public static double GoodSanityModifier
        {
            get
            {
                return Convert.ToDouble(GetValue("GoodSanityModifier"));
            }
        }
        public static double BadSanityModifier
        {
            get
            {
                return Convert.ToDouble(GetValue("BadSanityModifier"));
            }
        }
        public static double TotalSanityModifier
        {
            get
            {
                return Convert.ToDouble(GetValue("TotalSanityModifier"));
            }
        }
        public static double ProgressNodeBoost
        {
            get
            {
                return Convert.ToDouble(GetValue("ProgressNodeBoost"));
            }
        }
        public static double DurationDivisor
        {
            get
            {
                return Convert.ToDouble(GetValue("DurationDivisor"));
            }
        }
        public static double MaxDuration
        {
            get
            {
                return Convert.ToDouble(GetValue("MaxDuration"));
            }
        }
        public static double FeelThreshold
        {
            get
            {
                return Convert.ToDouble(GetValue("FeelThreshold"));
            }
        }
        public static float StupidityDivisor
        {
            get
            {
                return Convert.ToSingle(GetValue("StupidityDivisor"));
            }
        }
        public static float CourageDivisor
        {
            get
            {
                return Convert.ToSingle(GetValue("CourageDivisor"));
            }
        }
        public static float StupidityChange
        {
            get
            {
                return Convert.ToSingle(GetValue("StupidityChange"));
            }
        }
        public static float CourageChange
        {
            get
            {
                return Convert.ToSingle(GetValue("CourageChange"));
            }
        }
        public static float RandomMultiplier
        {
            get
            {
                return Convert.ToSingle(GetValue("RandomMultiplier"));
            }
        }
        public static float PersonalityMultiplier
        {
            get
            {
                return Convert.ToSingle(GetValue("PersonalityMultiplier"));
            }
        }
        public static float StupidityBalancer
        {
            get
            {
                return Convert.ToSingle(GetValue("StupidityBalancer"));
            }
        }
        public static float BadassAddition
        {
            get
            {
                return Convert.ToSingle(GetValue("BadassAddition"));
            }
        }

        public static ConfigNode CrewNode
        {
            get
            {
                return KFUtil.GetConfigNode("CREW");
            }
        }
        public static ConfigNode FlightNode
        {
            get
            {
                return KFUtil.GetConfigNode("FLIGHTS");
            }
        }
        #endregion

        #region default values
        private const double _defaultAverageMurderSentence = 60 * 60 * 6 * 426.08 * 2;//2 years
        private const int _defaultMurderImpact = 10;
        private const int _defaultDeathImpactDivisor = 10;
        private const int _defaultMurderRandomMultiplier = 5;
        private const int _defaultSanityThreshold = 5;
        private const double _defaultSuicideChance = .02;

        private const float _defaultStupidityChange = .3F;
        private const float _defaultCourageChange = .3F;
        private const int _defaultExpChange = 1;

        private const double _defaultSanityNumerator = 18;//the bigger this number, the more sanity affects the RNG portion of feels change
        private const double _defaultBaseSanity = 50;//if sanity drops too low and the kerbal has a feel status of "Hatred" towards another, they may try to murder them
        private const double _defaultMinSanity = 1;
        private const double _defaultGoodSanityModifier = .5;//how much good feelings reduce sanity loss
        private const double _defaultBadSanityModifier = .2;//how much bad feelings increase sanity loss
        private const double _defaultTotalSanityModifier = .05;//how much the number of total crew members reduces sanity loss

        private const double _defaultProgressNodeBoost = 2;

        private const float _defaultStupidityDivisor = 2;//used to change the effect of stupidity difference on feelings
        private const float _defaultCourageDivisor = 1;//used to change the effect of courage difference on feelings

        private const int _defaultPrecision = 4;
        private const float _defaultRandomMultiplier = 2;//How much RNG affects feels change
        private const float _defaultPersonalityMultiplier = .5F;//How much personality affects feels change
        private const float _defaultStupidityBalancer = .3F;//Moves stupidity into the positive if they're this close

        private const float _defaultBadassAddition = .5F;
        private const double _defaultDurationDivisor = 60 * 60 * 6;//How much each second together affects feels & sanity change(1/x; 60 * 60 * 6 = 1 day)
        private const double _defaultMaxDuration = 20;
        private const double _defaultFeelThreshold = 20;//number that when gone past will trigger a new feeling type of either good or bad
        #endregion

        private static ConfigNode config;

        public static void Init()
        {
            if (File.Exists<KFStartup>("KF_Constants.cfg"))
            {
                try
                {
                    config = ConfigNode.Load(IOUtils.GetFilePathFor(typeof(KFStartup), "KF_Constants.cfg"));
                }
                catch (Exception e)
                {
                    Debug.LogError("KFConfig.Init caught an exception trying to load KF_Constants.cfg: " + e);
                }
            }
            else
            {
                config = new ConfigNode();
                config.Save(IOUtils.GetFilePathFor(typeof(KFConfig), "KF_Constants.cfg"));
            }

        }

        public static object SetValue(string valueName, object value)
        {
            if (config.HasValue(valueName)) config.SetValue(valueName, value.ToString());
            else config.AddValue(valueName, value);
            config.Save(IOUtils.GetFilePathFor(typeof(KFConfig), "KF_Constants.cfg"));
            return value;
        }

        public static string GetValue(string valueName)
        {
            return config.HasValue(valueName) ? config.GetValue(valueName) : null;
        }
    }
}
