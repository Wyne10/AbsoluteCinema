using System;
using System.Collections.Generic;
using AbsoluteCinema.Models;

namespace AbsoluteCinema.Dtos;

public class MailingData
{
    public List<EmailContactDto> Contacts { get; set; } = [];
    public List<MailingRuleDto> Rules { get; set; } = [];
}

public class EmailContactDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MailingRuleDto
{
    public Guid Id { get; set; }
    public string ServiceKey { get; set; } = string.Empty;
    public MailingFrequency Frequency { get; set; }
    public string Subject { get; set; } = string.Empty;
    public List<Guid> ContactIds { get; set; } = [];
}
