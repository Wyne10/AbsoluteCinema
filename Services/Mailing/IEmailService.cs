using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Mailing;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, IEnumerable<string> attachmentPaths, CancellationToken cancellationToken = default);
}
