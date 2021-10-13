//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Probes
{
    using System;

    public partial class UserClass
    {
        public UserClass()
        {
        }

        ////protected abstract Task ProbeImplementation();
        
        protected void ProbeImplementation()
        {
            Console.WriteLine("This is hardcoded test");
            Console.WriteLine("arbitrary");
            Console.WriteLine("number");
            Console.WriteLine("of");

            Console.WriteLine("statements");
        }

        public void RunAsync() {
            this.GeneratedProbeImplementation();
        }

        partial void GeneratedProbeImplementation();
    }
}
