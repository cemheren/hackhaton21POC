//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Probes
{
    using System;
    using System.Threading.Tasks;
    using GeneratorDependencies;

    public partial class UserClassState : InterleaverState
    { }

    public partial class UserClass
    {
        public UserClass()
        {
        }
        
        protected void ProbeImplementation()
        {
            int x = 5;
            int y = 5;
            Interleaver.Pause();
            
            Console.WriteLine("This is hardcoded test");
            Interleaver.Pause();

            //Console.WriteLine(x);

            Interleaver.Pause();

            Console.WriteLine("End");
        }

        public void RunAsync() {
            var currentState = new UserClassState();
            while (currentState.ExecutionState != -1)
            {
                Console.WriteLine($"Running state {currentState.ExecutionState}");
                this.GeneratedProbeImplementation<UserClassState>(currentState);
                Console.WriteLine("Simulating delay between state executions");
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            }
            
        }

        public partial void GeneratedProbeImplementation<T>(T state) where T : InterleaverState;
    }
}
