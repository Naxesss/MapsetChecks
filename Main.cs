using MapsetVerifier;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetChecks
{
    public class Main
    {
        /// <summary> Needs to be exactly "public static void Run()". This is the start point of the program,
        /// which is executed from the MapsetVerifier application when its .dll file is provided in "/checks". </summary>
        public static void Run()
        {
            // CheckerRegistry is what MapsetVerifier uses to determine which checks to run, so by adding an
            // instance of a check to it, it will be executed and loaded exactly like any other check.

            // Examples
            // CheckerRegistry.RegisterCheck(new CheckTest());
            // CheckerRegistry.RegisterCheck(new GeneralCheckTest());

            // Timing
            CheckerRegistry.RegisterCheck(new CheckUnsnaps());
            CheckerRegistry.RegisterCheck(new CheckWrongSnapping());
        }
    }
}
