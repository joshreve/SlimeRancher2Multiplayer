using System.Buffers;

namespace SR2MP.Shared.Utils;

internal readonly struct SplitResult : IDisposable
{
    internal readonly ArraySegment<byte>[] Chunks;
    internal readonly int Count;

    internal SplitResult(ArraySegment<byte>[] chunks, int count)
    {
        Chunks = chunks;
        Count = count;
    }

    public void Dispose()
    {
        for (var i = 0; i < Count; i++)
        {
            if (Chunks[i].Array != null)
                ArrayPool<byte>.Shared.Return(Chunks[i].Array!);
        }

        ArrayPool<ArraySegment<byte>>.Shared.Return(Chunks);
    }
}