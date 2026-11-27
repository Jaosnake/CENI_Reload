using System.Collections.Generic;
using Jaosnake.CeniReload.Localization;
using RoR2;

namespace Jaosnake.CeniReload.Core
{
    public sealed class RestoreOutcome
    {
        public int PlayersRestored;
        public int AlliesRestored;
        public int FreshPlayers;
        public bool FatalError;
        public readonly CheckpointValidation Details = new CheckpointValidation();
        public bool HasWarnings => Details.Issues.Count != 0;
        public string StatusKey => FatalError || PlayersRestored + FreshPlayers == 0 ? "CENIRELOAD_RESULT_FAILED" : HasWarnings ? "CENIRELOAD_RESULT_PARTIAL" : "CENIRELOAD_RESULT_OK";
        public void Warn(string key, string detail = "") => Details.Add(key, detail);
        public void Publish()
        {
            string summary = CeniReloadStrings.Format(StatusKey, PlayersRestored, AlliesRestored);
            // Restoration failures are always visible, even with routine save messages disabled.
            Chat.AddMessage("[CENI RELOAD] " + summary);
            CeniReloadPlugin.Log?.LogInfo(summary);
            foreach (var issue in Details.Issues)
            {
                Chat.AddMessage("[CENI RELOAD] " + issue);
                CeniReloadPlugin.Log?.LogWarning(issue.ToString());
            }
        }
    }
}
