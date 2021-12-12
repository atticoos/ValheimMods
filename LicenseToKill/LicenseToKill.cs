using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LicenseToKill
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class LicenseToKill : BaseUnityPlugin
    {
        public const string PluginGuid = "com.atticoos.valheim.licensetokill";
        public const string PluginName = "LicenseToKill";
        public const string PluginVersion = "1.0.0";

        static ManualLogSource _logger;
        Harmony _harmony;
        static ConfigEntry<bool> isModEnabled;
        static CircularQueue<HitData> recentHitQueue = new CircularQueue<HitData>(3);

        public void Awake()
        {
            isModEnabled = Config.Bind<bool>("_Global", "isModEnabled", true, "Enable or disable this mod.");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGuid);
            _logger = Logger;
        }

        public void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        // Track the most recent damage dealt to the player.
        // This will be used to analyze what caused the player's death (eg: another player?)
        [HarmonyPatch(typeof(Character))]
        class CharacterPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Character.ApplyDamage))]
            static void ApplyDamagePrefix(Character __instance, HitData hit)
            {
                if (isModEnabled.Value && __instance == Player.m_localPlayer)
                {
                    recentHitQueue.Enqueue(hit);
                }
            }
        }

        // When a player dies from PVP, do not drain skills.
        [HarmonyPatch(typeof(Skills))]
        class SkillsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Skills.OnDeath))]
            static bool OnDeathPrefix()
            {
                if (isModEnabled.Value && isPvpDeath())
                {
                    _logger.LogInfo("Player died due to PvP, skipping skill drain.");
                    return false;
                }
                return true;
            }
        }

        // When a player dies to PVP damage, don't gain the "No Skill Drain" buff.
        [HarmonyPatch(typeof(Player))]
        class PlayerPatch
        {           
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.OnDeath))]
            static void OnDeathPostfix(Player __instance)
            {
                if (isModEnabled.Value && __instance == Player.m_localPlayer && isPvpDeath())
                {
                    // Warning: LicenseToSkill extends `m_hardDeathCooldown` inside the `HardDeath` evaluation.
                    // Cannot invoke `ClearHardDeath`, as that resets `m_timeSinceDeath` to a value less than LTS's cooldown override.
                    __instance.m_timeSinceDeath = 999999f;
                    recentHitQueue.Clear();
                }
            }
        }

        // Determine, based on the most recent damage dealt to the player,
        // if the damage was dealt from another player (PVP)
        static bool isPvpDeath()
        {
            // Track the most recent `n` hits received.
            // In the case of taking damage from a Fire Arrow, the player will receive:
            // 1. Player damage (arrow)
            // 2. Effect damage (fire)
            // In the case of Effect damage, this cannot be traced back to a player.
            // Scan the list to determine if a player had recently inflicted damage on the current player.
            foreach (HitData hit in recentHitQueue)
            {
                Character attacker = hit.GetAttacker();
                if (attacker && attacker.IsPlayer())
                {
                    return true;
                }
            }
            return false;
        }
    }
}