using System;
using System.Collections.Generic;
using Jaosnake.CeniReload.Model;

namespace Jaosnake.CeniReload.Core
{
    // A saved identity can be claimed once for this run, regardless of join order.
    internal sealed class PlayerRestoreSession
    {
        private CheckpointSnapshot _snapshot;
        private readonly HashSet<int> _claimed = new HashSet<int>();
        public void Begin(CheckpointSnapshot snapshot) { _snapshot = snapshot; _claimed.Clear(); }
        public void Clear() { _snapshot = null; _claimed.Clear(); }
        public bool HasClaimed(int index) => _claimed.Contains(index);
        public void AppendUnclaimedTo(CheckpointSnapshot next)
        {
            if (_snapshot?.players == null) return;
            for (int i = 0; i < _snapshot.players.Count; i++)
            {
                var player = _snapshot.players[i];
                if (_claimed.Contains(i) || player == null || string.IsNullOrEmpty(player.playerId)) continue;
                if (next.players.Exists(p => p != null && string.Equals(p.playerId, player.playerId, StringComparison.OrdinalIgnoreCase))) continue;
                next.players.Add(player.ForLaterStage());
            }
        }
        public bool IsKnown(string identity) => !string.IsNullOrEmpty(identity) && _snapshot?.players != null &&
            _snapshot.players.Exists(p => p != null && string.Equals(p.playerId, identity, StringComparison.OrdinalIgnoreCase));
        public PlayerSnapshot Claim(string identity)
        {
            if (_snapshot?.players == null) return null;
            for (int i = 0; i < _snapshot.players.Count; i++)
            {
                var player = _snapshot.players[i];
                if (player == null || string.IsNullOrEmpty(identity) || string.IsNullOrEmpty(player.playerId)) continue;
                if (string.Equals(player.playerId, identity, StringComparison.OrdinalIgnoreCase))
                    return _claimed.Add(i) ? player : null;
            }
            if (_snapshot.IsMultiplayer()) return null;
            for (int i = 0; i < _snapshot.players.Count; i++)
                if (_snapshot.players[i] != null && _claimed.Add(i)) return _snapshot.players[i];
            return null;
        }
    }
}
