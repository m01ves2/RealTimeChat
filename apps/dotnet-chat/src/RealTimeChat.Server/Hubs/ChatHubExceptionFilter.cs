using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Application.Exceptions;

namespace RealTimeChat.Server.Hubs
{
    public sealed class ChatHubExceptionFilter : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync( HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            try {
                return await next(context);
            }
            catch (NotFoundException ex) {
                throw new HubException(ex.Message);
            }
        }
    }
}
