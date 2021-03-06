﻿//-----------------------------------------------------------------------
// <copyright file="ExamplePersistentActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Persistence;
using Newtonsoft.Json.Linq;

namespace PersistenceExample
{
    public class Command
    {
        public Command(string data)
        {
            Data = data;
        }

        public string Data { get; private set; }

        public override string ToString()
        {
            return Data;
        }
    }

    public class Event
    {
        public Event(string data)
        {
            Data = data;
        }

        public string Data { get; private set; }

        public override string ToString()
        {
            return Data;
        }
    }

    public class ExampleState
    {
        public ExampleState(List<string> events = null)
        {
            Events = events ?? new List<string>();
        }

        public IEnumerable<string> Events { get; private set; }

        public ExampleState Update(Event evt)
        {
            var list = new List<string> {evt.Data};
            list.AddRange(Events);
            return new ExampleState(list);
        }

        public override string ToString()
        {
            return string.Join(", ", Events);
        }
    }

    public class ExamplePersistentActor : PersistentActor
    {
        public ExamplePersistentActor()
        {
            State = new ExampleState();
        }

        public override string PersistenceId { get { return "sample-id-1"; }}

        public ExampleState State { get; set; }
        public int EventsCount { get { return State.Events.Count(); } }

        public void UpdateState(Event evt)
        {
            State = State.Update(evt);
        }

        protected override bool ReceiveRecover(object message)
        {
            if (message is JObject)
            {
                UpdateState(((JObject)message).ToObject<Event>());

            }
            else if (message is SnapshotOffer )
            {
                SnapshotOffer so = (SnapshotOffer) message;
                Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)so.Snapshot;
                State = jo.ToObject<ExampleState>();
            }
            else return false;
            return true;
        }

        protected override bool ReceiveCommand(object message)
        {
            
            if (message is Command)
            {
                var cmd = message as Command;
                Persist(new Event(cmd.Data + "-" + EventsCount), UpdateState);
            }
            else if (message as string == "snap")
                SaveSnapshot(State);
            else if (message as string == "print")
                Console.WriteLine(State);
            else return false;
            return true;
        }
    }
}

