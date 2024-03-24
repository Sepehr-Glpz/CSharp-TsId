using SimpleBase;
using System.Numerics;
using System.Runtime.Serialization;

namespace SGSX.Common.TsId;

[Serializable]
public readonly struct TsId : IComparable<ulong>, ISerializable, IEquatable<ulong>, IEqualityOperators<TsId, TsId, bool>
{
    #region Fields

    private readonly ulong _value;

    #endregion

    #region Props
    public DateTimeOffset DefaultCreationDate => TsIdGenerator.DefaultEpoch + TimeSpan.FromMilliseconds(_value >> (NODE_ID_BIT_SIZE + COUNTER_BIT_SIZE));
    public DateTimeOffset CreationDate(DateTimeOffset epoch) => epoch + TimeSpan.FromMilliseconds(_value >> (NODE_ID_BIT_SIZE + COUNTER_BIT_SIZE));
    public byte NodeId => (byte)((_value >> 16) & 0b_00111111);
    public ushort Counter => (ushort)_value;

    #endregion

    #region Constructors

    private TsId(ulong value) => _value = value;

    public TsId(ulong timestamp, byte nodeId, ushort counter) =>
        (_value) = Generate(SanatizeTimestamp(timestamp), SanatizeNodeId(nodeId), counter);

    static TsId()
    {
        _factory = new TsIdGenerator();
    }

    #endregion

    #region Static Methods
    public static TsId NewTsId() => _factory.NewTsId();

    public static TsId FromLong(long value) => new((ulong)value);
    public static TsId FromLong(ulong value) => new(value);
    public static TsId FromString(string base32Value) => Base32.Crockford.DecodeUInt64(base32Value);

    public static implicit operator TsId(ulong value) => new(value);
    public static implicit operator ulong(TsId value) => value._value;

    public static bool operator ==(TsId left, TsId right) => left._value == right._value;
    public static bool operator !=(TsId left, TsId right) => left._value != right._value;
    public static bool operator <(TsId left, TsId right) => left._value < right._value;
    public static bool operator <=(TsId left, TsId right) => left._value <= right._value;
    public static bool operator >(TsId left, TsId right) => left._value > right._value;
    public static bool operator >=(TsId left, TsId right) => left._value >= right._value;

    #endregion

    #region Private Utility
    private static readonly TsIdGenerator _factory;

    private static ulong SanatizeTimestamp(ulong value) => (value & 0b00000000_00000000_00000011_111111111_11111111_11111111_11111111_11111111);
    private static byte SanatizeNodeId(byte nodeId) => (byte)(nodeId & 0b_00111111);

    private const byte NODE_ID_BIT_SIZE = 6;
    private const byte TIMESTAMP_BIT_SIZE = (sizeof(uint) + 10) * 8;
    private const byte COUNTER_BIT_SIZE = sizeof(ushort) * 8;
    private static ulong Generate(ulong timestamp, byte nodeId, ushort counter)
    {
        ulong id = 0;
        id |= timestamp;
        id <<= NODE_ID_BIT_SIZE;
        id |= nodeId;
        id <<= COUNTER_BIT_SIZE;
        id |= counter;

        return id;
    }

    #endregion

    #region Impl

    public int CompareTo(ulong other) => _value.CompareTo(other);

    public void GetObjectData(SerializationInfo info, StreamingContext context) => 
        info.AddValue(nameof(_value), _value);

    public bool Equals(ulong other) => _value == other;

    public override bool Equals(object? obj) => _value.Equals(obj);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => Base32.Crockford.Encode(BitConverter.GetBytes(_value));

    #endregion
}
