﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq;

namespace Digify.Events
{
    public class EventBusState : IEventBusState
    {
        public ConcurrentDictionary<string, ConcurrentBag<Func<IServiceProvider, IDictionary<string, object>, Task>>> Subscribers { get; private set; }

        public EventBusState()
        {
            Subscribers =
                new ConcurrentDictionary<string,
                    ConcurrentBag<Func<IServiceProvider, IDictionary<string, object>, Task>>>();
        }

        public void Add(string message, Func<IServiceProvider, IDictionary<string, object>, Task> action)
        {
            var messageSubscribers = Subscribers.GetOrAdd(message, m => new ConcurrentBag<Func<IServiceProvider, IDictionary<string, object>, Task>>());
            messageSubscribers.Add(action);
            Subscribers = Subscribers;
        }
    }
}