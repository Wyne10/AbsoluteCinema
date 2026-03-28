using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Dtos;

namespace AbsoluteCinema.Services.Trailers;

public interface ITrailerService
{
    Task<string> RenderTrailers(IEnumerable<KinoplanFile> files, CancellationToken cancellationToken = default);
}