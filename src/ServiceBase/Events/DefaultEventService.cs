﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ServiceBase.Events
{
    /// <summary>
    /// Default implementation of the event service. Write events raised to the log.
    /// </summary>

    public class DefaultEventService : IEventService
    {
        /// <summary>
        /// The options
        /// </summary>
        protected readonly EventOptions Options;

        /// <summary>
        /// The context
        /// </summary>
        protected readonly IHttpContextAccessor Context;

        /// <summary>
        /// The sink
        /// </summary>
        protected readonly IEventSink Sink;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEventService"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="context">The context.</param>
        /// <param name="sink">The sink.</param>
        public DefaultEventService(EventOptions options, IHttpContextAccessor context, IEventSink sink)
        {
            Options = options;
            Context = context;
            Sink = sink;
        }

        /// <summary>
        /// Raises the specified event.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">evt</exception>
        public async Task RaiseAsync(Event evt)
        {
            if (evt == null) throw new ArgumentNullException("evt");

            if (CanRaiseEvent(evt))
            {
                await PrepareEventAsync(evt);
                await Sink.PersistAsync(evt);
            }
        }

        /// <summary>
        /// Indicates if the type of event will be persisted.
        /// </summary>
        /// <param name="evtType"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public bool CanRaiseEventType(EventTypes evtType)
        {
            switch (evtType)
            {
                case EventTypes.Failure:
                    return Options.RaiseFailureEvents;
                case EventTypes.Information:
                    return Options.RaiseInformationEvents;
                case EventTypes.Success:
                    return Options.RaiseSuccessEvents;
                case EventTypes.Error:
                    return Options.RaiseErrorEvents;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Determines whether this event would be persisted.
        /// </summary>
        /// <param name="evt">The evt.</param>
        /// <returns>
        ///   <c>true</c> if this event would be persisted; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool CanRaiseEvent(Event evt)
        {
            return CanRaiseEventType(evt.EventType);
        }

        /// <summary>
        /// Prepares the event.
        /// </summary>
        /// <param name="evt">The evt.</param>
        /// <returns></returns>
        protected virtual async Task PrepareEventAsync(Event evt)
        {
            evt.ActivityId = Context.HttpContext.TraceIdentifier;
            evt.TimeStamp = DateTimeHelper.UtcNow;
            evt.ProcessId = Process.GetCurrentProcess().Id;

            if (Context.HttpContext.Connection.LocalIpAddress != null)
            {
                evt.LocalIpAddress = Context.HttpContext.Connection.LocalIpAddress.ToString() + ":" + Context.HttpContext.Connection.LocalPort;
            }
            else
            {
                evt.LocalIpAddress = "unknown";
            }

            if (Context.HttpContext.Connection.RemoteIpAddress != null)
            {
                evt.RemoteIpAddress = Context.HttpContext.Connection.RemoteIpAddress.ToString();
            }
            else
            {
                evt.RemoteIpAddress = "unknown";
            }

            await evt.PrepareAsync();
        }
    }
}