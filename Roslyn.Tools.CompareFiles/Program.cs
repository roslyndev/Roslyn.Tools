using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        //Compare -s --origin D:\Projects\Origin --target D:\Projects\Target --output console

        string originFolder = string.Empty;
        string targetFolder = string.Empty;
        bool subfolders = false;
        string outputOption = string.Empty;

        if (true)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: filecompare -s --origin <origin_folder> --target <target_folder> --output <console/file>");
                return;
            }

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-s")
                {
                    subfolders = true;
                }
                else if (args[i] == "--origin" && i < args.Length - 1)
                {
                    originFolder = args[i + 1];
                    i++;
                }
                else if (args[i] == "--target" && i < args.Length - 1)
                {
                    targetFolder = args[i + 1];
                    i++;
                }
                else if (args[i] == "--output" && i < args.Length - 1)
                {
                    outputOption = args[i + 1];
                    i++;
                }
            }
        }
        else
        {
            originFolder = "D:\\Projects\\iCloudHospital\\CloudHospital.IdentityServerAdmin\\src\\CloudHospital.IdentityServer.Admin.Api";
            targetFolder = "D:\\Projects\\iCloudHospital\\Net8Identity\\src\\Net8Identity.Admin.Api";
            subfolders = true;
            outputOption = "console";
        }

        if (string.IsNullOrEmpty(originFolder) || string.IsNullOrEmpty(targetFolder))
        {
            Console.WriteLine("Origin and target folders must be specified.");
            return;
        }

        List<string> differences = CompareFolders(originFolder, targetFolder, subfolders);

        if (outputOption.Equals("console", StringComparison.OrdinalIgnoreCase))
        {
            if (differences != null && differences.Count > 0)
            {
                int num = 1;
                foreach (var diff in differences)
                {
                    Console.WriteLine($"{num++}) {diff}");
                }
            }
            else
            {
                Console.WriteLine("Differences is empty.");
            }
        }
        else if (outputOption.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllLines("compare.txt", differences);
            Console.WriteLine("Differences written to compare.txt");
        }
        else
        {
            Console.WriteLine("Invalid output option. Use 'console' or 'file'.");
        }
    }

    static List<string> CompareFolders(string originFolder, string targetFolder, bool includeSubfolders)
    {
        List<string> differences = new List<string>();

        var originFiles = GetFiles(originFolder, includeSubfolders);
        var targetFiles = GetFiles(targetFolder, includeSubfolders);

        foreach (var originFile in originFiles)
        {
            string relativePath = originFile.Substring(originFolder.Length);

            if (targetFiles.Contains(targetFolder + relativePath))
            {
                if (!FileEquals(originFile, targetFolder + relativePath))
                {
                    differences.Add($"File '{relativePath}' is different.");
                }
            }
            else
            {
                differences.Add($"File '{relativePath}' exists in origin but not in target.");
            }
        }

        foreach (var targetFile in targetFiles)
        {
            string relativePath = targetFile.Substring(targetFolder.Length);

            if (!originFiles.Contains(originFolder + relativePath))
            {
                differences.Add($"File '{relativePath}' exists in target but not in origin.");
            }
        }

        return differences;
    }

    static List<string> GetFiles(string folder, bool includeSubfolders)
    {
        var allowedExtensions = new[] { ".cs", ".cshtml" };
        var files = Directory.GetFiles(folder, "*.*", includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(file => allowedExtensions.Any(file.ToLower().EndsWith));
        return new List<string>(files);
    }

    static bool FileEquals(string file1, string file2)
    {
        return File.ReadAllText(file1).Equals(File.ReadAllText(file2), StringComparison.OrdinalIgnoreCase);
    }
}
