using Microsoft.AspNetCore.Identity;
using RealTimeChat.Application.Exceptions;
using RealTimeChat.Application.Services;
using RealTimeChat.Contracts.Messages;
using RealTimeChat.Contracts.Rooms;
using RealTimeChat.Infrastructure.Identity;

namespace RealTimeChat.Server.Endpoints
{
    public static class RoomEndpoints
    {
        public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/rooms") //makes group of endpoints with common prefix: /api/rooms
                .RequireAuthorization() //Authorization required for whole group
                .DisableCookieRedirect();

            // GET /api/rooms/
            group.MapGet("/", async (ChatService chatService, CancellationToken cancellationToken) =>
            {
                var rooms = await chatService.GetRoomsAsync(cancellationToken);

                var response = rooms.Select(room => new RoomResponse(room.Id, room.Name)).ToArray();

                return Results.Ok(response);
            });

            group.MapGet("/{roomId:int}", async (int roomId,
                                                 ChatService chatService, CancellationToken cancellationToken) =>
            {
                try {
                    var room = await chatService.GetRoomAsync(roomId, cancellationToken);
                    return Results.Ok(room);
                }
                catch (NotFoundException) {
                    return Results.NotFound();
                }
            });

            group.MapGet("/{roomId:int}/messages", async (int roomId, HttpContext context,
                                                          UserManager<ApplicationUser> userManager,
                                                          ChatService chatService,
                                                          CancellationToken cancellationToken) =>
            {
                var userIdText = userManager.GetUserId(context.User);
                if (!int.TryParse(userIdText, out var userId)) {
                    return Results.Unauthorized();
                }

                try {
                    await chatService.GetRoomAsync(roomId, cancellationToken);

                    var messages = await chatService.GetRecentMessagesAsync(
                        roomId, userId, cancellationToken);


                    var response = messages.Select(message => new ChatMessageResponse(  message.Id,
                                                                                        message.RoomId,
                                                                                        message.AuthorId,
                                                                                        message.AuthorName,
                                                                                        message.RecipientId,
                                                                                        message.RecipientName,
                                                                                        message.Text,
                                                                                        message.CreatedAt)).ToArray();

                    return Results.Ok(response);
                }
                catch (NotFoundException) {
                    return Results.NotFound();
                }
            });

            return endpoints;
        }
    }
}
