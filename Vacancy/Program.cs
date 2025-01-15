using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Vacancy
{
    internal static class Program
    {
        internal static void Main()
        {
            try
            {
                var path = GetPath();
                var entries = ReadEntries(path);
                Console.WriteLine($"共读取{entries.Length}个词条。查找空码中...");
                var vacancies = FindVacancies(entries);
                if (vacancies.Length == 0)
                    Console.WriteLine("没有找到任何空码。");
                else
                {
                    Console.WriteLine($"共找到{vacancies.Length}个空码。输出中...");
                    var outputPath = DistinctName(path);
                    if (!WriteFile(outputPath, vacancies)) WriteScreen(vacancies);
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

        private static string[] FindVacancies((string word, string code)[] entries)
        {
            var vacancies = new ConcurrentBag<string>();
            Parallel.ForEach(
                entries, entry =>
                {
                    var limit = entry.word.Length == 3
                        ? 3
                        : 4;
                    for (var i = entry.code.Length - 1; i >= limit; i--)
                    {
                        var code = entry.code.Substring(0, i);
                        if (entries.All(e => e.code != code)) vacancies.Add(code);
                    }
                });
            return vacancies.Distinct().OrderBy(v => v).ToArray();
        }

        private static string DistinctName(string path)
        {
            var dir = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("无法获取上级目录");
            var name = Path.GetFileNameWithoutExtension(path);
            var outputPath = Path.Combine(dir, $"{name}_空码.txt");
            for (var i = 2; File.Exists(outputPath); i++) outputPath = Path.Combine(dir, $"{name}_空码_{i}.txt");
            return outputPath;
        }

        private static bool WriteFile(string path, string[] vacancies)
        {
            try
            {
                File.WriteAllLines(path, vacancies);
                Console.WriteLine($"空码已输出至{path}。");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法将空码输出为文件{path}，因为：{ex.Message}");
                Console.WriteLine("将直接在此列出：");
                return false;
            }
        }

        private static void WriteScreen(string[] vacancies)
        {
            Console.WriteLine("空码：");
            foreach (var vacancy in vacancies) Console.WriteLine(vacancy);
        }
    }
}