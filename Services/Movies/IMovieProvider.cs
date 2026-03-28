using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Movies;

public interface IMovieProvider<TMovie>
{
    Task<Dictionary<string, TMovie>> GetMovies(IEnumerable<string> movieNames, CancellationToken cancellationToken = default);
}