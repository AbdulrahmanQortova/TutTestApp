using Grpc.Core;
using ProtoBuf.Grpc;
using Tut.Common.Business;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutBackend.Repositories;
namespace TutBackend.Services;

public static class AuthUtils
{
    public static async Task<Driver?> AuthorizeDriver(CallContext context, IDriverRepository driverRepository, QipClient qipClient)
    {
        Driver? driver = null;
        string? token = context.CallOptions.Headers?.GetValue("Authorization") ?? string.Empty;
        ValidateResponse vres = await qipClient.ValidateAsync(new ValidateRequest
        {
            Token = token
        });
        if (vres is { IsValid: true, Username: not null })
            driver = await driverRepository.GetByMobileAsync(vres.Username);

        if (driver is null || driver.State == DriverState.Deleted)
        {
            return null;
        }
        return driver;
    }
    public static async Task<User?> AuthorizeUser(CallContext context, IUserRepository userRepository, QipClient qipClient)
    {
        User? user = null;
        string? token = context.CallOptions.Headers?.GetValue("Authorization") ?? string.Empty;
        ValidateResponse vres = await qipClient.ValidateAsync(new ValidateRequest
        {
            Token = token
        });
        if (vres is { IsValid: true, Username: not null })
            user = await userRepository.GetByMobileAsync(vres.Username);

        if (user is null || user.Status == UserState.Deleted || user.Status == UserState.Blocked)
        {
            return null;
        }
        return user;
    }
}
