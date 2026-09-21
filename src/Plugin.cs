using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace LessSleep
{
    [BepInPlugin(Guid, "LessSleep", Version)]
    public class LessSleepPlugin : BaseUnityPlugin
    {
        public const string Guid = "LessCx.LessSleep";
        public const string Version = "1.0.0";

        internal static ConfigEntry<int> PercentInBedToStartVote;
        internal static ConfigEntry<int> MinimumPlayersInBed;
        internal static ConfigEntry<int> VotePercentOfAwakeToPass;
        internal static ConfigEntry<float> VoteTimeoutSeconds;
        internal static ConfigEntry<float> VoteCooldownSeconds;
        internal static ConfigEntry<bool> EnableVotePrompt;
        internal static ConfigEntry<bool> AllowChatYesNo;

        private Harmony harmony;
        private static BepInEx.Logging.ManualLogSource log;

        internal static void Log(string message)
        {
            log?.LogInfo(message);
        }

        private void Awake()
        {
            log = Logger;

            PercentInBedToStartVote = Config.Bind(
                "Server",
                "PercentInBedToStartVote",
                50,
                new ConfigDescription(
                    "Percentage of online players that must be in bed before a sleep vote starts.",
                    new AcceptableValueRange<int>(1, 100)));

            MinimumPlayersInBed = Config.Bind(
                "Server",
                "MinimumPlayersInBed",
                1,
                new ConfigDescription(
                    "Minimum number of players in bed before a sleep vote can start.",
                    new AcceptableValueRange<int>(1, 100)));

            VotePercentOfAwakeToPass = Config.Bind(
                "Server",
                "VotePercentOfAwakeToPass",
                50,
                new ConfigDescription(
                    "Percentage of awake LessSleep clients that must accept for the night to be skipped.",
                    new AcceptableValueRange<int>(1, 100)));

            VoteTimeoutSeconds = Config.Bind(
                "Server",
                "VoteTimeoutSeconds",
                30f,
                new ConfigDescription(
                    "How long a sleep vote stays open before it fails.",
                    new AcceptableValueRange<float>(5f, 120f)));

            VoteCooldownSeconds = Config.Bind(
                "Server",
                "VoteCooldownSeconds",
                60f,
                new ConfigDescription(
                    "Wait time after a failed or cancelled vote before a new one can start.",
                    new AcceptableValueRange<float>(0f, 600f)));

            EnableVotePrompt = Config.Bind(
                "Client",
                "EnableVotePrompt",
                true,
                "Show sleep vote prompts and allow voting with the Y and N keys.");

            AllowChatYesNo = Config.Bind(
                "Client",
                "AllowChatYesNo",
                true,
                "While a sleep vote is open, typing yes or no in chat casts your vote.");

            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(ServerPatches));
            harmony.PatchAll(typeof(ClientPatches));
            Rpc.Register();

            Logger.LogInfo($"LessSleep {Version} loaded.");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        private void Update()
        {
            ZNet znet = ZNet.instance;
            if (znet == null)
            {
                return;
            }

            Rpc.Register();

            if (znet.IsServer())
            {
                VoteManager.Tick();
            }

            ClientState.Tick();
        }
    }
}
