using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Dtos;
using AbsoluteCinema.Services.Movies;
using Microsoft.Extensions.Options;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace AbsoluteCinema.Services.Schedule;

public class RepertoireService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    IOptionsMonitor<DocumentTemplateConfiguration> templateConfiguration
    ) : ScheduleService(rootConfiguration)
{
    private DocumentTemplateConfiguration Configuration => templateConfiguration.Get("Repertoire");

    public override async Task<string> GenerateScheduleFiles(DateTime from, DateTime to, CinemaWebMovieProvider movieProvider,
        CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);
        Directory.CreateDirectory(sessionPath);
        var activeMovies = await movieProvider.GetActiveMoviesAsync(cancellationToken: cancellationToken);
        FillRepertoire(activeMovies, sessionPath);
        return sessionPath;
    }
    
    private void FillRepertoire(IReadOnlyCollection<CinemaWebMovie> movies, string sessionPath)
    {
        var templatePath = Configuration.TemplatePath;
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new Exception("Repertoire template path is not setup");

        using var document = DocX.Load(templatePath);

        var builder = new StringBuilder();
        foreach (var movie in movies)
        {
            builder.AppendLine($"c {movie.DistributionBegin:d MMMM} - {movie.DistributionEnd:d MMMM}" +
                               $" «{movie.Name}», {movie.AgeRestriction}, " +
                               $"{string.Join(", ", movie.Formats)}, {movie.Details?.Countries ?? ""}, {movie.Details?.Genres.ToLower() ?? ""}");
        }

        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{movies}}", NewValue = builder.ToString() }); 
        
        var newFileName = "Репертуар кинофильмов. ТЮЗ.";
        var newFilePath = Path.Combine(sessionPath, newFileName);
        document.SaveAs(newFilePath);
    }
}