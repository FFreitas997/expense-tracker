namespace Infrastructure.Email.Models;

public sealed class EmailMessage
{
    public string To { get; set; } = null!;
    public string ToName { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool IsHtml { get; set; } = true;
}