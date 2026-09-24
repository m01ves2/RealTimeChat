using RealTimeChat.Application.Exceptions;
using RealTimeChat.Application.Services;
using RealTimeChat.Domain.Entities;

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
                return Results.Ok(rooms);
            });

            group.MapGet("/{roomId:int}", async (int roomId, ChatService chatService, CancellationToken cancellationToken) =>
            {
                try {
                    var room = await chatService.GetRoomAsync(roomId, cancellationToken);
                    return Results.Ok(room);
                }
                catch (NotFoundException) {
                    return Results.NotFound();
                }
            });

            return endpoints;
        }
    }
}
