//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Probes
{
    using System;
    using GeneratorDependencies;

    public partial class UserClass
    {
        public UserClass()
        {
        }

        ////protected abstract Task ProbeImplementation();
        
        protected void ProbeImplementation()
        {
            var x = 5;
            var y = 5;
            Console.WriteLine("This is hardcoded test");
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
