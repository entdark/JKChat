using System;
using System.Threading;
using System.Threading.Tasks;

namespace JKChat.Core.Helpers {
	//source: https://github.com/gentlee/SerialQueue
	public class TasksQueue {
		private readonly SpinLock spinLock = new(false);
		private readonly WeakReference<Task> lastTaskRef = new(null);

		public Task Enqueue(Action action) {
			return Enqueue(() => {
				action();
				return Task.FromResult(true);
			});
		}

		public Task<T> Enqueue<T>(Func<T> func) {
			return Enqueue(() => Task.FromResult(func()));
		}

		public Task Enqueue(Func<Task> func) {
			return Enqueue(async () => {
				await func();
				return true;
			});
		}

		public Task<T> Enqueue<T>(Func<Task<T>> func) {
			bool lockTaken = false;
			try {
				Task<T> resultTask;

				spinLock.Enter(ref lockTaken);

				if (lastTaskRef.TryGetTarget(out Task lastTask)) {
					resultTask = lastTask.ContinueWith(_ => func(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
				} else {
					resultTask = Task.Run(func);
				}

				lastTaskRef.SetTarget(resultTask);

				return resultTask;
			} finally {
				if (lockTaken)
					spinLock.Exit(false);
			}
		}
	}
}

