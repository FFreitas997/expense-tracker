namespace Infrastructure.Email.Templates;

public static class PasswordResetTemplate
{
    public static string Build(string userName, string resetLink)
    {
        return $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;">
                  <div style="max-width: 600px; margin: auto; background: white;
                              padding: 30px; border-radius: 8px;">
                    <h2 style="color: #333;">Password Reset Request</h2>
                    <p>Hi <strong>{userName}</strong>,</p>
                    <p>We received a request to reset your password.
                       Click the button below to proceed:</p>
                    <a href="{resetLink}"
                       style="display: inline-block; padding: 12px 24px;
                              background-color: #4F46E5; color: white;
                              text-decoration: none; border-radius: 6px;
                              margin: 20px 0;">
                      Reset Password
                    </a>
                    <p style="color: #666; font-size: 14px;">
                      This link expires in <strong>1 hour</strong>.<br/>
                      If you did not request a password reset, you can safely ignore this email.
                    </p>
                  </div>
                </body>
                </html>
                """;
    }
}