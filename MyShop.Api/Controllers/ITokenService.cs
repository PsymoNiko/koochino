using MyShop.Core.Models;
using System.Collections.Generic;

namespace MyShop.Api.Auth;

public interface ITokenService
{
    string GenerateToken(int userId, string username, string role, int expiresMinutes);

}
