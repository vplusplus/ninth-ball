using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall
{
    internal sealed class FatalWarning(string message) : Exception(message);
}
