using System;
using System.IO;
using System.Text;

namespace Jaosnake.CeniReload.Storage
{
    internal static class AtomicCheckpointFile
    {
        public static void Write(string destination, string json, string backupDestination = null)
        {
            // Keep the temporary file on the same volume for atomic rename/replace.
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] bytes = new UTF8Encoding(false, true).GetBytes(json);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(destination))
                    File.Replace(temporary, destination, backupDestination ?? destination + ".bak");
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
