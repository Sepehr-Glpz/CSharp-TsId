using System.Security.Cryptography;
using System.Threading;

namespace SGSX.Common.TsId;
public class TsIdGenerator
{
    #region Fields

    private readonly byte _nodeId;

    private volatile uint _counter;

    private readonly DateTimeOffset _epoch;

    public static readonly DateTimeOffset DefaultEpoch;

    #endregion

    #region Constructors
    public TsIdGenerator(byte nodeId, DateTimeOffset epoch) : this(nodeId) => 
        _epoch = epoch;

    public TsIdGenerator(byte nodeId) =>
        (_nodeId, _epoch) = (nodeId, DefaultEpoch);

    public TsIdGenerator() : base()
    {
        _nodeId = CreateDefaultNodeId();
        _counter = 0;
        _epoch = DefaultEpoch;
    }

    static TsIdGenerator()
    {
        DefaultEpoch = new DateTimeOffset(new DateOnly(2020, 1, 1), TimeOnly.MinValue, TimeSpan.Zero);
    }
    #endregion

    #region Methods

    public TsId NewTsId() => new(GetTimeStamp(), _nodeId, NextCount());

    #endregion

    #region Private Methods
    private ushort NextCount() => (ushort)Interlocked.Increment(ref _counter);

    private ulong GetTimeStamp() => Convert.ToUInt64((DateTimeOffset.UtcNow - _epoch).TotalMilliseconds);

    private static byte CreateDefaultNodeId()
    {
        // 16 byte array of machine name
        var hashedBytes =
            MD5.HashData(Encoding.UTF8.GetBytes(Environment.MachineName)).AsSpan();

        const byte MAX_NODE_SIZE = 0b00111111;

        int ulongSize = sizeof(ulong);

        // first 8 bytes
        ulong first = BitConverter.ToUInt64(hashedBytes[0..ulongSize]);

        // second 8 bytes
        ulong second = BitConverter.ToUInt64(hashedBytes[ulongSize..(ulongSize * 2)]);

        // shuffle bits into 8 bytes
        ulong randomized = ~(first ^ second);

        // control for 0 values
        randomized = randomized switch
        {
            not 0 => randomized,
            _ => 1, // fallback to 1
        };

        // get the number of digits in the number
        var digits = Math.Floor(Math.Log10(randomized) + 1);

        // move all the digits behind the dec point
        decimal behindDec = (decimal)(randomized / Math.Pow(10, digits));

        // multiply by max node value to get random number in the range
        return (byte)Math.Floor(behindDec * MAX_NODE_SIZE);
    }
    #endregion
}
