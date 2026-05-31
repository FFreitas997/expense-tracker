namespace API.Security.Authorization.Policies;

public static class PolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string MemberOnly = "MemberOnly";
    public const string ResourceOwner = "ResourceOwner";
    public const string BackOffice = "BackOffice";
}