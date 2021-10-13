//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Probes
{
    using System;
    using Hackathon21Poc.Utils;

    public partial class UserClass
    {
        public UserClass()
        {
        }

        ////protected abstract Task ProbeImplementation();
        
        protected void ProbeImplementation()
        {
            Console.WriteLine("This is hardcoded test3fafds");
            Console.WriteLine("arbitrary");
            Console.WriteLine("number");
            Console.WriteLine("of");

            Interleaver.Pause();

            Console.WriteLine("of");
            Console.WriteLine("statements");
        }

        public void RunAsync() {
            this.GeneratedProbeImplementation();
        }

        partial void GeneratedProbeImplementation();
    }
}
