using System.Linq;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Domain.Entities;
using NetArchTest.Rules;
using Xunit;

namespace Ecommerce.Architecture.Tests
{
    public class LayerDependencyTests
    {
        private static NetArchTest.Rules.PredicateList DomainTypes =>
            Types.InAssembly(typeof(Product).Assembly).That().ResideInNamespace("Ecommerce.Domain");

        private static NetArchTest.Rules.PredicateList ApplicationTypes =>
            Types.InAssembly(typeof(Ecommerce.Application.DTOs.ProductDto).Assembly).That().ResideInNamespace("Ecommerce.Application");

        private static NetArchTest.Rules.PredicateList InfrastructureTypes =>
            Types.InAssembly(typeof(Ecommerce.Infrastructure.Persistence.ApplicationDbContext).Assembly).That().ResideInNamespace("Ecommerce.Infrastructure");

        private static NetArchTest.Rules.PredicateList ApiTypes =>
            Types.InAssembly(typeof(Program).Assembly).That().ResideInNamespace("Ecommerce.Api");

        [Fact]
        public void Domain_ShouldNot_DependOn_Application()
        {
            var result = DomainTypes.ShouldNot().HaveDependencyOn("Ecommerce.Application").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Domain_ShouldNot_DependOn_Infrastructure()
        {
            var result = DomainTypes.ShouldNot().HaveDependencyOn("Ecommerce.Infrastructure").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Domain_ShouldNot_DependOn_Api()
        {
            var result = DomainTypes.ShouldNot().HaveDependencyOn("Ecommerce.Api").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Domain_ShouldNot_DependOn_ExternalSdks()
        {
            var result = DomainTypes.ShouldNot()
                .HaveDependencyOn("Microsoft.EntityFrameworkCore")
                .Or().HaveDependencyOn("Microsoft.AspNetCore")
                .GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Application_ShouldNot_DependOn_Infrastructure()
        {
            var result = ApplicationTypes.ShouldNot().HaveDependencyOn("Ecommerce.Infrastructure").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Application_ShouldNot_DependOn_Api()
        {
            var result = ApplicationTypes.ShouldNot().HaveDependencyOn("Ecommerce.Api").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Infrastructure_ShouldNot_DependOn_Api()
        {
            var result = InfrastructureTypes.ShouldNot().HaveDependencyOn("Ecommerce.Api").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Controllers_Should_DependOn_ApplicationLayer()
        {
            // Controllers act as the thin HTTP facade over the Application CQRS layer.
            var controllers = Types.InAssembly(typeof(Program).Assembly)
                .That().HaveNameEndingWith("Controller");

            var result = controllers.Should().HaveDependencyOn("Ecommerce.Application").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        [Fact]
        public void Controllers_ShouldNot_DependOn_Infrastructure_Except_AccountController()
        {
            var controllers = Types.InAssembly(typeof(Program).Assembly)
                .That().HaveNameEndingWith("Controller")
                .And().DoNotHaveName("AccountController");

            var result = controllers.ShouldNot().HaveDependencyOn("Ecommerce.Infrastructure").GetResult();
            Assert.True(result.IsSuccessful, BuildFailureMessage(result));
        }

        private static string BuildFailureMessage(TestResult result)
        {
            var details = string.Join("\n", result.FailingTypeNames?.Take(20) ?? Enumerable.Empty<string>());
            return $"Architecture rule violated. Failing types:\n{details}";
        }
    }

    public class ConventionTests
    {
        [Fact]
        public void Entities_Should_ResideIn_DomainEntitiesNamespace()
        {
            var types = Types.InAssembly(typeof(Product).Assembly)
                .That().HaveNameEndingWith("Entity")
                .GetTypes();

            Assert.True(types.All(t => t.FullName!.StartsWith("Ecommerce.Domain.Entities.")),
                "An 'Entity'-named type is not in Ecommerce.Domain.Entities");
        }

        [Fact]
        public void CommandHandlers_Should_Implement_ICommandHandler()
        {
            var handlers = Types.InAssembly(typeof(Ecommerce.Application.DTOs.ProductDto).Assembly)
                .That().HaveNameEndingWith("CommandHandler")
                .GetTypes();

            Assert.NotEmpty(handlers);
            Assert.All(handlers, h =>
            {
                Assert.True(
                    h.GetInterfaces().Any(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)) ||
                    h.GetInterfaces().Any(i => i.Name.StartsWith("ICommandHandler")),
                    $"{h.FullName} does not implement ICommandHandler<,>");
            });
        }

        [Fact]
        public void QueryHandlers_Should_Implement_IQueryHandler()
        {
            var handlers = Types.InAssembly(typeof(Ecommerce.Application.DTOs.ProductDto).Assembly)
                .That().HaveNameEndingWith("QueryHandler")
                .GetTypes();

            Assert.NotEmpty(handlers);
            Assert.All(handlers, h =>
            {
                Assert.True(
                    h.GetInterfaces().Any(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)) ||
                    h.GetInterfaces().Any(i => i.Name.StartsWith("IQueryHandler")),
                    $"{h.FullName} does not implement IQueryHandler<,>");
            });
        }

        [Fact]
        public void Interfaces_Should_ResideIn_ApplicationInterfaces()
        {
            var types = Types.InAssembly(typeof(Ecommerce.Application.DTOs.ProductDto).Assembly)
                .That().AreInterfaces()
                .And().HaveNameStartingWith("I")
                .GetTypes()
                .Where(t => t.Name.StartsWith("I") && !t.Name.StartsWith("IEnumerable"));

            Assert.True(types.All(t => t.FullName!.StartsWith("Ecommerce.Application.")),
                "An interface is defined outside the Application layer");
        }

        [Fact]
        public void Dtos_Should_ResideIn_ApplicationDtos()
        {
            var dtos = Types.InAssembly(typeof(Ecommerce.Application.DTOs.ProductDto).Assembly)
                .That().HaveNameEndingWith("Dto")
                .GetTypes();

            Assert.True(dtos.All(t => t.FullName!.StartsWith("Ecommerce.Application.")),
                "A DTO type is not in the Application layer");
        }
    }
}