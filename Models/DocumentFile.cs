namespace AbsoluteCinema.Models;

public record DocumentFile(string Path)
{
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
    public string Extension => System.IO.Path.GetExtension(Path).ToLowerInvariant();

    public bool IsPdf => Extension == ".pdf";
    public bool IsExcel => Extension is ".xlsx" or ".xls";
    public bool IsWord => Extension is ".docx" or ".doc";
}