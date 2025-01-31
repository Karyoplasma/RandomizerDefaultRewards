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

            // reroute some methods through the delegate
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
                    Debug.Log("Before common: [" + string.Join(", ", monster.RewardsCommon.Select(m => m.GetComponent<BaseItem>().name)) + "]");
                    Debug.Log("Before rare: [" + string.Join(", ", monster.RewardsRare.Select(m => m.GetComponent<BaseItem>().name)) + "]");
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
                        raresBackupTemp.Add(gameObject);
                        if (gameObject.GetComponent<BaseItem>() is Egg)
                        {
                            // evolved monsters can drop their special eggs
                            if (monster.EvolutionEgg != null)
                            {
                                raresTemp.Add(monster.EvolutionEgg);
                            } else
                            {
                                raresTemp.Add(gameObject);
                            }
                            
                        }
                        if (gameObject.GetComponent<BaseItem>() is Catalyst)
                        {
                            raresTemp.Add(gameObject);
                        }
                    }
                    commonsBackup.Add(commonsBackupTemp);
                    raresBackup.Add(raresBackupTemp);
                    // put the rewards of the original monster (which you would fight without randomizer) in the lists. ignore eggs and
                    // catalysts in the list for rare rewards as we want to get eggs/catalysts or the monster we are actually fighting
                    foreach (GameObject gameObject in GameModeManager.Instance.GetReverseReplacement(monster).RewardsCommon)
                    {
                        commonsTemp.Add(gameObject);
                    }
                    foreach (GameObject gameObject in GameModeManager.Instance.GetReverseReplacement(monster).RewardsRare)
                    {
                        if (!(gameObject.GetComponent<BaseItem>() is Egg) && !(gameObject.GetComponent<BaseItem>() is Catalyst))
                        {
                            raresTemp.Add(gameObject);
                        }
                    }
                    // bind the modified lists to the monster before determining rewards
                    monster.RewardsRare = raresTemp;
                    monster.RewardsCommon = commonsTemp;
                    Debug.Log("After common: [" + string.Join(", ", monster.RewardsCommon.Select(m => m.GetComponent<BaseItem>().name)) + "]");
                    Debug.Log("After rare: [" + string.Join(", ", monster.RewardsRare.Select(m => m.GetComponent<BaseItem>().name)) + "]");

                }
                orig(self);
                // restore the lists after rewards have been selected or else stuff breaks
                int i = 0;
                foreach (Monster monster in self.Enemies)
                {
                    Debug.Log("Restore before common: [" + string.Join(", ", monster.RewardsCommon.Select(m => m.GetComponent<BaseItem>().name)) + "]");
                    Debug.Log("Restore before rare: [" + string.Join(", ", monster.RewardsRare.Select(m => m.GetComponent<BaseItem>().name)) + "]");
                    monster.RewardsRare = raresBackup[i];
                    monster.RewardsCommon = commonsBackup[i++];
                    Debug.Log("Restore after common: [" + string.Join(", ", monster.RewardsCommon.Select(m => m.GetComponent<BaseItem>().name)) + "]");
                    Debug.Log("Restore after rare: [" + string.Join(", ", monster.RewardsRare.Select(m => m.GetComponent<BaseItem>().name)) + "]");
                }
            } else
            {
                orig(self);
            }
            
        }

    }
}
