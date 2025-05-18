using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeValidator.Tester
{
    internal class CodeValidator
    {
        public CodeValidator()
        {
            new CodeValidatorBuilder.CodeValidatorBuilder()
                .ForNamespace("CodeValidator.Tester.Test")
                .ForAllProperties()
                .RequireNullableProperties("Les propriétés doivent être nullable");

            new CodeValidatorBuilder.CodeValidatorBuilder()
                .ForNamespace("CodeValidator.Tester.Test")
                .ForAllClasses()
                .RequireClassNamePattern("^Form.*", "Les classes doivent commencer par Form");
        }
    }
}
