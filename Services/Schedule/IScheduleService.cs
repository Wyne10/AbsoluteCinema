using System;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Services.Movies;

namespace AbsoluteCinema.Services.Schedule;

public interface IScheduleService
{
    string GetSessionPath(DateTime from, DateTime to);
    Task<string> GenerateScheduleFiles(DateTime from, DateTime to, CinemaWebMovieProvider movieProvider, CancellationToken cancellationToken = default);
}