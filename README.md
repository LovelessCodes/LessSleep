# LessSleep

Sleep voting for Valheim dedicated servers: skip the night **before** every single player is
in bed, if enough people agree.

- **Server-driven**: the server decides when to ask and when the night skips.
- **No client is required**: on a server where nobody runs the mod, it silently behaves like
  auto-skip (see *How it works*).
- **Clients get a prompt**: awake LessSleep clients see a message and can vote with `Y` / `N`
  or by typing `yes` / `no` in chat.

## How it works

1. When at least `PercentInBedToStartVote` (default 50%) of online players are in bed
   (and at least `MinimumPlayersInBed`), the server opens a vote.
2. Sleeping players are treated as a **yes** (they are the ones asking - it's their night too).
3. Awake players running LessSleep are asked to accept or decline.
   If **no** awake player runs LessSleep, the vote passes immediately (auto-skip).
4. The vote passes when at least `VotePercentOfAwakeToPass` of awake modded players accepted,
   and fails on timeout or when everyone voted no.
5. On a pass the server skips to morning exactly like vanilla sleeping does.

## Installation

### Server

Install `BepInExPack_Valheim` and drop `LessSleep.dll` into `BepInEx/plugins` on the server.

### Clients (optional, but recommended)

Same thing: install `BepInExPack_Valheim`, drop `LessSleep.dll` into `BepInEx/plugins`.
Clients without the mod still see the vote messages and the night still skips for them,
they just cannot cast a vote.

## Configuration

Server config (`BepInEx/config/LessCx.LessSleep.cfg`):

| Setting | Default | Description |
| --- | --- | --- |
| `PercentInBedToStartVote` | 50 | Percentage of online players that must be in bed to start a vote. |
| `MinimumPlayersInBed` | 1 | Minimum number of players in bed before a vote can start. |
| `VotePercentOfAwakeToPass` | 50 | Percentage of awake LessSleep clients that must accept. |
| `VoteTimeoutSeconds` | 30 | How long a vote stays open. |
| `VoteCooldownSeconds` | 60 | Wait after a failed vote before a new one can start. |

Client config:

| Setting | Default | Description |
| --- | --- | --- |
| `EnableVotePrompt` | true | Allow voting with the `Y` and `N` keys. |
| `AllowChatYesNo` | true | While a vote is open, typing `yes` or `no` in chat casts your vote. |

## Notes

- The server never needs to know which clients run the mod to *skip*; the modded-client list
  is only used to decide who gets asked.
- Voting is intentionally simple: one vote per player, last vote wins, votes are only accepted
  from connected players that announced themselves.

## Building

```bash
dotnet build src/LessSleep.csproj -c Release
./scripts/pack.sh          # produces dist/LessCx-LessSleep-<version>.zip for Thunderstore
```

Reference paths default to the macOS Steam install and can be overridden:

```bash
dotnet build src/LessSleep.csproj -c Release \
  -p:ValheimManagedDir=".../Steam/steamapps/common/Valheim/valheim.app/Contents/Resources/Data/Managed" \
  -p:BepInExCoreDir=".../Steam/steamapps/common/Valheim/BepInEx/core"
```

## License

MIT
