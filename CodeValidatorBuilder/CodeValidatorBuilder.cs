namespace CodeValidatorBuilder
{
    public class CodeValidatorBuilder
    {
        public NamespaceValidatorBuilder ForNamespace(string namespaceName)
        {
            return new NamespaceValidatorBuilder();
        }
    }

    public class NamespaceValidatorBuilder
    {
        public PropertyValidatorBuilder ForAllProperties()
        {
            return new PropertyValidatorBuilder();
        }
    }

    public class PropertyValidatorBuilder
    {
        public PropertyValidatorBuilder MustBeNullable(string errorMessage)
        {
            return this;
        }
    }
}
