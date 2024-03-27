using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        //Compare -s --origin D:\Projects\Origin --target D:\Projects\Target --output console -d

        string originFolder = string.Empty;
        string targetFolder = string.Empty;
        bool subfolders = false;
        string outputOption = string.Empty;
        bool isDetail = false;

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
                else if (args[i] == "-d" || args[i] == "-D")
                {
                    isDetail = true;
                }
            }
        }
        else
        {
            originFolder = "D:\\Projects\\iCloudHospital\\CloudHospital.IdentityServerAdmin\\src\\CloudHospital.IdentityServer.Admin.Api";
            targetFolder = "D:\\Projects\\iCloudHospital\\Net8Identity\\src\\Net8Identity.Admin.Api";
            subfolders = true;
            outputOption = "console";
            isDetail = true;
        }

        if (string.IsNullOrEmpty(originFolder) || string.IsNullOrEmpty(targetFolder))
        {
            Console.WriteLine("Origin and target folders must be specified.");
            return;
        }

        List<CompareItem> differences = CompareFolders(originFolder, targetFolder, subfolders);

        if (outputOption.Equals("console", StringComparison.OrdinalIgnoreCase))
        {
            if (differences != null && differences.Count > 0)
            {
                int num = 1;
                foreach (var diff in differences)
                {
                    Console.WriteLine($"{num++}) {diff.file}");
                    if (isDetail && diff.rows != null && diff.rows.Count > 0)
                    {
                        foreach(string row in diff.rows)
                        {
                            Console.WriteLine($"\t => {row}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Differences is empty.");
            }
        }
        else if (outputOption.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllLines("compare.txt", differences.ToCompareList());
            Console.WriteLine("Differences written to compare.txt");
        }
        else
        {
            Console.WriteLine("Invalid output option. Use 'console' or 'file'.");
        }
    }

    static List<CompareItem> CompareFolders(string originFolder, string targetFolder, bool includeSubfolders)
    {
        List<CompareItem> differences = new List<CompareItem>();

        var originFiles = GetFiles(originFolder, includeSubfolders);
        var targetFiles = GetFiles(targetFolder, includeSubfolders);

        foreach (var originFile in originFiles)
        {
            string relativePath = originFile.Substring(originFolder.Length);
            List<string> diff = new List<string>();

            if (targetFiles.Contains(targetFolder + relativePath))
            {
                if (!FileEquals(originFile, targetFolder + relativePath, out diff))
                {
                    differences.Add(new CompareItem($"File '{relativePath}' is different.", diff));
                }
            }
            else
            {
                differences.Add(new CompareItem($"File '{relativePath}' exists in origin but not in target.", new List<string>()));
            }
        }

        foreach (var targetFile in targetFiles)
        {
            string relativePath = targetFile.Substring(targetFolder.Length);

            if (!originFiles.Contains(originFolder + relativePath))
            {
                differences.Add(new CompareItem($"File '{relativePath}' exists in target but not in origin.", new List<string>()));
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

    static bool FileEquals(string file1, string file2, out List<string> list)
    {
        list = new List<string>();
        var txt1 = File.ReadAllText(file1);
        var txt2 = File.ReadAllText(file2);

        if (txt1.Equals(txt2, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        else
        {
            var list1 = txt1.Replace("\n", Environment.NewLine).Split(Environment.NewLine);
            var list2 = txt2.Replace("\n", Environment.NewLine).Split(Environment.NewLine);

            int end = Math.Min(list1.Count(), list2.Count());
            int chk = 0;
            string line1 = string.Empty;
            string line2 = string.Empty;
            for (int i = 0; i < end; i++)
            {
                if (!string.IsNullOrWhiteSpace(list1[i]) && !list1[i].StartsWith("//") && !list1[i].StartsWith("namespace"))
                {
                    line1 = list1[i].Trim();
                    line2 = list2[i].Trim();

                    if (!line1.Equals(line2, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add($"{i} line : {line1}");
                        chk++;
                    }
                }
            }

            return !(chk > 0);
        }
    }
}

public class CompareItem
{
    public string file { get; set; } = string.Empty;

    public List<string> rows { get; set; } = new List<string>();

    public CompareItem()
    {
        this.file = string.Empty;
        this.rows = new List<string>();
    }

    public CompareItem(string _file)
    {
        this.file = _file;
        this.rows = new List<string>();
    }

    public CompareItem(string file, List<string> rows) : this(file)
    {
        this.rows = rows;
    }
}

public static class CompareConvert
{
    public static List<string> ToCompareList(this List<CompareItem> list)
    {
        var result = new List<string>();
        foreach(var item in list)
        {
            result.Add(item.file);
            result.AddRange(item.rows);
        }

        return result;
    }
}