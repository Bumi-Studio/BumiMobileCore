using System;

namespace BumiMobile
{
    public abstract class LoadingTask
    {
        public bool IsActive { get; private set; }
        public bool IsFinished { get; private set; }

        public CompleteStatus Status { get; private set; }

        public abstract string TaskName { get; }

        public event Action<CompleteStatus> OnTaskCompleted;

        protected LoadingTask()
        {
            IsActive = false;
            IsFinished = false;
        }

        public void CompleteTask(CompleteStatus status)
        {
            if (IsFinished) return;

            Status = status;
            IsFinished = true;

            OnTaskCompleted?.Invoke(status);
        }

        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;

            try
            {
                OnTaskActivated();
            }
            catch
            {
                CompleteTask(CompleteStatus.Failed);
            }
        }

        protected abstract void OnTaskActivated();

        public enum CompleteStatus
        {
            Skipped,
            Completed,
            Failed
        }
    }
}
