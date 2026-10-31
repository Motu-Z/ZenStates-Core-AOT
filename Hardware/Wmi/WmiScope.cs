using System;
using WmiLight;

namespace ZenStates.Core.Hardware
{
    /// <summary>
    /// AOT-safe stand-in for <c>System.Management.ManagementScope</c>, backed by
    /// <see cref="WmiConnection"/> from WmiLight.
    /// </summary>
    /// <remarks>
    /// The scope owns an open connection for its whole lifetime. WmiLight's <c>Open()</c> is idempotent,
    /// so <see cref="Connect"/> may be called repeatedly.
    /// </remarks>
    public sealed class WmiScope : IDisposable
    {
        private readonly WmiConnection connection;
        private bool disposed;

        public WmiScope(string scope)
        {
            if (string.IsNullOrEmpty(scope))
                throw new ArgumentNullException(nameof(scope));

            ScopePath = scope;
            connection = new WmiConnection(scope);
        }

        /// <summary>
        /// The namespace this scope is bound to, e.g. <c>root\wmi</c>.
        /// </summary>
        public string ScopePath { get; private set; }

        /// <summary>
        /// True once a WMI server/namespace binding has been established.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (disposed)
                    return false;

                return connection.IsConnected;
            }
        }

        /// <summary>
        /// Binds to the WMI namespace. Idempotent.
        /// </summary>
        public void Connect()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(WmiScope));

            connection.Open();
        }

        internal WmiConnection Connection
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(WmiScope));

                return connection;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            connection.Dispose();
        }
    }
}
