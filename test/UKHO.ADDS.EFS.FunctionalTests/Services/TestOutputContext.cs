using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    /// <summary>
    /// Provides ambient context for the current test output helper.
    /// This allows static utility classes to access logging without direct dependencies.
    /// </summary>
    public static class TestOutputContext
    {
        private static readonly AsyncLocal<ITestOutputHelper?> _current = new AsyncLocal<ITestOutputHelper?>();
        
        /// <summary>
        /// Gets or sets the current test output helper for the executing context.
        /// </summary>
        public static ITestOutputHelper? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        /// <summary>
        /// Clears the current test output helper reference.
        /// </summary>
        public static void Clear() => Current = null;

        /// <summary>
        /// Writes a message to the current output helper if available.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void WriteLine(string message)
        {
            Current?.WriteLine(message);
        }

        /// <summary>
        /// Writes a formatted message using invariant culture to the current output helper if available.
        /// </summary>
        /// <param name="message">The formattable message to write.</param>
        public static void WriteLine(FormattableString message)
        {
            // Use invariant culture to avoid locale drift in logs
            Current?.WriteLine(FormattableString.Invariant(message));
        }

        /// <summary>
        /// Writes an exception (and optional contextual message) to the current output helper if available.
        /// </summary>
        /// <param name="ex">The exception to write.</param>
        /// <param name="message">Optional contextual message.</param>
        public static void WriteLine(Exception ex, string? message = null)
        {
            if (Current is null)
                return;

            if (!string.IsNullOrWhiteSpace(message))
            {
                Current.WriteLine(message!);
            }

            // Include full exception details with stack trace
            Current.WriteLine(ex.ToString());
        }

        /// <summary>
        /// Begins a scope where <see cref="Current"/> is set to the provided helper and restored on dispose.
        /// Usage: using var _ = TestOutputContext.BeginScope(helper);
        /// </summary>
        /// <param name="helper">The test output helper to set for the scope.</param>
        /// <returns>An <see cref="IDisposable"/> that restores the previous helper when disposed.</returns>
        public static IDisposable BeginScope(ITestOutputHelper helper) => new TestOutputScope(helper);

        /// <summary>
        /// Scope helper that sets the current output helper on construction and restores the previous value on dispose.
        /// </summary>
        private sealed class TestOutputScope : IDisposable
        {
            private readonly ITestOutputHelper? _previous;
            private bool _disposed;

            public TestOutputScope(ITestOutputHelper helper)
            {
                _previous = Current;
                Current = helper ?? throw new ArgumentNullException(nameof(helper));
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                Current = _previous;
                _disposed = true;
            }
        }
    }
}
