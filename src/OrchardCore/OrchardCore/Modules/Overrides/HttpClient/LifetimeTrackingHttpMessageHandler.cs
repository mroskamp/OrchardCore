// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    // This is a marker used to check if the underlying handler should be disposed. HttpClients
    // share a reference to an instance of this class, and when it goes out of scope the inner handler
    // is eligible to be disposed.
    internal sealed class LifetimeTrackingHttpMessageHandler : DelegatingHandler
    {
        public LifetimeTrackingHttpMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override void Dispose(bool disposing)
        {
            // The lifetime of this is tracked separately by ActiveHandlerTrackingEntry.
        }
    }
}
