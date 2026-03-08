using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace AbsoluteCinema.Dtos;

public record GrossMovieData(string MovieName, string ScreenType, int SessionCount, int ViewerCount, int Gross);

public static class GrossMovieDataConstructors 
{
    extension(GrossMovieData)
    {
        public static HashSet<GrossMovieData> Parse(string grossReportFilePath)
        {
            var grossMovieData = new HashSet<GrossMovieData>();
            using var workbook = new XLWorkbook(grossReportFilePath);
            var worksheet = workbook.Worksheets.First();

            var currentMovieTitle = string.Empty;

            // TODO Use RowsUsed instead?
            // Пропускаем первые 4 строки (заголовки)
            foreach (var row in worksheet.Rows().Skip(4))
            {
                var firstCell = row.Cell(1);
                var firstCellValue = firstCell.GetValue<string>().Trim();

                // Если ячейка во втором столбце пустая, а в первом нет, и это не "Итог" - это название фильма
                if (!string.IsNullOrWhiteSpace(firstCellValue) && row.Cell(2).IsEmpty())
                {
                    if (!firstCellValue.Equals("итог", StringComparison.OrdinalIgnoreCase))
                    {
                        currentMovieTitle = firstCellValue;
                    }
                }
            
                // Если в первой ячейке "Итог", это строка с итоговыми данными для фильма
                if (!firstCellValue.Equals("итог", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(currentMovieTitle)) continue;
            
                var movieData = new GrossMovieData
                (
                    currentMovieTitle[..^2].Trim(),
                    currentMovieTitle.Substring(currentMovieTitle.Length - 2, 2).Trim(),
                    row.Cell(2).GetValue<int>(),
                    row.Cell(3).GetValue<int>(),
                    row.Cell(4).GetValue<int>()
                );
                grossMovieData.Add(movieData);
                currentMovieTitle = string.Empty; // Сбрасываем для следующего фильма
            }

            return grossMovieData;
        }
    }
}