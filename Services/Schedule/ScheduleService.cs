using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Services.Movies;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Schedule;

public abstract class ScheduleService(IOptionsMonitor<DocumentRootConfiguration> rootConfiguration) : IScheduleService
{
    public string GetSessionPath(DateTime startDate, DateTime endDate)
    {
        var scheduleRootPath = rootConfiguration.Get("ScheduleRootPath").RootPath;
        var sessionPath = Path.Combine(scheduleRootPath, $"Расписание {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
        return sessionPath;
    }

    public abstract Task<string> GenerateScheduleFiles(DateTime from, DateTime to, CinemaWebMovieProvider movieProvider, CancellationToken cancellationToken = default);
}