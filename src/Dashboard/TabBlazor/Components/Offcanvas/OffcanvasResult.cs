namespace TabBlazor
{
    public class OffcanvasResult
    {
        internal OffcanvasResult(object data, Type resultType, bool cancelled)
        {
            Data = data;
            DataType = resultType;
            Cancelled = cancelled;
        }

        public object Data { get; }
        public Type DataType { get; }
        public bool Cancelled { get; }

        public static ModalResult Ok() => new(default, typeof(object), false);
        public static ModalResult Ok<T>(T result) => new(result, typeof(T), false);

        public static ModalResult Cancel() => new(default, typeof(object), true);
    }
}
