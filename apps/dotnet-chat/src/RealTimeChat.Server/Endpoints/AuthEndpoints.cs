using Microsoft.AspNetCore.Identity;
using RealTimeChat.Contracts.Authentication;
using RealTimeChat.Infrastructure.Identity;
using System.Net;

namespace RealTimeChat.Server.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/auth")
                .DisableCookieRedirect(); // Этот метод указывает cookie-обработчику возвращать 401 при отсутствии входа и 403 при запрете доступа вместо перенаправления.

            group.MapPost("/register", RegisterAsync).AllowAnonymous();
            group.MapPost("/login", LoginAsync).AllowAnonymous();
            group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();
            group.MapPost("/logout", LogoutAsync).RequireAuthorization();

            return endpoints;
        }

        private static async Task<IResult> RegisterAsync(RegisterRequest request,
                                                         UserManager<ApplicationUser> userManager,
                                                         SignInManager<ApplicationUser> signInManager)
        {
            if (string.IsNullOrWhiteSpace(request.UserName)) {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["UserName"] = ["User name is required."]
                });
            }

            if (string.IsNullOrEmpty(request.Password)) {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Password"] = ["Password is required."]
                });
            }

            var user = new ApplicationUser()
            {
                UserName = request.UserName.Trim()
            };

            // UserManager creates and persists the Identity user:
            // validates the password and user, hashes the password,
            // normalizes the username, and persists the user.
            // See:
            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.createasync?view=aspnetcore-10.0

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded) { //result.Succeeded сообщает, прошли ли правила Identity и удалось ли сохранение; result.Errors объясняет отказ.
                var errors = result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => error.Description)
                            .ToArray());

                return Results.ValidationProblem(errors);
            }

            // SignInManager creates the ClaimsPrincipal and issues the authentication cookie.
            // Cookie authentication without Identity, shown explicitly:
            // https://metanit.com/sharp/aspnet6/13.4.php
            await signInManager.SignInAsync(user, isPersistent: false); //SignInManager подготавливает представление пользователя и выдаёт cookie через настроенную схему Identity.

            return Results.Ok(new CurrentUserResponse(user.Id, user.UserName!));
        }

        private static async Task<IResult> LoginAsync(LoginRequest request,
                                                      UserManager<ApplicationUser> userManager,
                                                      SignInManager<ApplicationUser> signInManager)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password)) {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByNameAsync(request.UserName.Trim());
            if (user is null) {
                return Results.Unauthorized();
            }

            var result = await signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!result.Succeeded) {
                return Results.Unauthorized();
            }

            return Results.Ok(new CurrentUserResponse(user.Id, user.UserName!));
        }

        private static async Task<IResult> GetCurrentUserAsync(HttpContext context,
                                                               UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.GetUserAsync(context.User);

            return user is null ? Results.Unauthorized() : Results.Ok(new CurrentUserResponse(user.Id, user.UserName!));
        }

        private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }
    }
}