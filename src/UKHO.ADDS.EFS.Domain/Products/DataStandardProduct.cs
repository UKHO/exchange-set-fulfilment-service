using System.ComponentModel.DataAnnotations;
using UKHO.ADDS.EFS.Domain.Implementation.Extensions;
using Vogen;

namespace UKHO.ADDS.EFS.Domain.Products
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

        public static DataStandardProduct S57 => DataStandardProduct.From((int)DataStandardProductType.S57);
        public static DataStandardProduct S101 => DataStandardProduct.From((int)DataStandardProductType.S101);
        public static DataStandardProduct S102 => DataStandardProduct.From((int)DataStandardProductType.S102);
        public static DataStandardProduct S104 => DataStandardProduct.From((int)DataStandardProductType.S104);
        public static DataStandardProduct S111 => DataStandardProduct.From((int)DataStandardProductType.S111);
        public static DataStandardProduct S121 => DataStandardProduct.From((int)DataStandardProductType.S121);
        public static DataStandardProduct S122 => DataStandardProduct.From((int)DataStandardProductType.S122);
        public static DataStandardProduct S124 => DataStandardProduct.From((int)DataStandardProductType.S124);
        public static DataStandardProduct S125 => DataStandardProduct.From((int)DataStandardProductType.S125);
        public static DataStandardProduct S126 => DataStandardProduct.From((int)DataStandardProductType.S126);
        public static DataStandardProduct S127 => DataStandardProduct.From((int)DataStandardProductType.S127);
        public static DataStandardProduct S128 => DataStandardProduct.From((int)DataStandardProductType.S128);
        public static DataStandardProduct S129 => DataStandardProduct.From((int)DataStandardProductType.S129);
        public static DataStandardProduct S130 => DataStandardProduct.From((int)DataStandardProductType.S130);
        public static DataStandardProduct S131 => DataStandardProduct.From((int)DataStandardProductType.S131);
        public static DataStandardProduct S164 => DataStandardProduct.From((int)DataStandardProductType.S164);

        public DataStandardProductType AsEnum => (DataStandardProductType)Value;

        public string DisplayName => AsEnum.GetDisplayName();

        public string Description => AsEnum.GetDisplayDescription() ?? AsEnum.ToString(); 

        public static DataStandardProduct FromEnum(DataStandardProductType type) => DataStandardProduct.From((int)type);

        public DataStandard DataStandard
        {
            get
            {
                return Value == (int)DataStandardProductType.S57 ? DataStandard.S57 : DataStandard.S100;
            }
        }
    }
}
