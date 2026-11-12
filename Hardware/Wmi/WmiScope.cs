using System;
using WmiLight;

namespace ZenStates.Core.Hardware
{
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

        public string ScopePath { get; private set; }

        public bool IsConnected
        {
            get
            {
                if (disposed)
                    return false;

                return connection.IsConnected;
            }
        }

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
