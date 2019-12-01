using System;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public abstract class DisposableObject
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void FreeManagedObjects() { }
        protected virtual void FreeUnmanagedObjects() { }

        // Protected implementation of Dispose pattern.
        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                FreeManagedObjects();
            }

            FreeUnmanagedObjects();
            disposed = true;
        }

        ~DisposableObject()
        {
            Dispose(false);
        }
    }
}
