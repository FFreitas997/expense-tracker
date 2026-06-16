using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Email;

public class EmailSettings
{
    [Required(ErrorMessage = "SMTP host is required.")]
    public string Host { get; set; } = null!;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 587;

    [Required(ErrorMessage = "SMTP username is required.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "SMTP password is required.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Sender email address is required.")]
    [EmailAddress(ErrorMessage = "FromAddress must be a valid email address.")]
    public string FromAddress { get; set; } = null!;

    [Required(ErrorMessage = "Sender display name is required.")]
    public string FromName { get; set; } = null!;

    public bool UseSsl { get; set; } = true;
}
