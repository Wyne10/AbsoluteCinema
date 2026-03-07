namespace AbsoluteCinema.ViewModels.Preview;

public interface IPreviewRenderer
{
    void Render(string filePath);
    void Hide();
}