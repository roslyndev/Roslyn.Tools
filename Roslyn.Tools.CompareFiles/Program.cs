using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var compare = new CompareCsharp(txt1, txt2);
            if (compare.IsEquals())
            {
                return true;
            }
            else
            {
                foreach(var item in compare.FindDifferences())
                {
                    list.Add($"{item.Key} line : {item.Value}");
                }
                return false;
            }
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

public class CompareCsharp
{
    private string Origin = string.Empty;
    private string Target = string.Empty;

    public SyntaxTree OriginTree { get; set; }
    public SyntaxTree TargetTree { get; set; }

    public IEnumerable<SyntaxNode> OriginRoot { get; set; }
    public IEnumerable<SyntaxNode> TargetRoot { get; set; }

    public CompareCsharp()
    {
    }

    public CompareCsharp(string origin, string target)
    {
        this.Origin = origin;
        this.Target = target;

        if (!string.IsNullOrWhiteSpace(this.Origin) && !string.IsNullOrWhiteSpace(this.Target))
        {
            this.OriginTree = CSharpSyntaxTree.ParseText(this.Origin);
            this.TargetTree = CSharpSyntaxTree.ParseText(this.Target);

            this.OriginRoot = this.OriginTree.GetRoot().DescendantNodesAndSelf().Where(n => !(n is UsingDirectiveSyntax) && !(n is NamespaceDeclarationSyntax));
            this.TargetRoot = this.TargetTree.GetRoot().DescendantNodesAndSelf().Where(n => !(n is UsingDirectiveSyntax) && !(n is NamespaceDeclarationSyntax));
        }
    }

    public bool IsEquals()
    {
        return OriginTree.GetRoot().IsEquivalentTo(TargetTree.GetRoot());
    }

    public Dictionary<int, string> FindDifferences()
    {
        var differences = new Dictionary<int, string>();
        var originRoot = this.OriginTree.GetRoot();
        var targetRoot = this.TargetTree.GetRoot();
        var originNodes = originRoot.DescendantNodesAndSelf();
        var targetNodes = targetRoot.DescendantNodesAndSelf();
        int end = Math.Min(originNodes.Count(), targetNodes.Count());

        for (int i = 0; i < end; i++)
        {
            if (!originNodes.ElementAt(i).ToString().Equals(targetNodes.ElementAt(i).ToString(), StringComparison.OrdinalIgnoreCase) && !originNodes.ElementAt(i).IsEquivalentTo(targetNodes.ElementAt(i)))
            {
                differences.Add(i, $"Difference found: '{originNodes.ElementAt(i)}' vs '{targetNodes.ElementAt(i)}'");
            }
        }

        return differences;
    }
}