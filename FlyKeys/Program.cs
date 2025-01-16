using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FlyKeys
{
    internal static class Program
    {
        internal static void Main()
        {
            try
            {
                var path = GetPath();
                var entries = ReadEntries(path);
                Console.WriteLine($"共读取{entries.Length}个词条。查找遗漏的飞键中...");
                var omitted = FindOmitted(entries);
                if (omitted.Length == 0)
                    Console.WriteLine("没有找到任何遗漏的飞键。");
                else
                {
                    Console.WriteLine($"共找到{omitted.Length}个遗漏的飞键。输出中...");
                    var outputPath = DistinctName(path);
                    if (!WriteFile(outputPath, omitted)) WriteScreen(omitted);
                }
            }
            catch (Exception ex) { Console.WriteLine($"程序异常：{ex.Message}，已中止。"); }
            finally { _ = Console.ReadKey(); }
        }

        private static string GetPath()
        {
            Console.WriteLine("输入rime词库文件路径：");
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

        private static string[] FindOmitted((string word, string code)[] entries)
        {
            var omitted = new ConcurrentBag<string>();
            var checkedWords = new ConcurrentBag<string>();
            var flyKeys = new Dictionary<string, string>();

            AddFlyKey("fh", "qh");
            AddFlyKey("fz", "qz");
            AddFlyKey("fe", "qe");
            AddFlyKey("jz", "wz");
            AddFlyKey("je", "we");
            AddFlyKey("gx", "gm");
            AddFlyKey("kx", "km");
            AddFlyKey("hx", "hm");
            AddFlyKey("fx", "fm");
            AddFlyKey("wx", "wm");
            AddFlyKey("ex", "em");

            Parallel.ForEach(
                entries, entry =>
                {
                    if (checkedWords.Contains(entry.word)) return;
                    checkedWords.Add(entry.word);
                    if (flyKeys.TryGetValue(entry.code.Substring(0, 2), out var value)
                     && !entries.Any(e => e.word == entry.word && e.code.StartsWith(value)))
                        omitted.Add($"{entry.word}\t{entry.code}\t{value}");
                });
            return omitted.ToArray();

            void AddFlyKey(string code1, string code2)
            {
                flyKeys.Add(code1, code2);
                flyKeys.Add(code2, code1);
            }
        }

        private static string DistinctName(string path)
        {
            var dir = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("无法获取上级目录");
            var name = Path.GetFileNameWithoutExtension(path);
            var outputPath = Path.Combine(dir, $"{name}_空码.txt");
            for (var i = 2; File.Exists(outputPath); i++) outputPath = Path.Combine(dir, $"{name}_空码_{i}.txt");
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
            Console.WriteLine("词组\t编码\t遗漏的飞键");
            foreach (var vacancy in vacancies) Console.WriteLine(vacancy);
        }
    }
}