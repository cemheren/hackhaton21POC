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

    public partial class UserClass : IGeneratorCapable
    {
        public void StatelessImplementation()
        {
            int x = 5;
            float y = 5;
            Interleaver.Pause();
            
            Console.WriteLine(x);
            Interleaver.Pause();

            x = 10;
            Console.WriteLine(x);

            Interleaver.Wait(TimeSpan.FromSeconds(10));

            Console.WriteLine(x);
            Console.WriteLine("End");
        }

        public void RunAsync() {
            var currentState = new UserClassState();
            while (currentState.ExecutionState != -1)
            {
                Console.WriteLine($"Running state {currentState.ExecutionState}");
                this.GeneratedStatefulImplementation(currentState);
                Console.WriteLine("Simulating delay between state executions");
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            }
            
        }

        public partial void GeneratedStatefulImplementation(UserClassState state);
    }
}
