using Mikibot.Crawler.WebsocketCrawler.Packet;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace Mikibot.Crawler.WebsocketCrawler.Client;

public class RawWebSocketWorker : IDisposable
{
    private readonly ClientWebSocket ws = new();
    private readonly CancellationTokenSource _csc;
    private readonly SemaphoreSlim _semaphore;

    public RawWebSocketWorker()
    {
        _csc = new CancellationTokenSource();
        _semaphore = new(1);
        _semaphore.Wait();
    }

    public ValueTask ConnectAsync(string host, int port, long roomId, long uid, string auth, string protocol, CancellationToken token)
    {
        return ConnectAsync(null!, host, port, roomId, uid, auth, protocol, token);
    }

    public async ValueTask ConnectAsync(HttpMessageInvoker invoker, string host, int port, long roomId, long uid, string auth, string protocol, CancellationToken token)
    {
        using var csc = CancellationTokenSource.CreateLinkedTokenSource(_csc.Token, token);
        var safeToken = csc.Token;

        var uri = new UriBuilder(protocol, host, port, "/sub").Uri;
        await ws.ConnectAsync(uri, invoker, safeToken);

        await SendAsync(BasePacket.Auth(roomId, uid, auth), safeToken);
        await SendAsync(BasePacket.Keepalive(), token);

        _ = Keepalive(safeToken);

        _semaphore.Release();
    }

    private async ValueTask SendAsync(BasePacket packet, CancellationToken token)
    {
        await SendAsync(packet.ToByte(), token);
    }

    private async ValueTask SendAsync(ReadOnlyMemory<byte> packet, CancellationToken token)
    {
        await ws.SendAsync(packet, WebSocketMessageType.Binary, true, token);
    }

    private async Task Keepalive(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            await SendAsync(BasePacket.Keepalive(), token);
        }
    }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadPacket([EnumeratorCancellation] CancellationToken token)
    {
        using var csc = CancellationTokenSource.CreateLinkedTokenSource(_csc.Token, token);
        await _semaphore.WaitAsync(csc.Token);
        while (ws.State == WebSocketState.Open && !csc.IsCancellationRequested)
        {
            ValueWebSocketReceiveResult result;
            do
            {
                var buffer = new Memory<byte>(new byte[16384]);
                result = await ws.ReceiveAsync(buffer, token);
#if DEBUG
                var length = BinaryPrimitives.ReadUInt32BigEndian(buffer[..4].Span);
                if (length != result.Count)
                    Debug.WriteLine("[Socket] packet required={0} socket receive={1}", length, result.Count);
#endif
                yield return buffer[..result.Count];
            } while (!result.EndOfMessage && !csc.IsCancellationRequested);
        }
    }

    public void Dispose()
    {
        using var socket = this.ws;
        using var csc = this._csc;
        GC.SuppressFinalize(this);
    }
}