using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
    public class AIAgentData : IExposable
    {
        private List<Thing> _targetedBy = new List<Thing>(4);
        /* Fields
         * ---------------------------------------------------------   
         */
        
        #region Fields

        private List<Pair<Thing, int>> targetedBy;
        private AIEnvThings    enemies;
        private AIEnvThings    allies;

        #endregion
        
        /* 
         * ---------------------------------------------------------   
         */
        
        public AIAgentData()
        {
            enemies    = new AIEnvThings();
            allies     = new AIEnvThings();
            targetedBy = new List<Pair<Thing, int>>();
        }
        
        /* Timestamps
         * ---------------------------------------------------------   
         */
        
        #region Timestamps

        public int LastSawEnemies
        {
            get;
            set;
        }
        public int LastTookDamage
        {
            get;
            set;
        }
        public int lastRetreated
        {
            get;
            set;
        }
        public int LastInterrupted
        {
            get;
            set;
        }
        public int LastScanned
        {
            get;
            set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RetreatedRecently(int ticks)
        {
            return GenTicks.TicksGame - lastRetreated <= ticks;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TookDamageRecently(int ticks)
        {
            return GenTicks.TicksGame - LastTookDamage <= ticks;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InterruptedRecently(int ticks)
        {
            return GenTicks.TicksGame - LastInterrupted <= ticks;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ScannedRecently(int ticks)
        {
            return GenTicks.TicksGame - LastScanned <= ticks;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SawEnemiesRecently(int ticks)
        {
            return GenTicks.TicksGame - LastSawEnemies <= ticks;
        }

        #endregion
        
        /* Environment
         * ---------------------------------------------------------   
         */
        
        #region Spotting

        public List<Thing> BeingTargetedBy
        {
            get
            {
                bool cleanUp = false;
                _targetedBy.Clear();
                for (int i = 0; i < targetedBy.Count; i++)
                {
                    Pair<Thing, int> pair = targetedBy[i];
                    if (GenTicks.TicksGame - pair.second > 240)
                    {
                        cleanUp = true;
                        continue;
                    }
                    _targetedBy.Add(pair.First);
                }
                if (cleanUp)
                {
                    targetedBy.RemoveAll(t => GenTicks.TicksGame - t.Second > 240);
                }
                return _targetedBy;
            }
        }
        public AIEnvThings AllEnemies
        {
            get => enemies.AsReadonly;
        }
        public int NumEnemies
        {
            get;
            set;
        }
        public IEnumerator<AIEnvAgentInfo> EnemiesVisible()
        {
            return enemies.GetEnumerator(AIEnvAgentState.visible);
        }
        public IEnumerator<AIEnvAgentInfo> EnemiesNearBy()
        {
            return enemies.GetEnumerator(AIEnvAgentState.visible);
        }
        public IEnumerator<AIEnvAgentInfo> Enemies()
        {
            return enemies.GetEnumerator(AIEnvAgentState.unknown);
        }
        public IEnumerator<AIEnvAgentInfo> MeleeEnemiesNearBy()
        {
            return enemies.GetEnumerator(AIEnvAgentState.melee & AIEnvAgentState.melee);
        }
        public IEnumerator<AIEnvAgentInfo> EnemiesWhere(AIEnvAgentState customState)
        {
            return enemies.GetEnumerator(customState);
        }
        public void ReSetEnemies(HashSet<AIEnvAgentInfo> items)
        {
            enemies.ClearAndAddRange(items);
            NumEnemies = enemies.Count;
        }
        public void ReSetEnemies(Dictionary<Thing,AIEnvAgentInfo> dict)
        {
            enemies.ClearAndAddRange(dict);
            NumEnemies = enemies.Count;
        }
        public void ReSetEnemies()
        {
            enemies.Clear();
            NumEnemies = 0;
        }
        
        public AIEnvThings AllAllies
        {
            get => allies.AsReadonly;
        }
        public int NumAllies
        {
            get;
            set;
        }
        public IEnumerator<AIEnvAgentInfo> AlliesNearBy()
        {
            return allies.GetEnumerator(AIEnvAgentState.nearby);
        }
        public IEnumerator<AIEnvAgentInfo> Allies()
        {
            return allies.GetEnumerator(AIEnvAgentState.unknown);
        }
        public IEnumerator<AIEnvAgentInfo> AlliesWhere(AIEnvAgentState customState)
        {
            return allies.GetEnumerator(customState);
        }
        public void ReSetAllies(HashSet<AIEnvAgentInfo> items)
        {
            allies.ClearAndAddRange(items);
            NumAllies = allies.Count;
        }
        public void ReSetAllies(Dictionary<Thing,AIEnvAgentInfo> dict)
        {
            allies.ClearAndAddRange(dict);
            NumAllies = allies.Count;
        }
        public void ReSetAllies()
        {
            allies.Clear();
            NumAllies = 0;
        }

        public void BeingTargeted(Thing targeter)
        {
            targetedBy.RemoveAll(t => GenTicks.TicksGame - t.Second > 90 || t.First == targeter);
            targetedBy.Add(new Pair<Thing, int>(targeter, GenTicks.TicksGame));
        }

        #endregion
        
        /*
         * ---------------------------------------------------------   
         */

        public void ExposeData()
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                List<Thing> things = BeingTargetedBy;
                Scribe_Collections.Look(ref things, "targetedBy.1", LookMode.Reference);
            }
            Scribe_Deep.Look(ref enemies, "enemies.1");            
            enemies ??= new AIEnvThings();
            Scribe_Deep.Look(ref allies, "allies.1");
            allies  ??= new AIEnvThings();
        }
    }
}
