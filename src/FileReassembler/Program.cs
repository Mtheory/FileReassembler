// FileReassembler: joins split parts (part_1 ... part_N) back into the original file
// and checks the result against a known SHA-1 hash.

const string ExpectedSha1 = "bd32dd329aef54c6e672089c5e301baf4b4600ed";

// Setup helper (already done for you): finds the "parts" folder.
// You can pass a folder as the first argument; otherwise we search upwards from where
// the program runs, because Rider runs it from bin/Debug/net10.0, not the project root.
string partsDir = args.Length > 0 ? args[0] : FindFolderUpwards("parts");
string outputDir = Path.Combine(Path.GetDirectoryName(partsDir)!, "output");

Console.WriteLine($"Parts folder:  {partsDir}");
Console.WriteLine($"Output folder: {outputDir}");
Console.WriteLine($"Expected SHA-1: {ExpectedSha1}");

// ---------------------------------------------------------------------------
// YOUR TURN: write the logic in small steps, running the program after each one.
//
// Step 1: Check the parts folder exists. If not, print an error and stop.
//         Then list the files in it and print how many there are.
//
// Step 2: For each file, extract the number at the END of its name
//         (hint: System.Text.RegularExpressions.Regex with the pattern (\d+)$).
//         Sort the files by that number. Print the first few names to check
//         that part_2 comes after part_1, not part_10.
//
// Step 3: Validate: every file has a number, no duplicates, no gaps (1..N).
//         Stop with a clear error if any check fails.
//
// Step 4: Create the output folder, open ONE output FileStream
//         (output/reassembled.png) and CopyTo each part into it, in order.
//         Remember `using` so the files get closed.
//
// Step 5: Compute the SHA-1 of the output file (System.Security.Cryptography.SHA1),
//         convert it with Convert.ToHexString, and compare with ExpectedSha1
//         using StringComparison.OrdinalIgnoreCase. Print MATCH or MISMATCH.
// ---------------------------------------------------------------------------


static string FindFolderUpwards(string folderName)
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
