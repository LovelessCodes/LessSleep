using System.Collections.Generic;
using UnityEngine;

namespace LessSleep
{
    /// <summary>
    /// Server-side state machine for one sleep vote at a time.
    /// Sleepers implicitly agree (they are the ones asking); awake LessSleep clients vote.
    /// </summary>
    internal static class VoteManager
    {
        private static readonly HashSet<long> moddedPeers = new HashSet<long>();
        private static readonly Dictionary<long, bool> votes = new Dictionary<long, bool>();

        private static bool voting;
        private static bool passed;
        private static string requester = "A player";
        private static int yesNeeded;
        private static int awakeCount;
        private static float deadline;
        private static float cooldownUntil;

        public static bool IsVoting => voting;

        public static bool PassedNow => passed;

        public static float CooldownUntil => cooldownUntil;

        public static void RegisterModdedPeer(long peer, string version)
        {
            moddedPeers.Add(peer);
        }

        /// <summary>
        /// Returns the awake peers that run LessSleep. Caller passes the peers currently asleep
        /// so the modded set can be intersected with connected players.
        /// </summary>
        public static HashSet<long> FilterModdedAwake(HashSet<long> awakePeerIds)
        {
            PruneConnected();
            var result = new HashSet<long>();
            foreach (long peer in awakePeerIds)
            {
                if (moddedPeers.Contains(peer))
                {
                    result.Add(peer);
                }
            }

            return result;
        }

        private static void PruneConnected()
        {
            List<ZNet.PlayerInfo> players = ZNet.instance.GetPlayerList();
            if (players == null)
            {
                moddedPeers.Clear();
                return;
            }

            var connected = new HashSet<long>();
            foreach (ZNet.PlayerInfo player in players)
            {
                if (player.m_characterID != ZDOID.None)
                {
                    connected.Add(player.m_characterID.UserID);
                }
            }

            moddedPeers.IntersectWith(connected);
        }

        public static void StartVote(string requesterName, HashSet<long> moddedAwake)
        {
            requester = string.IsNullOrEmpty(requesterName) ? "A player" : requesterName;
            awakeCount = moddedAwake.Count;

            if (awakeCount == 0)
            {
                // Nobody to ask - behave like the pre-vote behaviour and just skip.
                Pass("nobody to ask");
                return;
            }

            votes.Clear();
            voting = true;
            passed = false;
            yesNeeded = Mathf.Max(1, Mathf.CeilToInt(awakeCount * (LessSleepPlugin.VotePercentOfAwakeToPass.Value / 100f)));
            deadline = Time.time + LessSleepPlugin.VoteTimeoutSeconds.Value;

            Rpc.SendToAll(Net.VoteStart, requester, LessSleepPlugin.VoteTimeoutSeconds.Value, yesNeeded, awakeCount);
            Rpc.MessageAll(
                $"{requester} wants to sleep. Press Y to accept or N to decline " +
                $"({yesNeeded} of {awakeCount} votes needed, {LessSleepPlugin.VoteTimeoutSeconds.Value:0}s). " +
                "You can also type yes or no in chat.");

            LessSleepPlugin.Log($"[LessSleep] Vote started by {requester}: {yesNeeded}/{awakeCount} needed.");
        }

        public static void CastVote(long peer, bool accept)
        {
            if (!voting || !moddedPeers.Contains(peer))
            {
                return;
            }

            if (votes.TryGetValue(peer, out bool previous) && previous == accept)
            {
                return;
            }

            votes[peer] = accept;
            BroadcastUpdate();
            CheckOutcome();
        }

        public static void Tick()
        {
            if (!voting)
            {
                return;
            }

            if (Time.time >= deadline)
            {
                Fail("timed out");
                return;
            }

            CheckOutcome();
        }

        public static void Cancel(string reason)
        {
            if (!voting)
            {
                return;
            }

            voting = false;
            passed = false;
            cooldownUntil = Time.time + LessSleepPlugin.VoteCooldownSeconds.Value;
            Rpc.SendToAll(Net.VoteResult, false, reason);
            Rpc.MessageAll($"Sleep vote cancelled: {reason}.");
            LessSleepPlugin.Log($"[LessSleep] Vote cancelled: {reason}.");
        }

        /// <summary>Called by the sleep patch right after a passed vote was honoured.</summary>
        public static void ConsumePass()
        {
            passed = false;
            cooldownUntil = Time.time + 10f;
        }

        private static void CheckOutcome()
        {
            int yes = 0;
            foreach (bool vote in votes.Values)
            {
                if (vote)
                {
                    yes++;
                }
            }

            if (yes >= yesNeeded)
            {
                Pass("vote passed");
                return;
            }

            if (votes.Count >= awakeCount)
            {
                Fail("not enough votes");
            }
        }

        private static void BroadcastUpdate()
        {
            int yes = 0;
            foreach (bool vote in votes.Values)
            {
                if (vote)
                {
                    yes++;
                }
            }

            Rpc.SendToAll(Net.VoteUpdate, yes, yesNeeded);
        }

        private static void Pass(string reason)
        {
            voting = false;
            passed = true;
            cooldownUntil = Time.time + 10f;
            Rpc.SendToAll(Net.VoteResult, true, reason);
            Rpc.MessageAll("Sleep vote passed - skipping to morning.");
            LessSleepPlugin.Log("[LessSleep] Vote passed.");
        }

        private static void Fail(string reason)
        {
            voting = false;
            passed = false;
            cooldownUntil = Time.time + LessSleepPlugin.VoteCooldownSeconds.Value;
            Rpc.SendToAll(Net.VoteResult, false, reason);
            Rpc.MessageAll($"Sleep vote failed: {reason}.");
            LessSleepPlugin.Log($"[LessSleep] Vote failed: {reason}.");
        }
    }
}
