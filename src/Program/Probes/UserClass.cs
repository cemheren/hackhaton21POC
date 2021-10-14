//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Probes
{
    using System;
    using GeneratorDependencies;

    public partial class UserClassState
    { }

    public partial class UserClass
    {
        public UserClass()
        {
        }

        ////protected abstract Task ProbeImplementation();
        
        protected void ProbeImplementation()
        {
            int x = 5;
            int y = 5;
            Interleaver.Pause();
            
            Console.WriteLine("This is hardcoded test");
            Console.WriteLine("arbitrary");
            Console.WriteLine("number");
            Console.WriteLine("of");
            Interleaver.Pause();
            Interleaver.Pause();
            Interleaver.Pause();

            Console.WriteLine(x);

            Console.WriteLine("statements");
        }

        public void RunAsync() {
            var state = new UserClassState();
            state.x = 1;

            this.GeneratedProbeImplementation<UserClassState>(state);
        }

        public partial void GeneratedProbeImplementation<T>(T state);
    }
}
