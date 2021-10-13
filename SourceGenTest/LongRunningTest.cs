using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceGenTest
{
    [LongRunningTest]
    public class LongRunningTest
    {
        public void Execute()
        {
            var x = 5;

            Interleaver.Pause();

            x = 10;
        }

        public void ExecuteInterleavedMock(ref int state, int x)
        {
            if (state == 0)
            {
                x = 5;
                state = 1;
                return;
            }

            if (state == 1)
            {
                // x should be 5 now. 

                x = 10;
                state = 0;
                return;
            }
        }
    }
}
