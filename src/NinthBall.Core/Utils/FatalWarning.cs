using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall
{
    public sealed class FatalWarning(string message) : Exception(message);
}
