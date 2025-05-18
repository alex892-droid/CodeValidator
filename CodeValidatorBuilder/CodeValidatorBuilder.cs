namespace CodeValidatorBuilder
{
    public class CodeValidatorBuilder
    {
        public NamespaceValidatorBuilder ForNamespace(string namespaceName)
        {
            throw new NotSupportedException("This builder is for analyzers only.");
        }
    }

    public class NamespaceValidatorBuilder
    {
        public PropertyValidatorBuilder ForAllProperties()
        {
            throw new NotSupportedException("This builder is for analyzers only.");
        }

        public ClassValidatorBuilder ForAllClasses()
        {
            throw new NotSupportedException("This builder is for analyzers only.");
        }
    }

    public class PropertyValidatorBuilder
    {
        public PropertyValidatorBuilder RequireNullableProperties(string errorMessage)
        {
            throw new NotSupportedException("This builder is for analyzers only.");
        }
    }
    public class ClassValidatorBuilder
    {
        public ClassValidatorBuilder RequireClassNamePattern(string regex, string errorMessage)
        {
            throw new NotSupportedException("This builder is for analyzers only.");
        }
    }
}
