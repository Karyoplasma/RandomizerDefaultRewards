using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RandomizerDefaultRewards
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class RandomizerDefaultRewards : BaseUnityPlugin
    {
        public const string ModGUID = "karyoplasma.RandomizerDefaultRewards";
        public const string ModName = "RandomizerDefaultRewards";
        public const string ModVersion = "0.0.1";
        public ConfigEntry<bool> Enabled;

        public RandomizerDefaultRewards()
        {
            // have a nice on/off toggle for better mod management
            Enabled = Config.Bind(
                "General",
                "Enabled",
                true,
                "Mod enabled?"
            );

            // reroute the reward granting method through the delegate
            On.CombatController.GrantReward += CombatController_GrantReward;

        }
        
        private void CombatController_GrantReward(On.CombatController.orig_GrantReward orig, CombatController self)
        {
            if (Enabled.Value && GameModeManager.Instance.RandomizerMode)
            {
                // backup lists for restoring
                List<List<GameObject>> commonsBackup = new List<List<GameObject>>();
                List<List<GameObject>> raresBackup = new List<List<GameObject>>();
                // go through the enemies
                foreach (Monster monster in self.Enemies)
                {
                    LogRewardLists("Initial", monster);
                    List<GameObject> commonsTemp = new List<GameObject>();
                    List<GameObject> raresTemp = new List<GameObject>();
                    List<GameObject> commonsBackupTemp = new List<GameObject>();
                    List<GameObject> raresBackupTemp = new List<GameObject>();
                    // backup commons (no eggs in commons)
                    foreach (GameObject gameObject in monster.RewardsCommon)
                    {
                        commonsBackupTemp.Add(gameObject);
                    }
                    // backup rares, add catalysts and eggs to the modified reward list
                    foreach (GameObject gameObject in monster.RewardsRare)
                    {
                        BaseItem baseItem = gameObject.GetComponent<BaseItem>();
                        raresBackupTemp.Add(gameObject);
                        // keep eggs
                        if (baseItem is Egg)
                        {
                            // if the monster has an evolution egg, add that instead
                            raresTemp.Add(monster.EvolutionEgg ?? gameObject);
                        }
                        else if (baseItem is Catalyst)
                        {
                            // keep catalysts
                            raresTemp.Add(gameObject);
                        }
                    }
                    commonsBackup.Add(commonsBackupTemp);
                    raresBackup.Add(raresBackupTemp);
                    // put the rewards of the original monster (which you would fight without randomizer) in the lists. ignore eggs and
                    // catalysts in the list for rare rewards as we want to get eggs/catalysts or the monster we are actually fighting
                    Monster reverseReplacementMonster = GameModeManager.Instance.GetReverseReplacement(monster);
                    foreach (GameObject gameObject in reverseReplacementMonster.RewardsCommon)
                    {
                        commonsTemp.Add(gameObject);
                    }
                    foreach (GameObject gameObject in reverseReplacementMonster.RewardsRare)
                    {
                        // ignore eggs and catalysts as those are from a different monster
                        BaseItem baseItem = gameObject.GetComponent<BaseItem>();
                        if (!(baseItem is Egg) && !(baseItem is Catalyst))
                        {
                            raresTemp.Add(gameObject);
                        }
                    }
                    // bind the modified lists to the monster before determining rewards
                    monster.RewardsRare = raresTemp;
                    monster.RewardsCommon = commonsTemp;
                    LogRewardLists("Changed", monster);

                }
                orig(self);
                // restore the lists after rewards have been selected or else stuff breaks
                int i = 0;
                foreach (Monster monster in self.Enemies)
                {
                    monster.RewardsRare = raresBackup[i];
                    monster.RewardsCommon = commonsBackup[i++];
                    LogRewardLists("Restored", monster);
                }
            } else
            {
                orig(self);
            }
            
        }

        private void LogRewardLists(string prefix, Monster monster)
        {
            Debug.Log(prefix + " common: [" + string.Join(", ", monster.RewardsCommon.Select(m => m.GetComponent<BaseItem>().name)) + "]");
            Debug.Log(prefix + " rare: [" + string.Join(", ", monster.RewardsRare.Select(m => m.GetComponent<BaseItem>().name)) + "]");
        }

    }
}
