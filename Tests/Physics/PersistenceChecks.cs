using ImpactDrive.Runtime;

namespace ImpactDrive.HeadlessTests;

internal static class PersistenceChecks
{
    public static void Register(TestSuite suite)
    {
        suite.Run("saves/checksummed UTF8 payload round trips exactly", () => WithFile(path =>
        {
            const string data = "{\"name\":\"Aster — 道路\",\"volume\":0.75}\n";
            AtomicSaveFile.Write(path, data);
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == data, "Unicode save payload must round trip without normalization");
            Check.True(!recovered, "healthy primary must not report recovery");
            Check.True(!File.Exists(path + ".tmp"), "successful write must consume temporary file");
        }));
        suite.Run("saves/backup rotates to immediately previous valid write", () => WithFile(path =>
        {
            AtomicSaveFile.Write(path, "first");
            AtomicSaveFile.Write(path, "second");
            AtomicSaveFile.Write(path, "third");
            Check.True(AtomicSaveFile.Read(path, out _) == "third", "latest primary payload");
            File.WriteAllText(path, "broken");
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == "second", "backup must preserve previous valid write");
            Check.True(recovered, "backup read must be reported");
        }));
        suite.Run("saves/payload checksum detects corruption despite valid envelope", () => WithFile(path =>
        {
            AtomicSaveFile.Write(path, "previous");
            AtomicSaveFile.Write(path, "current");
            string envelope = File.ReadAllText(path);
            File.WriteAllText(path, envelope[..^1] + "x");
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == "previous", "modified payload must fail its checksum");
            Check.True(recovered, "valid backup must recover corrupt primary");
        }));
        suite.Run("saves/both corrupt copies fail safely", () => WithFile(path =>
        {
            File.WriteAllText(path, "corrupt primary");
            File.WriteAllText(path + ".bak", "corrupt backup");
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == null, "corrupt saves must return no payload");
            Check.True(!recovered, "invalid backup is not a successful recovery");
        }));
        suite.Run("saves/writing after corruption preserves last good backup", () => WithFile(path =>
        {
            AtomicSaveFile.Write(path, "known-good");
            AtomicSaveFile.Write(path, "second");
            File.WriteAllText(path, "corrupt");
            AtomicSaveFile.Write(path, "recovered-session");
            Check.True(AtomicSaveFile.Read(path, out _) == "recovered-session", "new primary should become readable");
            File.WriteAllText(path, "corrupt again");
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == "known-good", "corrupt old primary must never overwrite valid backup");
            Check.True(recovered, "backup remains recoverable after another failure");
        }));
        suite.Run("saves/oversized writes reject without damaging existing data", () => WithFile(path =>
        {
            AtomicSaveFile.Write(path, "small-good");
            Check.Throws<ArgumentException>(() => AtomicSaveFile.Write(path, new string('x', 131072)), "bounded envelope includes payload size");
            Check.True(AtomicSaveFile.Read(path, out _) == "small-good", "rejected write must preserve prior data");
            Check.True(!File.Exists(path + ".tmp"), "rejected oversize write must not leave temporary data");
        }));
        suite.Run("saves/oversized on-disk primary is rejected with backup recovery", () => WithFile(path =>
        {
            AtomicSaveFile.Write(path, "first");
            AtomicSaveFile.Write(path, "second");
            File.WriteAllBytes(path, new byte[131073]);
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == "first", "oversized on-disk primary cannot bypass bounds");
            Check.True(recovered, "oversized primary recovers backup");
        }));
        suite.Run("saves/missing save returns null and empty valid payload is preserved", () => WithFile(path =>
        {
            Check.True(AtomicSaveFile.Read(path, out bool missingRecovered) == null && !missingRecovered, "first launch must not invent a save");
            AtomicSaveFile.Write(path, "");
            Check.True(AtomicSaveFile.Read(path, out bool recovered) == "" && !recovered, "empty valid payload differs from unreadable save");
        }));
    }

    private static void WithFile(Action<string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "impactdrive-save-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { check(Path.Combine(directory, "settings.save")); }
        finally { Directory.Delete(directory, true); }
    }
}
