namespace LessSleep
{
    /// <summary>
    /// Thin wrapper around ZRoutedRpc. All methods must be called from the main thread
    /// (plugin Update / RPC handlers), which is always the case here.
    /// </summary>
    internal static class Rpc
    {
        private static bool registered;

        public static void Register()
        {
            if (registered)
            {
                return;
            }

            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
            {
                return;
            }

            registered = true;

            rpc.Register<string>(Net.Hello, OnHello);
            rpc.Register<bool>(Net.Vote, OnVote);
            rpc.Register<string, float, int, int>(Net.VoteStart, OnVoteStart);
            rpc.Register<int, int>(Net.VoteUpdate, OnVoteUpdate);
            rpc.Register<bool, string>(Net.VoteResult, OnVoteResult);
        }

        /// <summary>Broadcast to every peer (0L == ZRoutedRpc.Everybody).</summary>
        public static void SendToAll(string method, params object[] args)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(0L, method, args);
        }

        /// <summary>Send to the server (routes to self when this peer is the server).</summary>
        public static void ToServer(string method, params object[] args)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(method, args);
        }

        /// <summary>Show a message on every client, including clients without the mod.</summary>
        public static void MessageAll(string text)
        {
            SendToAll(Net.VanillaShowMessage, (int)MessageHud.MessageType.TopLeft, text);
        }

        private static void OnHello(long sender, string version)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                VoteManager.RegisterModdedPeer(sender, version);
            }
        }

        private static void OnVote(long sender, bool accept)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                VoteManager.CastVote(sender, accept);
            }
        }

        private static void OnVoteStart(long sender, string requester, float timeout, int yesNeeded, int awakeCount)
        {
            ClientState.OnVoteStart(requester, timeout, yesNeeded, awakeCount);
        }

        private static void OnVoteUpdate(long sender, int yesVotes, int yesNeeded)
        {
            ClientState.OnVoteUpdate(yesVotes, yesNeeded);
        }

        private static void OnVoteResult(long sender, bool passed, string reason)
        {
            ClientState.OnVoteResult(passed, reason);
        }
    }
}
