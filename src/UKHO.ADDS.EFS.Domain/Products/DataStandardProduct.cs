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

        /// <summary>
        /// Creates a <see cref="DataStandardProduct"/> from the name of a <see cref="DataStandardProductType"/>.
        /// </summary>
        /// <param name="name">The string representation of the enum value (e.g. "S57").</param>
        /// <exception cref="ValidationException">Thrown if the string does not correspond to a valid <see cref="DataStandardProductType"/>.</exception>
        public static DataStandardProduct From(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException($"{nameof(DataStandardProduct)} cannot be created from an empty string.");
            }

            if (Enum.TryParse<DataStandardProductType>(name.Trim(), ignoreCase: true, out var parsed) &&
                Enum.IsDefined(typeof(DataStandardProductType), parsed))
            {
                return FromEnum(parsed);
            }

            throw new ValidationException($"Unknown {nameof(DataStandardProduct)} name '{name}'.");
        }

        public DataStandard DataStandard
        {
            get
            {
                return Value == (int)DataStandardProductType.S57
                    ? DataStandard.S57
                    : DataStandard.S100;
            }
        }
    }
}
