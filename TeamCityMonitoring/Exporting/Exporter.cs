﻿using System;
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
                    //CreateBranchDetailsSheet(workbook, entries);
                    //CreateBuildTypeDetailsSheet(workbook, entries);

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
                statusRow.CreateCell(3).SetCellValue((status.Ending - status.Beginning).TotalMinutes);
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
                Beginning = g.Elements.Min(e => e.Timestamp),
                Ending = g.Elements.Max(e => e.Timestamp),
                g.Elements.First().Type,
                g.Elements.First().Branch
            }).ToArray();

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
                statusRow.CreateCell(0).SetCellValue(status.Id);
                statusRow.CreateCell(1).SetCellValue(status.Beginning);
                statusRow.CreateCell(2).SetCellValue(status.Ending);
                statusRow.CreateCell(3).SetCellValue((status.Ending - status.Beginning).TotalMinutes);
                statusRow.CreateCell(4).SetCellValue(status.Type);
                statusRow.CreateCell(5).SetCellValue(status.Branch);
            }
        }

        private static void CreateBuildNumberSheet(IWorkbook workbook, IEnumerable<QueuedBuildStatus> entries)
        {
            var buildQueueSizeSheet = workbook.CreateSheet("Build queue size");

            var numberOfBuildsByTime = entries.GroupBy(e => $"{e.Timestamp}").Select(g => g.First()).ToArray();
            
            var rowIndex = 0;
            var row = buildQueueSizeSheet.CreateRow(rowIndex);
            var dateTimeStyle = workbook.CreateCellStyle();


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
