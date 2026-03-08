using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using ClosedXML.Excel;

namespace AbsoluteCinema.ViewModels.Preview;

public class ExcelPreviewRenderer(DataGrid dataGrid)
{
    public void Render(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return;

        var lastRow = worksheet.LastRowUsed();
        if (lastRow == null) return;

        var totalColumns = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (totalColumns == 0) return;

        // ── Auto-detect header row ──
        // The header row is the first row where more than half the cells
        // contain non-empty text (titles typically span one merged cell).

        var headerRowNum = 1;
        foreach (var row in worksheet.RowsUsed())
        {
            var filledCount = 0;
            for (var c = 1; c <= totalColumns; c++)
            {
                if (!row.Cell(c).IsEmpty())
                    filledCount++;
            }

            if (filledCount <= totalColumns / 2) continue;
            headerRowNum = row.RowNumber();
            break;
        }

        var headerRow = worksheet.Row(headerRowNum);

        // ── Build columns dynamically from the detected header row ──

        var columnKeys = new List<(string Key, int ColIndex)>();

        Clear();

        for (var c = 1; c <= totalColumns; c++)
        {
            var cell = headerRow.Cell(c);
            if (cell.IsEmpty()) continue;

            var key = $"Col{c}";
            var header = cell.GetFormattedString().Replace("\n", " ");

            columnKeys.Add((key, c));

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding($"[{key}]"),
                Width = DataGridLength.Auto
            });
        }

        // ── Read every row below the header into dictionaries ──

        var items = new ObservableCollection<Dictionary<string, object>>();

        var lastRowNum = lastRow.RowNumber();

        for (var r = headerRowNum + 1; r <= lastRowNum; r++)
        {
            var row  = worksheet.Row(r);
            var dict = new Dictionary<string, object>();

            foreach (var (key, colIndex) in columnKeys)
            {
                var cell = row.Cell(colIndex);
                dict[key] = cell.IsEmpty()
                    ? string.Empty
                    : cell.GetFormattedString();
            }

            items.Add(dict);
        }

        dataGrid.ItemsSource = items;
    }

    public async Task RenderAsync(string filePath)
    {
        var (columnKeys, items) = await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                return ([], []);

            var lastRow = worksheet.LastRowUsed();
            var totalColumns = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow == null || totalColumns == 0)
                return ([], []);

            // Auto-detect header row
            var headerRowNum = 1;
            foreach (var row in worksheet.RowsUsed())
            {
                var filled = 0;
                for (var c = 1; c <= totalColumns; c++)
                    if (!row.Cell(c).IsEmpty()) filled++;

                if (filled <= totalColumns / 2) continue;
                headerRowNum = row.RowNumber();
                break;
            }

            // Collect column info
            var cols = new List<(string Key, string Header)>();
            var colIndices = new List<(string Key, int Index)>();

            for (var c = 1; c <= totalColumns; c++)
            {
                var cell = worksheet.Row(headerRowNum).Cell(c);
                if (cell.IsEmpty()) continue;

                var key = $"Col{c}";
                cols.Add((key, cell.GetFormattedString().Replace("\n", " ")));
                colIndices.Add((key, c));
            }

            // Read data rows
            var rows = new List<Dictionary<string, object>>();
            for (var r = headerRowNum + 1; r <= lastRow.RowNumber(); r++)
            {
                var row = worksheet.Row(r);
                var dict = new Dictionary<string, object>();
                foreach (var (key, colIndex) in colIndices)
                    dict[key] = row.Cell(colIndex).IsEmpty()
                        ? string.Empty
                        : row.Cell(colIndex).GetFormattedString();
                rows.Add(dict);
            }

            return (cols, rows);
        });
        
        Clear();
        
        foreach (var (key, header) in columnKeys)
        {
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding($"[{key}]"),
                Width = DataGridLength.Auto
            });
        }

        dataGrid.ItemsSource = new ObservableCollection<Dictionary<string, object>>(items);
    }

    public void Clear()
    {
        dataGrid.Columns.Clear();
        dataGrid.ItemsSource = null;
    }
}