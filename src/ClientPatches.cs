using HarmonyLib;
using UnityEngine;

namespace LessSleep
{
    internal static class ClientPatches
    {
        /// <summary>Intercept plain chat text ("yes" / "no") while a vote is open.</summary>
        [HarmonyPatch(typeof(Chat), nameof(Chat.SendText))]
        internal static class ChatSendTextPatch
        {
            private static bool Prefix(string text)
            {
                return !ClientState.TryChatVote(text);
            }
        }

        /// <summary>
        /// Intercept chat input starting with a slash: the chat input strips the leading '/'
        /// and calls TryRunCommand, so "/yes" arrives here as "yes".
        /// </summary>
        [HarmonyPatch(typeof(Terminal), "TryRunCommand")]
        internal static class TerminalTryRunCommandPatch
        {
            private static bool Prefix(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return true;
                }

                string trimmed = text.Trim();
                if (trimmed.StartsWith("/"))
                {
                    trimmed = trimmed.Substring(1);
                }

                if (trimmed.IndexOf(' ') >= 0)
                {
                    return true;
                }

                return !ClientState.TryChatVote(trimmed);
            }
        }
    }

    internal static class ClientState
    {
        private static bool voteOpen;
        private static float helloNext;

        public static void Tick()
        {
            if (Player.m_localPlayer == null || ZNet.instance == null)
            {
                return;
            }

            // Let the server know this client supports voting (sent periodically, deduped server-side).
            if (Time.time >= helloNext)
            {
                helloNext = Time.time + 30f;
                Rpc.ToServer(Net.Hello, LessSleepPlugin.Version);
            }

            if (!voteOpen || !LessSleepPlugin.EnableVotePrompt.Value)
            {
                return;
            }

            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                CastVote(true);
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                CastVote(false);
            }
        }

        public static bool TryChatVote(string text)
        {
            if (!voteOpen || !LessSleepPlugin.AllowChatYesNo.Value)
            {
                return false;
            }

            string trimmed = (text ?? string.Empty).Trim().ToLowerInvariant();
            if (trimmed == "yes" || trimmed == "y")
            {
                CastVote(true);
                return true;
            }

            if (trimmed == "no" || trimmed == "n")
            {
                CastVote(false);
                return true;
            }

            return false;
        }

        public static void OnVoteStart(string requester, float timeout, int yesNeeded, int awakeCount)
        {
            voteOpen = true;
        }

        public static void OnVoteUpdate(int yesVotes, int yesNeeded)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.TopLeft,
                    $"Sleep vote: {yesVotes}/{yesNeeded}",
                    0,
                    null,
                    false,
                    false);
            }
        }

        public static void OnVoteResult(bool passed, string reason)
        {
            voteOpen = false;
        }

        private static void CastVote(bool accept)
        {
            Rpc.ToServer(Net.Vote, accept);

            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.TopLeft,
                    accept ? "Sleep vote: accepted" : "Sleep vote: declined",
                    0,
                    null,
                    false,
                    false);
            }
        }
    }
}
