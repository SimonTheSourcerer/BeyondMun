using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BeyondMun
{
    public class ModuleHibernate : PartModule
    {

        public const float TIME_ONE_MINUTE = 60;
        public const float TIME_ONE_HOUR = TIME_ONE_MINUTE * 60;
        public const float TIME_ONE_DAY = TIME_ONE_HOUR * 6;

        public string[] STATE_TRANSLATION = new string[]
        {
            "BROKEN",
            "SHUTTING DOWN",
            "SLEEPING",
            "BOOTING",
            "ACTIVE"
        };

        public enum HybernationState
        {
            BROKEN,
            SHUTTING_DOWN,
            SLEEPING,
            BOOTING,
            ACTIVE
        }
        
        HybernationState state = HybernationState.ACTIVE;

        float progress = 0;
        float sleepTime = 0;

        float timeToBoot = 2 * TIME_ONE_HOUR;
        float energyReductionRate = 0.1f;

        [KSPField(guiActive = true, guiName = "Hib. status")]
        private string statusLine = "ACTIVE";


        private ModuleCommand commandModule;
        private ModuleResource ressourceModule;

        private void SetStatus()
        {
            if (state == HybernationState.SHUTTING_DOWN || state == HybernationState.BOOTING)
            {
                statusLine = state.ToString() + "(" + GetReadableTimeString(progress) + ")";
            }
            else if (state == HybernationState.SLEEPING)
            {
                statusLine = state.ToString() + "(" + GetReadableTimeString(sleepTime) + ")";
            } 
            else
            {
                statusLine = state.ToString();
            }
        }

        private string GetReadableTimeString(float time)
        {
            string readableString = "";
            float days = (time / TIME_ONE_DAY);
            if (days >= 1)
            {
                readableString = days.ToString("0.0") + "d ";
            }
            else if (time > TIME_ONE_HOUR)
            {
                readableString = (time / TIME_ONE_HOUR).ToString("0.0") + "h";
            }
            else if (time < TIME_ONE_HOUR)
            {
                readableString = (time / 60).ToString("0") + "m";
            }            
            return readableString;
        }

        /// <summary>
        /// Called when the part is started by Unity.
        /// </summary>
        public override void OnStart(StartState state)
        {
            commandModule = part.Modules.GetModule<ModuleCommand>();
            
            foreach (ModuleResource res in commandModule.inputResources)
            {
                if (res.name == "ElectricCharge")
                {
                    ressourceModule = res;
                    break;
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            switch (state)
            {
                case HybernationState.SLEEPING:
                    commandModule.part.RequestResource("ElectricCharge", ressourceModule.rate * energyReductionRate * TimeWarp.deltaTime);
                    sleepTime -= TimeWarp.deltaTime;
                    if (sleepTime <= 0)
                    {
                        state = HybernationState.BOOTING;
                        progress = timeToBoot;
                    }
                    break;

                case HybernationState.SHUTTING_DOWN:
                    commandModule.part.RequestResource("ElectricCharge", ressourceModule.rate * TimeWarp.deltaTime);
                    progress -= TimeWarp.deltaTime;
                    if (progress <= 0)
                    {
                        state = HybernationState.SLEEPING;
                    }
                    break;

                case HybernationState.BOOTING:
                    commandModule.part.RequestResource("ElectricCharge", ressourceModule.rate * TimeWarp.deltaTime);
                    progress -= TimeWarp.deltaTime;
                    if (progress <= 0)
                    {
                        state = HybernationState.ACTIVE;
                        commandModule.minimumCrew = 0;
                    }
                    break;
            }

            SetStatus();
        }

        public void GoHibernate(float time)
        {
            if (commandModule != null)
            {
                state = HybernationState.SHUTTING_DOWN;
                statusLine = state.ToString();
                progress = timeToBoot;
                sleepTime = time;
                commandModule.minimumCrew = 1;
            }
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false, guiName = "Hibernate")]
        private void Hibernate()
        {
            HibernateGUI.callingModule = this;
            HibernateGUI.OnToggleTrue();
        }

        public void Load(ConfigNode config)
        {
            base.Load(config);

            if (float.TryParse(config.GetValue("timeToBoot"), out timeToBoot))
            {
                timeToBoot *= TIME_ONE_HOUR;
            }

            float.TryParse(config.GetValue("energyRate"), out energyReductionRate);

            float.TryParse(config.GetValue("progress"), out progress);
            float.TryParse(config.GetValue("sleepTime"), out sleepTime);

            string stateString = config.GetValue("state");
            if (stateString != null) { 
                state = (HybernationState)Enum.Parse(typeof(HybernationState), stateString);
            }
        }

        public virtual void Save(ConfigNode config)
        {
            base.Save(config);
            config.AddValue("timeToBoot", Mathf.Round(timeToBoot / TIME_ONE_HOUR));
            config.AddValue("energyRate", energyReductionRate);

            config.AddValue("progress", progress);
            config.AddValue("sleepTime", sleepTime);

            config.AddValue("state", state.ToString());
        }
    }
}
