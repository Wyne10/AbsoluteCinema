using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Movies;

public interface IMovieProvider
{
    Task<Dictionary<string, Dtos.Movie>> GetMovies(IEnumerable<string> movieNames);
}