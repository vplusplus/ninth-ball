using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall.Core
{
    public sealed class FatalWarning(string message) : Exception(message);
}
