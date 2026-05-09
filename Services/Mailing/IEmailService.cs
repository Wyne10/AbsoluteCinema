using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Mailing;

public interface IEmailService
{
    Task SendAsync(IEnumerable<string> recipients, string subject, string body, IEnumerable<string> attachmentPaths, CancellationToken cancellationToken = default);
}
