//
// AgentProcessManagerTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class AgentProcessManagerTests
	{
		WorkbookAppInstallation workbookApp;
		List<Guid> observedAgentIdentityIds;

		[OneTimeSetUp]
		public void FixtureSetUp ()
		{
			observedAgentIdentityIds = new List<Guid> ();
			workbookApp = WorkbookAppInstallation.FromManifestObject (
				".",
				"test",
				JObject.FromObject (new {
					flavor = "Test",
					appPath = ".",
					sdk = new {
						targetFramework = ".NETFramework,Version=4.6.1",
						assemblySearchPaths = new [] { "." }
					}
				})).ShouldNotBeNull ();
		}

		/// <summary>
		/// Ensures that for each [TestCase] we have exactly one AgentProcess that
		/// starts up and responds to the identification request. Subsequent
		/// tickets will block until the first has started. All tickets should share
		/// the agent process with the same identity. Once the last ticket is
		/// disposed, the AgentProcess will terminate. Subsequent [TestCase] invocations
		/// should start over with a new AgentProcess. We ensure this is the case
		/// by tracking identity IDs that we've seen before.
		/// </summary>
		[TestCase (1)]
		[TestCase (2)]
		[TestCase (3)]
		[TestCase (4)]
		[TestCase (5)]
		[TestCase (10)]
		[TestCase (100)]
		[TestCase (1000)]
		[TestCase (10000)]
		public async Task OneAgentProcessForManyTickets (int ticketCount)
		{
			MainThread.Ensure ();

			var ticketRequests = new Task<AgentProcessTicket> [ticketCount];
			AgentIdentity firstIdentity = null;

			Func<Task<AgentProcessTicket>> RequestTicketAsync = async () => {
				var ticket = await workbookApp.RequestTestTicketAsync ();
				ticket.ShouldNotBeNull ();

				var identity = await ticket.GetAgentIdentityAsync ();
				identity.ShouldNotBeNull ();

				Interlocked.CompareExchange (ref firstIdentity, identity, null);

				return ticket;
			};

			await Task.Run (async () => {
				for (var i = 0; i < ticketRequests.Length; i++)
					ticketRequests [i] = RequestTicketAsync ();

				await Task.WhenAll (ticketRequests).ConfigureAwait (false);
			});

			MainThread.Ensure ();

			foreach (var ticketRequest in ticketRequests) {
				ticketRequest.IsCompleted.ShouldBeTrue ();

				var ticket = ticketRequest.Result;
				var identity = await ticket.GetAgentIdentityAsync ();

				identity.Id.ShouldEqual (firstIdentity.Id);
				identity.ShouldBeSameAs (firstIdentity);
			}

			observedAgentIdentityIds.ShouldNotContain (firstIdentity.Id);
			observedAgentIdentityIds.Add (firstIdentity.Id);

			foreach (var ticketRequest in ticketRequests)
				ticketRequest.Result.Dispose ();
		}

		/// <summary>
		/// Tests that an IAgentProcess raising its UnexpectedlyTerminated event
		/// results in associated AgentProcessTicket instances having their
		/// Disconnected event raised in response.
		/// </summary>
		[Test]
		[Ignore("Test flaps, needs revision")]
		public async Task AgentProcessTerminated ()
		{
			MainThread.Ensure ();

			FixtureSetUp ();

			TestAgent agent = null;
			TestAgent.PushAllocHandler (a => agent = a);

			var taskCompletionSource = new TaskCompletionSource ();
			await workbookApp.RequestTestTicketAsync (taskCompletionSource.SetResult);
			agent.Stop ();

			(await Task.WhenAny (Task.Delay (60000), taskCompletionSource.Task))
				.ShouldBeSameAs (taskCompletionSource.Task);
			taskCompletionSource.Task.IsCompleted.ShouldBeTrue ();
		}
	}
}