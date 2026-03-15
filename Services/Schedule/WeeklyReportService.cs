using AbsoluteCinema.Configuration;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Schedule;

public sealed class CompleteScheduleService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    RepertoireService repertoire,
    ScheduleSessionService session)
    : CompositeScheduleService(rootConfiguration, [repertoire, session]);