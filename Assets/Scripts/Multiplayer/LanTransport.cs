using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Two-peer LAN prototype transport. No Unity API is used on worker threads.
/// Frames are length-prefixed; state has a one-frame mailbox so slow peers cannot
/// accumulate obsolete snapshots. Control messages are bounded and ordered.</summary>
public sealed class LanTransport : IDisposable
{
    public const int Port = 7777;
    public const int MaxFrameBytes = 1024 * 1024;
    readonly ConcurrentQueue<string> received = new ConcurrentQueue<string>();
    readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
    readonly CancellationTokenSource stop = new CancellationTokenSource();
    readonly object gate = new object();
    TcpListener listener;
    TcpClient client;
    byte[] latestOutgoing;
    string latestIncoming;
    volatile string error;
    volatile bool connected;
    public bool Connected => connected;
    public string Error => error;
    public long BytesSent;
    public long BytesReceived;

    public void Host()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start(2);
            _ = AcceptLoop();
        }
        catch (Exception ex) { Fail("无法创建房间（端口可能被占用）：" + ex.Message); }
    }

    async Task AcceptLoop()
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                var next = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                lock (gate)
                {
                    if (client != null || stop.IsCancellationRequested) { next.Close(); continue; }
                    client = next;
                }
                StartPeer(next);
            }
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail(ex.Message); }
    }

    public async void Join(string address)
    {
        try
        {
            var next = new TcpClient(AddressFamily.InterNetwork);
            lock (gate) { if (stop.IsCancellationRequested) { next.Close(); return; } client = next; }
            var connection = next.ConnectAsync(IPAddress.Parse(address), Port);
            if (await Task.WhenAny(connection, Task.Delay(6000, stop.Token)).ConfigureAwait(false) != connection)
            { Fail("连接超时，请检查房主 IP、局域网和 TCP 7777 端口。"); return; }
            await connection.ConfigureAwait(false);
            if (!stop.IsCancellationRequested) StartPeer(next);
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail("连接失败：" + ex.Message); }
    }

    void StartPeer(TcpClient peer)
    {
        peer.NoDelay = true;
        connected = true;
        _ = Task.Run(() => ReadLoop(peer));
        _ = Task.Run(() => WriteLoop(peer));
    }

    public void Send(string json, bool state = false)
    {
        if (stop.IsCancellationRequested) return;
        byte[] data = Encoding.UTF8.GetBytes(json);
        if (data.Length + 1 > MaxFrameBytes) { Fail("场景状态超过测试版传输上限。"); return; }
        byte[] frame = new byte[data.Length + 5];
        int length = data.Length + 1;
        frame[0] = (byte)(length >> 24); frame[1] = (byte)(length >> 16);
        frame[2] = (byte)(length >> 8); frame[3] = (byte)length;
        frame[4] = state ? (byte)1 : (byte)0;
        Buffer.BlockCopy(data, 0, frame, 5, data.Length);
        if (state) Interlocked.Exchange(ref latestOutgoing, frame);
        else if (outgoing.Count < 32) outgoing.Enqueue(frame);
        else Fail("网络发送队列已满。");
    }

    public bool TryReceive(out string json) => received.TryDequeue(out json);
    public string TakeState() => Interlocked.Exchange(ref latestIncoming, null);

    async Task ReadLoop(TcpClient peer)
    {
        try
        {
            NetworkStream stream = peer.GetStream();
            byte[] header = new byte[4];
            while (!stop.IsCancellationRequested)
            {
                await ReadExactly(stream, header, 4, stop.Token).ConfigureAwait(false);
                int length = DecodeLength(header);
                byte[] body = new byte[length];
                await ReadExactly(stream, body, length, stop.Token).ConfigureAwait(false);
                Interlocked.Add(ref BytesReceived, length + 4);
                string json = Encoding.UTF8.GetString(body, 1, body.Length - 1);
                if (body[0] == 1) Interlocked.Exchange(ref latestIncoming, json);
                else if (body[0] == 0 && received.Count < 32) received.Enqueue(json);
                else throw new InvalidDataException("Invalid frame or message flood.");
            }
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail("连接已中断：" + ex.Message); }
    }

    public static int DecodeLength(byte[] header)
    {
        int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
        if (length < 2 || length > MaxFrameBytes) throw new InvalidDataException("Invalid frame length.");
        return length;
    }

    static async Task ReadExactly(Stream stream, byte[] data, int length, CancellationToken token)
    {
        int offset = 0;
        while (offset < length)
        {
            int count = await stream.ReadAsync(data, offset, length - offset, token).ConfigureAwait(false);
            if (count == 0) throw new EndOfStreamException("对方已退出房间");
            offset += count;
        }
    }

    async Task WriteLoop(TcpClient peer)
    {
        try
        {
            var stream = peer.GetStream();
            while (!stop.IsCancellationRequested)
            {
                if (!outgoing.TryDequeue(out byte[] frame)) frame = Interlocked.Exchange(ref latestOutgoing, null);
                if (frame == null) { await Task.Delay(5, stop.Token).ConfigureAwait(false); continue; }
                await stream.WriteAsync(frame, 0, frame.Length, stop.Token).ConfigureAwait(false);
                Interlocked.Add(ref BytesSent, frame.Length);
            }
        }
        catch (Exception ex) { if (!stop.IsCancellationRequested) Fail("发送失败：" + ex.Message); }
    }

    void Fail(string message)
    {
        // Some Mono/Windows socket messages contain trailing NUL padding.
        string clean = message.Replace("\0", string.Empty).Trim();
        error = clean.Length > 250 ? clean.Substring(0, 250) : clean;
        Dispose();
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (stop.IsCancellationRequested) return;
            stop.Cancel(); connected = false;
            listener?.Stop(); client?.Close();
        }
    }
}
