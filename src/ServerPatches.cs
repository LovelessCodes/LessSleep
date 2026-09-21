using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace LessSleep
{
    internal static class ServerPatches
    {
        /// <summary>
        /// Replaces the vanilla "everyone must be in bed" gate with a percentage trigger
        /// plus a community vote. Sleepers implicitly agree (they are the requesters).
        /// </summary>
        [HarmonyPatch(typeof(Game), "EverybodyIsTryingToSleep")]
        internal static class EverybodyIsTryingToSleepPatch
        {
            private static bool Prefix(ref bool __result)
            {
                ZNet znet = ZNet.instance;
                if (znet == null || !znet.IsServer())
                {
                    return true;
                }

                List<ZDO> zdos = znet.GetAllCharacterZDOS();
                if (zdos == null || zdos.Count == 0)
                {
                    return true;
                }

                int inBed = 0;
                var awakePeers = new HashSet<long>();
                foreach (ZDO zdo in zdos)
                {
                    if (zdo.GetBool(ZDOVars.s_inBed))
                    {
                        inBed++;
                    }
                    else
                    {
                        awakePeers.Add(zdo.m_uid.UserID);
                    }
                }

                int total = zdos.Count;

                // Everybody is in bed already: vanilla may skip, no vote needed.
                if (inBed >= total)
                {
                    VoteManager.Cancel("everyone is already in bed");
                    VoteManager.ConsumePass();
                    return true;
                }

                int trigger = Math.Max(
                    LessSleepPlugin.MinimumPlayersInBed.Value,
                    (int)Math.Ceiling(total * (LessSleepPlugin.PercentInBedToStartVote.Value / 100.0)));

                if (inBed >= trigger)
                {
                    if (VoteManager.IsVoting)
                    {
                        __result = false;
                        return false;
                    }

                    if (VoteManager.PassedNow)
                    {
                        VoteManager.ConsumePass();
                        __result = true;
                        return false;
                    }

                    if (Time.time < VoteManager.CooldownUntil)
                    {
                        __result = false;
                        return false;
                    }

                    VoteManager.StartVote(FindRequesterName(zdos), VoteManager.FilterModdedAwake(awakePeers));
                    __result = false;
                    return false;
                }

                if (VoteManager.IsVoting)
                {
                    VoteManager.Cancel("players left their beds");
                }

                return true;
            }

            private static string FindRequesterName(List<ZDO> zdos)
            {
                List<ZNet.PlayerInfo> players = ZNet.instance.GetPlayerList();
                if (players == null)
                {
                    return "A player";
                }

                foreach (ZDO zdo in zdos)
                {
                    if (!zdo.GetBool(ZDOVars.s_inBed))
                    {
                        continue;
                    }

                    foreach (ZNet.PlayerInfo player in players)
                    {
                        if (player.m_characterID == zdo.m_uid && !string.IsNullOrEmpty(player.m_name))
                        {
                            return player.m_name;
                        }
                    }

                    break;
                }

                return "A player";
            }
        }
    }
}
