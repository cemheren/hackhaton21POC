using System;

namespace SourceGenTest
{
    partial class Program
    {
        static void Main(string[] args)
        {
            HelloFrom("Generated Code");
        }

        static partial void HelloFrom(string name);
    }
}
