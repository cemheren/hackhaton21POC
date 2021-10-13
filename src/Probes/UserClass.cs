//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Generators.Probes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract partial class UserClass
    {
        protected UserClass(string probeType)
        {
        }

        protected abstract Task ProbeImplementation();

        protected Task RunAsync() {
            this.GeneratedProbeImplementation();
        }
    }
}
