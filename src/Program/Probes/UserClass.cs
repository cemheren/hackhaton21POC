//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Probes
{
    using System;
    using System.Threading.Tasks;
    using GeneratorDependencies;

    public partial class UserClass
    {
        public UserClass()
        {
        }
        
        protected void ProbeImplementation()
        {
            var x = 5;
            var y = 5;
            Interleaver.Pause();
            
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
            int currentState = 0;
            while (currentState != -1)
            {
                Console.WriteLine($"Running state {currentState}");
                this.GeneratedProbeImplementation(ref currentState);
                Console.WriteLine("Simulating delay between state executions");
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            }
            
        }

        partial void GeneratedProbeImplementation(ref int currentState);
    }
}
