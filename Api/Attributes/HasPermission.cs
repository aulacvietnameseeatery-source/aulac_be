using Microsoft.AspNetCore.Authorization;

namespace API.Attributes;

public class HasPermission : AuthorizeAttribute
{
    public HasPermission(string permission) : base(policy: permission)
    {
    }
}