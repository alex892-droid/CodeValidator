// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");

new CodeValidatorBuilder.CodeValidatorBuilder()
    .ForNamespace("CodeValidator.Tester.Test")
    .ForAllProperties()
    .MustBeNullable("Les propriétés doivent être nullable");