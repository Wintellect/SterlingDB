
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wintellect.Sterling.Core.Database
{
    public sealed class PendingOperationProgressChangedEventArgs : EventArgs
    {
        internal PendingOperationProgressChangedEventArgs( decimal progress )
        {
            this.Progress = progress;
        }

        public decimal Progress { get; private set; }
    }

    public sealed class PendingOperationErrorEventArgs : EventArgs
    {
        internal PendingOperationErrorEventArgs( Exception ex )
        {
            this.Exception = ex;
        }

        public Exception Exception { get; private set; }
    }

    public sealed class PendingOperation
    {
        private readonly Action _uow = null;
        private readonly CancellationTokenSource _cancelSource = null;

        internal PendingOperation( Action<CancellationToken> work )
        {
            _cancelSource = new CancellationTokenSource();
            _uow = () => work( _cancelSource.Token );
            this.Task = Task.Factory.StartNew( DoWork, _cancelSource.Token );
        }

        internal PendingOperation( Action<CancellationToken> work, CancellationTokenSource cancelSource )
        {
            _cancelSource = cancelSource;
            _uow = () => work( _cancelSource.Token );
            this.Task = Task.Factory.StartNew( DoWork, _cancelSource.Token );
        }

        internal PendingOperation( Action<CancellationToken, Action<decimal>> progressVisibleWork )
        {
            _cancelSource = new CancellationTokenSource();
            _uow = () => progressVisibleWork( _cancelSource.Token, ReportProgress );
            this.Task = Task.Factory.StartNew( DoWork, _cancelSource.Token );
        }

        internal PendingOperation( Action<CancellationToken, Action<decimal>> progressVisibleWork, CancellationTokenSource cancelSource )
        {
            _cancelSource = cancelSource;
            _uow = () => progressVisibleWork( _cancelSource.Token, ReportProgress );
            this.Task = Task.Factory.StartNew( DoWork, _cancelSource.Token );
        }

        public Task Task { get; private set; }

        public event EventHandler Completed = delegate { };
        public event EventHandler Canceled = delegate { };
        public event EventHandler<PendingOperationErrorEventArgs> ErrorOccured = delegate { };
        public event EventHandler<PendingOperationProgressChangedEventArgs> ProgressChanged = delegate { };

        public void Cancel()
        {
            _cancelSource.Cancel();
        }

        private void DoWork()
        {
            try
            {
                _uow();
                Completed( this, EventArgs.Empty );
            }
            catch ( OperationCanceledException )
            {
                Canceled( this, EventArgs.Empty );
                throw;
            }
            catch ( Exception ex )
            {
                ErrorOccured( this, new PendingOperationErrorEventArgs( ex ) );
                throw;
            }
        }

        private void ReportProgress( decimal percentComplete )
        {
            ProgressChanged( this, new PendingOperationProgressChangedEventArgs( percentComplete ) );
        }
    }
}
