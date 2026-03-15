using System;
using System.Collections.Generic;

namespace AbsoluteCinema.Dtos;

public record CinemaWebSession(
    int Id,
    string MovieName,
    int? MovieId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int Duration,
    List<int> Prices,
    bool IsLocked,
    bool IsDeleted,
    int AuditoriumId);
