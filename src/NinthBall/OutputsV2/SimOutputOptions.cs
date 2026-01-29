using NinthBall.Outputs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NinthBall.OutputsV2
{
    public sealed record SimOutputFiles
    (
        
        [property: Required] string HtmlFileName,
        [property: Required] string ExcelFileName
    );
}
