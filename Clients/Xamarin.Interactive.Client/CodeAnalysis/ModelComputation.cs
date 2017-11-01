//
// ModelComputation.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.CodeAnalysis
{
	sealed class ModelComputation<T> where T : class
	{
		readonly TaskScheduler taskScheduler;
		readonly Action<T> handleUpdatedModel;
		readonly CancellationTokenSource stopTokenSource = new CancellationTokenSource ();

		Task notifyTask;
		Task<T> lastTask;

		public Task<T> ModelTask { get { return lastTask; } }

		public T InitialUnfilteredModel { get; private set; }

		public ModelComputation (
			Action<T> handleUpdatedModel,
			TaskScheduler computationTaskScheduler)
		{
			this.handleUpdatedModel = handleUpdatedModel;
			this.taskScheduler = computationTaskScheduler;

			notifyTask = lastTask = Task.FromResult (default(T));
		}

		public void Stop ()
		{
			stopTokenSource.Cancel ();
			notifyTask = lastTask = Task.FromResult (default(T));
		}

		public void ChainTask (Func<T, T> transformModel)
			=> ChainTask ((m, c) => Task.FromResult (transformModel (m)));

		public void ChainTask (Func<T, CancellationToken, Task<T>> transformModelAsync)
		{
			// TODO: Roslyn uses their SafeContinueWithFromAsync extension
			// method, which also attaches a fatal error reporter to the task
			var nextTask = lastTask.ContinueWith (
				t => transformModelAsync (t.Result, stopTokenSource.Token),
				stopTokenSource.Token,
				TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LazyCancellation,
				taskScheduler).Unwrap ();

			lastTask = nextTask;

			notifyTask = Task.Factory.ContinueWhenAll (
				new [] { notifyTask, nextTask },
				tasks => {
					if (tasks.All (t => t.Status == TaskStatus.RanToCompletion)) {
						stopTokenSource.Token.ThrowIfCancellationRequested ();

						// Check if we're last task, if so, notify
						if (nextTask == lastTask) {
							var model = nextTask.Result;
							if  (InitialUnfilteredModel == null)
								InitialUnfilteredModel = model;
							handleUpdatedModel (model);
						}
					}
				},
				stopTokenSource.Token,
				TaskContinuationOptions.None,
				TaskScheduler.FromCurrentSynchronizationContext ());
		}
	}
}