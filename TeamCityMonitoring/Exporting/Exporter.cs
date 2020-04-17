using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TeamCityMonitoring.MonitoringMetrics;

namespace TeamCityMonitoring.Exporting
{
    public class Exporter
    {
        private readonly string _csvPath;
        private readonly string _excelPath;

        public Exporter(string csv, string excel)
        {
            _csvPath = csv;
            _excelPath = excel;
        }

        public async Task ExportAsync()
        {
            await Task.Yield();
            Console.WriteLine("Beginning of graph exporting");
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                ShouldQuote = (s, context) => true,
                IgnoreBlankLines = true,
                NewLine = NewLine.CRLF
            };

            try
            {
                using (var stream = File.OpenRead(_csvPath))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var csvReader = new CsvReader(reader, configuration))
                using (var excelStream = File.OpenWrite(_excelPath))
                {
                    var entries = csvReader.GetRecords<QueuedBuildStatus>().ToArray();

                    var workbook = new XSSFWorkbook();

                    CreateBuildNumberSheet(workbook, entries);
                    CreateBuildDetailsSheet(workbook, entries);
                    CreateBranchDetailsSheet(workbook, entries);
                    CreateBuildTypeDetailsSheet(workbook, entries);

                    workbook.Write(excelStream);
                }
            }
            finally
            {
                Console.WriteLine("End of graph exporting");
            }
        }

        private static void CreateBuildDetailsSheet(IWorkbook workbook, IEnumerable<QueuedBuildStatus> entries)
        {
            var buildsDetailsSheet = workbook.CreateSheet("Builds details");

            var builds = entries.GroupBy(e => e.Id).Select(g => new
            {
                Id = g.Key,
                Elements = g.ToArray()
            }).Select(g => new
            {
                g.Id,
                Beginning = g.Elements.Min(e => e.Timestamp),
                Ending = g.Elements.Max(e => e.Timestamp),
                g.Elements.First().Type,
                g.Elements.First().Branch
            }).ToArray();

            var rowIndex = 0;
            var row = buildsDetailsSheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue("Build id");
            row.CreateCell(1).SetCellValue("Beginning queue timestamp");
            row.CreateCell(2).SetCellValue("Ending queue timestamp");
            row.CreateCell(3).SetCellValue("Queue duration");
            row.CreateCell(4).SetCellValue("Build type");
            row.CreateCell(5).SetCellValue("Branch");

            foreach (var status in builds)
            {
                rowIndex++;
                var statusRow = buildsDetailsSheet.CreateRow(rowIndex);
                statusRow.CreateCell(0).SetCellValue(status.Id);
                statusRow.CreateCell(1).SetCellValue(status.Beginning);
                statusRow.CreateCell(2).SetCellValue(status.Ending);
                statusRow.CreateCell(3).SetCellValue((DateTime.Parse(status.Ending) - DateTime.Parse(status.Beginning)).TotalMinutes);
                statusRow.CreateCell(4).SetCellValue(status.Type);
                statusRow.CreateCell(5).SetCellValue(status.Branch);
            }
        }

        private static void CreateBuildTypeDetailsSheet(IWorkbook workbook, IEnumerable<QueuedBuildStatus> entries)
        {
            var buildTypesDetailsSheet = workbook.CreateSheet("Build types details");

            var builds = entries.GroupBy(e => e.Id).Select(g => new
            {
                Id = g.Key,
                Elements = g.ToArray()
            }).Select(g => new
            {
                g.Id,
                Duration = g.Elements.Max(e => DateTime.Parse(e.Timestamp)) - g.Elements.Min(e => DateTime.Parse(e.Timestamp)),
                g.Elements.First().Type,
                g.Elements.First().Branch
            }).GroupBy(b => b.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Elements = g.ToArray()
                }).Select(g => new
                {
                    g.Type,
                    MeanTime = g.Elements.Sum(e => e.Duration.TotalMinutes) / g.Elements.Length,
                    NumberOfBuilds = g.Elements.Length,
                    NumberOfBranches = g.Elements.Select(e => e.Branch).Distinct().Count()
                });

            var rowIndex = 0;
            var row = buildTypesDetailsSheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue("Build type");
            row.CreateCell(1).SetCellValue("Mean queue duration");
            row.CreateCell(2).SetCellValue("Number of builds");
            row.CreateCell(3).SetCellValue("Number of branches");

            foreach (var status in builds)
            {
                rowIndex++;
                var statusRow = buildTypesDetailsSheet.CreateRow(rowIndex);
                statusRow.CreateCell(0).SetCellValue(status.Type);
                statusRow.CreateCell(1).SetCellValue(status.MeanTime);
                statusRow.CreateCell(2).SetCellValue(status.NumberOfBuilds);
                statusRow.CreateCell(3).SetCellValue(status.NumberOfBranches);
            }
        }

        private static void CreateBranchDetailsSheet(IWorkbook workbook, IEnumerable<QueuedBuildStatus> entries)
        {
            var branchesDetailsSheet = workbook.CreateSheet("Branches details");

            var builds = entries.GroupBy(e => e.Id).Select(g => new
                {
                    Id = g.Key,
                    Elements = g.ToArray()
                }).Select(g => new
                {
                    g.Id,
                    Duration = g.Elements.Max(e => DateTime.Parse(e.Timestamp)) - g.Elements.Min(e => DateTime.Parse(e.Timestamp)),
                    g.Elements.First().Type,
                    g.Elements.First().Branch
                }).GroupBy(b => b.Branch)
                .Select(g => new
                {
                    Branch = g.Key,
                    Elements = g.ToArray()
                }).Select(g => new
                {
                    g.Branch,
                    MeanTime = g.Elements.Sum(e => e.Duration.TotalMinutes) / g.Elements.Length,
                    NumberOfBuilds = g.Elements.Length,
                    NumberOfBuildTypes = g.Elements.Select(e => e.Type).Distinct().Count()
                });

            var rowIndex = 0;
            var row = branchesDetailsSheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue("Branch");
            row.CreateCell(1).SetCellValue("Mean queue duration");
            row.CreateCell(2).SetCellValue("Number of builds");
            row.CreateCell(3).SetCellValue("Number of build types");

            foreach (var status in builds)
            {
                rowIndex++;
                var statusRow = branchesDetailsSheet.CreateRow(rowIndex);
                statusRow.CreateCell(0).SetCellValue(status.Branch);
                statusRow.CreateCell(1).SetCellValue(status.MeanTime);
                statusRow.CreateCell(2).SetCellValue(status.NumberOfBuilds);
                statusRow.CreateCell(3).SetCellValue(status.NumberOfBuildTypes);
            }
        }

        private static void CreateBuildNumberSheet(IWorkbook workbook, IEnumerable<QueuedBuildStatus> entries)
        {
            var buildQueueSizeSheet = workbook.CreateSheet("Build queue size");

            var numberOfBuildsByTime = entries.GroupBy(e => $"{e.Timestamp}").Select(g => g.First()).ToArray();
            
            var rowIndex = 0;
            var row = buildQueueSizeSheet.CreateRow(rowIndex);

            row.CreateCell(0).SetCellValue("Timestamps");
            row.CreateCell(1).SetCellValue("Number of builds");

            foreach (var status in numberOfBuildsByTime)
            {
                rowIndex++;
                var statusRow = buildQueueSizeSheet.CreateRow(rowIndex);
                statusRow.CreateCell(0).SetCellValue(status.Timestamp);
                statusRow.CreateCell(1).SetCellValue(status.NumberOfBuilds);
            }
        }
    }
}
