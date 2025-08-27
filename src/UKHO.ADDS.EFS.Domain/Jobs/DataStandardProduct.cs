using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.Extensions;
using UKHO.ADDS.EFS.Jobs;
using Vogen;

namespace UKHO.ADDS.EFS.VOS
{
    [ValueObject<int>(Conversions.SystemTextJson, typeof(ValidationException))]
    public partial struct DataStandardProduct
    {
        private static Validation Validate(int value)
        {
            return Enum.IsDefined(typeof(DataStandardProductType), value)
                ? Validation.Ok
                : Validation.Invalid($"Unknown {nameof(DataStandardProduct)} code '{value}'");
        }

        public static DataStandardProduct S57 => From((int)DataStandardProductType.S57);
        public static DataStandardProduct S101 => From((int)DataStandardProductType.S101);
        public static DataStandardProduct S102 => From((int)DataStandardProductType.S102);
        public static DataStandardProduct S104 => From((int)DataStandardProductType.S104);
        public static DataStandardProduct S111 => From((int)DataStandardProductType.S111);
        public static DataStandardProduct S121 => From((int)DataStandardProductType.S121);
        public static DataStandardProduct S122 => From((int)DataStandardProductType.S122);
        public static DataStandardProduct S124 => From((int)DataStandardProductType.S124);
        public static DataStandardProduct S125 => From((int)DataStandardProductType.S125);
        public static DataStandardProduct S126 => From((int)DataStandardProductType.S126);
        public static DataStandardProduct S127 => From((int)DataStandardProductType.S127);
        public static DataStandardProduct S128 => From((int)DataStandardProductType.S128);
        public static DataStandardProduct S129 => From((int)DataStandardProductType.S129);
        public static DataStandardProduct S130 => From((int)DataStandardProductType.S130);
        public static DataStandardProduct S131 => From((int)DataStandardProductType.S131);
        public static DataStandardProduct S164 => From((int)DataStandardProductType.S164);

        public DataStandardProductType AsEnum => (DataStandardProductType)Value;

        public string DisplayName => AsEnum.GetDisplayName();

        public string Description => AsEnum.GetDisplayDescription() ?? AsEnum.ToString(); 

        public static DataStandardProduct FromEnum(DataStandardProductType type) => From((int)type);

        public DataStandard DataStandard
        {
            get
            {
                return Value == (int)DataStandardProductType.S57 ? DataStandard.S57 : DataStandard.S100;
            }
        }
    }
}
