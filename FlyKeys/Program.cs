using System;
using System.IO;
using System.Linq;

namespace FlyKeys
{
    internal static class Program
    {
        private static readonly string[] FlyCodes =
        {
            "fh",
            "qh",
            "fz",
            "qz",
            "fe",
            "qe",
            "jz",
            "wz",
            "je",
            "we",
            "gx",
            "gm",
            "kx",
            "km",
            "hx",
            "hm",
            "fx",
            "fm",
            "wx",
            "wm",
            "ex",
            "em"
        };

        internal static void Main()
        {
            try
            {
                var dictPath = GetPath("Rime键道6词库");
                var entries = ReadEntries(dictPath);
                Console.WriteLine($"共读取{entries.Length}个词条。");

                var danPath = GetPath("Rime键道6单字词库");
                var flyChars = ReadDans(danPath);
                Console.WriteLine($"共读取{flyChars.Length}个需要飞键的单字。查找遗漏的飞键中...");

                var omitted = FindOmitted(entries, flyChars);
                if (omitted.Length == 0)
                    Console.WriteLine("没有找到任何遗漏的飞键。");
                else
                {
                    Console.WriteLine($"共找到{omitted.Length}个遗漏的飞键。输出中...");
                    var outputPath = DistinctName(dictPath);
                    if (!WriteFile(outputPath, omitted)) WriteScreen(omitted);
                }
            }
            catch (Exception ex) { Console.WriteLine($"程序异常：{ex.Message}，已中止。"); }
            finally { _ = Console.ReadKey(); }
        }

        private static string GetPath(string prompt)
        {
            Console.WriteLine($"输入{prompt}文件路径：");
            var path = Console.ReadLine();
            while (!File.Exists(path))
            {
                Console.WriteLine("路径无效，请重新输入！");
                path = Console.ReadLine();
            }
            return path;
        }

        private static (string, string)[] ReadEntries(string path) =>
            File.ReadAllLines(path)
                .AsParallel()
                .Where(
                    line => (line.Contains("#") && line.Substring(0, line.IndexOf('#')).Split('\t').Length >= 2)
                         || (!line.Contains("#") && line.Split('\t').Length >= 2))
                .Select(
                    line =>
                    {
                        var parts = line.Contains("#")
                            ? line.Substring(0, line.IndexOf('#')).Split('\t')
                            : line.Split('\t');
                        return (parts[0], parts[1]);
                    })
                .ToArray();

        private static string[] ReadDans(string path) =>
            File.ReadAllLines(path)
                .AsParallel()
                .Where(
                    line => (line.Contains("#")
                          && line.Substring(0, line.IndexOf('#')).Split('\t').Length >= 2
                          && line.Substring(0, line.IndexOf('#')).Split('\t')[1].Length >= 2
                          && FlyCodes.Contains(line.Substring(0, line.IndexOf('#')).Split('\t')[1].Substring(0, 2)))
                         || (!line.Contains("#")
                          && line.Split('\t').Length >= 2
                          && line.Split('\t')[1].Length >= 2
                          && FlyCodes.Contains(line.Split('\t')[1].Substring(0, 2))))
                .Select(
                    line =>
                    {
                        var parts = line.Contains("#")
                            ? line.Substring(0, line.IndexOf('#')).Split('\t')
                            : line.Split('\t');
                        return parts[0];
                    })
                .Distinct()
                .ToArray();

        private static string[] FindOmitted((string word, string code)[] entries, string[] flyChars) =>
            entries.AsParallel()
                .Where(
                    entry => entry.word.Any(c => flyChars.Contains(c.ToString()))
                          && entries.Count(e => e.word == entry.word) == 1)
                .Select(entry => $"{entry.word}\t{entry.code}")
                .OrderBy(s => s)
                .ToArray();

        private static string DistinctName(string path)
        {
            var dir = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("无法获取上级目录");
            var name = Path.GetFileNameWithoutExtension(path);
            var outputPath = Path.Combine(dir, $"{name}_遗漏飞键.txt");
            for (var i = 2; File.Exists(outputPath); i++) outputPath = Path.Combine(dir, $"{name}_遗漏飞键_{i}.txt");
            return outputPath;
        }

        private static bool WriteFile(string path, string[] omitted)
        {
            try
            {
                File.WriteAllLines(path, omitted);
                Console.WriteLine($"遗漏的飞键已输出至{path}。");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法将遗漏的飞键输出为文件{path}，因为：{ex.Message}");
                Console.WriteLine("将直接在此列出：");
                return false;
            }
        }

        private static void WriteScreen(string[] vacancies)
        {
            Console.WriteLine("词组\t编码");
            foreach (var vacancy in vacancies) Console.WriteLine(vacancy);
        }
    }
}