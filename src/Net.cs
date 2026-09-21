namespace LessSleep
{
    /// <summary>
    /// RPC method names shared between the server and client halves of the mod.
    /// Client to server calls use the parameterless-target overload
    /// <c>ZRoutedRpc.instance.InvokeRoutedRPC(name, args)</c> which routes to the server.
    /// Server to client calls use target <c>0L</c> (everybody).
    /// </summary>
    internal static class Net
    {
        /// <summary>Client announces it runs LessSleep (string version).</summary>
        public const string Hello = "LessSleep_Hello";

        /// <summary>Server starts a vote (string requester, float timeoutSeconds, int yesNeeded, int awakeModdedCount).</summary>
        public const string VoteStart = "LessSleep_VoteStart";

        /// <summary>Server reports current tally (int yesVotes, int yesNeeded).</summary>
        public const string VoteUpdate = "LessSleep_VoteUpdate";

        /// <summary>Server announces the outcome (bool passed, string reason).</summary>
        public const string VoteResult = "LessSleep_VoteResult";

        /// <summary>Client casts a vote (bool accept).</summary>
        public const string Vote = "LessSleep_Vote";

        /// <summary>Vanilla RPC used to display messages on all clients, including unmodded ones.</summary>
        public const string VanillaShowMessage = "ShowMessage";
    }
}
