// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Subscriptions.Coordinator
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Magnum;
	using Magnum.Extensions;
	using Messages;
	using log4net;

	public class EndpointSubscription
	{
		static readonly ILog _log = LogManager.GetLogger(typeof (EndpointSubscription));
		readonly IDictionary<Guid, PeerSubscription> _ids;
		readonly string _messageName;
		readonly BusSubscriptionEventObserver _observer;
		Uri _endpointUri;
		Guid _subscriptionId;

		public EndpointSubscription(string messageName, BusSubscriptionEventObserver observer)
		{
			_messageName = messageName;
			_observer = observer;

			_ids = new Dictionary<Guid, PeerSubscription>();

			_subscriptionId = Guid.Empty;
		}

		public void Send(AddPeerSubscription message)
		{
			if (_ids.ContainsKey(message.SubscriptionId))
				return;

			_ids.Add(message.SubscriptionId, message);

			if (_ids.Count > 1)
				return;

			_subscriptionId = CombGuid.Generate();
			_endpointUri = message.EndpointUri;

			var add = new SubscriptionAddedMessage
				{
					SubscriptionId = _subscriptionId,
					EndpointUri = _endpointUri,
					MessageName = _messageName
				};

			_log.DebugFormat("PeerSubscriptionAdded: {0}, {1} {2}", _messageName, _endpointUri, _subscriptionId);

			_observer.OnSubscriptionAdded(add);
		}

		public void Send(RemovePeerSubscription message)
		{
			bool wasRemoved = _ids.Remove(message.SubscriptionId);
			if (!wasRemoved || _ids.Count != 0)
				return;

			var remove = new SubscriptionRemovedMessage
				{
					SubscriptionId = _subscriptionId,
					EndpointUri = _endpointUri,
					MessageName = _messageName
				};

			_log.DebugFormat("PeerSubscriptionRemoved: {0}, {1} {2}", _messageName, _endpointUri, _subscriptionId);

			_observer.OnSubscriptionRemoved(remove);

			_subscriptionId = Guid.Empty;
		}

		public void Send(RemovePeer message)
		{
			List<Guid> remove = _ids.Where(x => x.Value.PeerId == message.PeerId)
				.Select(x => x.Key).ToList();

			if (remove.Count > 0)
			{
				remove.Each(x => _ids.Remove(x));

				_log.DebugFormat("Removed {0} subscriptions for peer: {1} {2}", remove.Count, message.PeerId, message.PeerUri);
			}
		}
	}
}