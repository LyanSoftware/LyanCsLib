using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Lytec.Polyfills;

public static class SocketPolyfill
{
    public static async ValueTask ConnectAsync(
        this Socket socket,
        EndPoint endPoint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (cancellationToken.Register(
            static state => ((Socket)state).Dispose(),
            socket))
        {
            try
            {
                await socket.ConnectAsync(endPoint).ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
