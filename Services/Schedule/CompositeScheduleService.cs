using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Services.Movies;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Schedule;

public class CompositeScheduleService : ScheduleService
{
    private readonly IEnumerable<IScheduleService> _scheduleServices;

    protected CompositeScheduleService(IOptionsMonitor<DocumentRootConfiguration> rootConfiguration, IEnumerable<IScheduleService> scheduleServices) : base(rootConfiguration)
    {
        _scheduleServices = scheduleServices;
    }

    public override async Task<string> GenerateScheduleFiles(DateTime from, DateTime to, CinemaWebMovieProvider movieProvider, CancellationToken cancellationToken = default)
    {
        foreach(var scheduleService in _scheduleServices) await scheduleService.GenerateScheduleFiles(from, to, movieProvider, cancellationToken);
        return GetSessionPath(from, to);
    }
}