#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ImpactDrive.Runtime
{
    /// <summary>Bounded, checksummed UTF-8 payload with a previous-good backup.</summary>
    public static class AtomicSaveFile
    {
        private const string Header = "IMPACTDRIVE_SAVE_V1";
        private const int MaximumBytes = 131072;

        public static string? Read(string path, out bool recoveredBackup)
        {
            recoveredBackup = false;
            string? payload = ReadOne(path);
            if (payload != null) return payload;
            payload = ReadOne(path + ".bak");
            recoveredBackup = payload != null;
            return payload;
        }

        private static string? ReadOne(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > MaximumBytes) return null;
                string text = File.ReadAllText(path, Encoding.UTF8);
                int first = text.IndexOf('\n');
                int second = first < 0 ? -1 : text.IndexOf('\n', first + 1);
                if (first < 0 || second < 0 || text.Substring(0, first) != Header) return null;
                string checksum = text.Substring(first + 1, second - first - 1);
                string payload = text.Substring(second + 1);
                return string.Equals(checksum, Hash(payload), StringComparison.Ordinal) ? payload : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        public static void Write(string path, string payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            byte[] bytes = new UTF8Encoding(false).GetBytes(Header + "\n" + Hash(payload) + "\n" + payload);
            if (bytes.Length > MaximumBytes) throw new ArgumentException("Save payload is too large.", nameof(payload));
            string? parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (!File.Exists(path))
            {
                File.Move(temporary, path);
                return;
            }
            // Keep a good backup when the primary was damaged by a previous interruption.
            if (ReadOne(path) == null)
            {
                File.Delete(path);
                File.Move(temporary, path);
                return;
            }
            try { File.Replace(temporary, path, path + ".bak"); }
            catch (PlatformNotSupportedException) { ReplaceWithRecovery(temporary, path); }
        }

        private static void ReplaceWithRecovery(string temporary, string path)
        {
            // On filesystems without replace, interruption between moves remains recoverable.
            string backup = path + ".bak";
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(path, backup);
            File.Move(temporary, path);
        }

        private static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
                var text = new StringBuilder(bytes.Length * 2);
                foreach (byte item in bytes) text.Append(item.ToString("x2"));
                return text.ToString();
            }
        }
    }
}
