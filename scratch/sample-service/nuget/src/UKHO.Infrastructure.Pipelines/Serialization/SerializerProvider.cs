using System;

namespace UKHO.Infrastructure.Pipelines.Serialization
{
    public static class SerializerProvider
    {
        private static IComponentSerializer _serializer;

        public static IComponentSerializer Serializer
        {
            get
            {
                if (_serializer == null)
                {
                    throw new NullReferenceException("The Serializer has not been set");
                }

                return _serializer;
            }
            set => _serializer = value;
        }
    }
}
