using Microsoft.AspNetCore.Components.Server.Circuits;

namespace RealTimeChat.BlazorServer.Services
{//привяжем статус онлайн к соединению вкладки, сохранив очистку участия при уничтожении компонента.
    public sealed class RoomCircuitHandler(ILogger<RoomCircuitHandler> logger) : CircuitHandler // экземпляр класса будет принадлежать circuit этой вкладки.
    {
        public bool IsConnected { get; private set; }

        public event Func<bool, Task>? ConnectionChanged;

        // Blazor сам вызываетOnConnectionUpAsync при первоначальном подключении и последующих переподключениях.
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            return SetConnectionAsync(true);
        }

        // Blazor сам вызывает OnConnectionDownAsync при обнаружении разрыва
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            return SetConnectionAsync(false);
        }

        private async Task SetConnectionAsync(bool connected)
        {
            IsConnected = connected;

            var handlers = ConnectionChanged;

            if (handlers is null)
                return;

            foreach (Func<bool, Task> handler in handlers.GetInvocationList()) {
                try {
                    await handler(connected);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failed to update room participation. Connected: {Connected}.", connected);
                }
            }
        }
    }
}
