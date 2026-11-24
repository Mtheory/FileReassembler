// FileReassembler: joins split parts (part_1 ... part_N) back into the original file
// and checks the result against a known SHA-1 hash.

using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FileReassembler;

internal static class Program
{
    private const string ExpectedSha1 = "bd32dd329aef54c6e672089c5e301baf4b4600ed";
    private const string OutputFileName = "reassembled.png";

    // Matches the number at the END of a file name: "part_12" -> "12".
    private static readonly Regex TrailingNumber = new(@"(\d+)$", RegexOptions.Compiled);

    private static int Main(string[] args)
    {
        try
        {
            // Setup: find the "parts" folder. You can pass a folder as the first argument;
            // otherwise we search upwards from where the program runs, because Rider runs it
            // from bin/Debug/net10.0, not the project root.
            string partsDir = args.Length > 0 ? Path.GetFullPath(args[0]) : FindFolderUpwards("parts");
            string outputDir = Path.Combine(Path.GetDirectoryName(partsDir)!, "output");
            string outputPath = Path.Combine(outputDir, OutputFileName);

            Console.WriteLine($"Parts folder:   {partsDir}");
            Console.WriteLine($"Output file:    {outputPath}");
            Console.WriteLine();

            // Step 1: check the folder exists and list the files.
            string[] files = ListPartFiles(partsDir);
            Console.WriteLine($"Step 1: found {files.Length} files.");

            // Step 2: extract the trailing number from each name and sort by it.
            List<Part> parts = ParseAndSort(files);
            Console.WriteLine($"Step 2: sorted order starts with {string.Join(", ", parts.Take(4).Select(p => p.Name))} ...");

            // Step 3: validate that the numbers are exactly 1..N.
            Validate(parts);
            Console.WriteLine($"Step 3: numbering OK (1..{parts.Count}, no gaps or duplicates).");

            // Step 4: copy every part, in order, into one output file.
            long totalBytes = Join(parts, outputPath);
            Console.WriteLine($"Step 4: wrote {totalBytes:N0} bytes.");

            // Step 5: compute the SHA-1 and compare with the expected one.
            string actualSha1 = ComputeSha1(outputPath);
            bool match = string.Equals(actualSha1, ExpectedSha1, StringComparison.OrdinalIgnoreCase);

            Console.WriteLine("Step 5: SHA-1 check");
            Console.WriteLine($"  expected: {ExpectedSha1}");
            Console.WriteLine($"  actual:   {actualSha1.ToLowerInvariant()}");
            Console.WriteLine(match ? "  MATCH - the file is identical to the original." : "  MISMATCH - the file is not correct.");

            return match ? 0 : 1;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            // Expected problems (missing folder, bad file names, ...) get a clear message, not a stack trace.
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    // One split piece: its number (the order) and its path on disk.
    private sealed record Part(int Number, string Path)
    {
        public string Name => System.IO.Path.GetFileName(Path);
    }

    private static string[] ListPartFiles(string partsDir)
    {
        if (!Directory.Exists(partsDir))
            throw new DirectoryNotFoundException($"Parts folder does not exist: {partsDir}");

        string[] files = Directory.GetFiles(partsDir);
        if (files.Length == 0)
            throw new InvalidDataException($"Parts folder is empty: {partsDir}");

        return files;
    }

    private static List<Part> ParseAndSort(string[] files)
    {
        var parts = new List<Part>();
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            Match m = TrailingNumber.Match(name);
            if (!m.Success)
                throw new InvalidDataException($"File name has no part number at the end: {name}");

            parts.Add(new Part(int.Parse(m.Groups[1].Value), file));
        }

        // Sort by the NUMBER, not the name, so part_2 comes before part_10.
        return parts.OrderBy(p => p.Number).ToList();
    }

    private static void Validate(List<Part> parts)
    {
        // After sorting, part i (0-based) must have number i + 1.
        // A duplicate or a gap both break that rule, so one loop catches both.
        for (int i = 0; i < parts.Count; i++)
        {
            int expected = i + 1;
            int actual = parts[i].Number;

            if (actual < expected)
                throw new InvalidDataException($"Duplicate part number {actual} ({parts[i].Name}).");
            if (actual > expected)
                throw new InvalidDataException($"Missing part number {expected} (next found: {parts[i].Name}).");
        }
    }

    private static long Join(List<Part> parts, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        // FileMode.Create overwrites an old output file, so re-running is safe.
        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        foreach (Part part in parts)
        {
            using var input = new FileStream(part.Path, FileMode.Open, FileAccess.Read);
            input.CopyTo(output); // copies raw bytes - no text conversion
        }

        return output.Length;
    }

    private static string ComputeSha1(string path)
    {
        using var stream = File.OpenRead(path);
        byte[] hash = SHA1.HashData(stream);
        return Convert.ToHexString(hash); // UPPERCASE hex, hence the case-insensitive compare
    }

    private static string FindFolderUpwards(string folderName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, folderName);
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find a '{folderName}' folder above {AppContext.BaseDirectory}");
    }
}
